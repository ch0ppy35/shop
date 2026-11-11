using Common.Database;
using Common.Messaging;
using Common.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Products.Repositories;
using Products.Services;
using Products.Tests.TestHelpers;

namespace Products.Tests.Services;

public class ProductConsumerServiceTests
{
    private readonly Mock<IProductService> _productServiceMock = new();
    private readonly Mock<INatsService> _natsServiceMock = new();
    private readonly List<(string Subject, object Message)> _published = new();

    public ProductConsumerServiceTests()
    {
        _natsServiceMock
            .Setup(n => n.PublishAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .Callback(new InvocationAction(inv =>
            {
                lock (_published)
                {
                    _published.Add(((string)inv.Arguments[0], inv.Arguments[1]!));
                }
            }))
            .Returns(Task.CompletedTask);
    }

    private ProductConsumerService CreateConsumer()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _productServiceMock.Object);
        return CreateConsumer(services.BuildServiceProvider());
    }

    private ProductConsumerService CreateConsumer(IServiceProvider provider) =>
        new(NullLogger<ProductConsumerService>.Instance, _natsServiceMock.Object,
            provider.GetRequiredService<IServiceScopeFactory>());

    private static ProductMessage Request(string? replyTo = "test.reply")
    {
        var message = TestData.GetTestProductMessage();
        message.SessionId = "test-session";
        message.ReplyTo = replyTo;
        return message;
    }

    private T SingleReply<T>() where T : class =>
        _published.Should().ContainSingle(p => p.Subject == "test.reply").Which.Message.Should().BeOfType<T>().Subject;

    private IEnumerable<ProductChangedEvent> ChangeEvents =>
        _published.Where(p => p.Subject == Subjects.ProductsChanged).Select(p => (ProductChangedEvent)p.Message);

    [Fact]
    public async Task Get_ShouldReplyWithProduct_WhenProductExists()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.GetProductAsync(message.ProductId!)).ReturnsAsync(message);

        await CreateConsumer().HandleRequestAsync("products.get", message);

        var reply = SingleReply<ProductResponse>();
        reply.Success.Should().BeTrue();
        reply.Product!.ProductId.Should().Be(message.ProductId);
        reply.SessionId.Should().Be("test-session");
        ChangeEvents.Should().BeEmpty("reads don't change anything");
    }

    [Fact]
    public async Task Get_ShouldReplyWithError_WhenProductDoesNotExist()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.GetProductAsync(message.ProductId!)).ReturnsAsync((ProductMessage?)null);

        await CreateConsumer().HandleRequestAsync("products.get", message);

        var reply = SingleReply<ProductResponse>();
        reply.Success.Should().BeFalse();
        reply.Error.Should().Contain("not found");
        reply.SessionId.Should().Be("test-session");
    }

    [Fact]
    public async Task Get_WithMissingProductId_ShouldReplyWithError_WithoutCallingService()
    {
        var message = Request();
        message.ProductId = null;

        await CreateConsumer().HandleRequestAsync("products.get", message);

        var reply = SingleReply<ProductResponse>();
        reply.Success.Should().BeFalse();
        reply.Error.Should().Contain("null or empty");
        _productServiceMock.Verify(s => s.GetProductAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_ShouldReplyWithPage()
    {
        var message = new ProductMessage
        {
            SessionId = "test-session", PageNumber = 2, PageSize = 5, ReplyTo = "test.reply"
        };
        var products = Enumerable.Range(1, 5).Select(_ => TestData.GetTestProductMessage()).ToList();
        _productServiceMock.Setup(s => s.GetPaginatedProductsAsync(2, 5)).ReturnsAsync((products, 20, 4));

        await CreateConsumer().HandleRequestAsync("products.getall", message);

        var reply = SingleReply<ProductListResponse>();
        reply.Success.Should().BeTrue();
        reply.Products.Should().HaveCount(5);
        reply.TotalCount.Should().Be(20);
        reply.TotalPages.Should().Be(4);
        reply.HasPreviousPage.Should().BeTrue();
        reply.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldReplyAndPublishCreatedEvent()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.CreateProductAsync(It.IsAny<ProductMessage>())).ReturnsAsync(message);

        await CreateConsumer().HandleRequestAsync("products.create", message);

        SingleReply<ProductResponse>().Success.Should().BeTrue();
        var change = ChangeEvents.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ProductChangeType.Created);
        change.ProductId.Should().Be(message.ProductId);
        change.Product.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldReplyWithError_AndPublishNothing_WhenServiceThrows()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.CreateProductAsync(It.IsAny<ProductMessage>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        await CreateConsumer().HandleRequestAsync("products.create", message);

        var reply = SingleReply<ProductResponse>();
        reply.Success.Should().BeFalse();
        reply.Error.Should().Contain("Test exception");
        reply.SessionId.Should().Be("test-session");
        ChangeEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ShouldReplyWithProduct_AndPublishUpdatedEvent()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.UpdateProductAsync(It.IsAny<ProductMessage>())).ReturnsAsync(true);
        _productServiceMock.Setup(s => s.GetProductAsync(message.ProductId!)).ReturnsAsync(message);

        await CreateConsumer().HandleRequestAsync("products.update", message);

        SingleReply<ProductResponse>().Product!.ProductId.Should().Be(message.ProductId);
        ChangeEvents.Should().ContainSingle(e => e.ChangeType == ProductChangeType.Updated);
    }

    [Fact]
    public async Task Update_ShouldReplyWithError_AndPublishNothing_WhenProductDoesNotExist()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.UpdateProductAsync(It.IsAny<ProductMessage>())).ReturnsAsync(false);

        await CreateConsumer().HandleRequestAsync("products.update", message);

        SingleReply<ProductResponse>().Error.Should().Contain("not found");
        ChangeEvents.Should().BeEmpty();
        _productServiceMock.Verify(s => s.GetProductAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ShouldPublishDeletedEvent_WithoutProduct()
    {
        var message = Request();
        _productServiceMock.Setup(s => s.DeleteProductAsync(message.ProductId!)).ReturnsAsync(true);

        await CreateConsumer().HandleRequestAsync("products.delete", message);

        SingleReply<BaseResponse>().Success.Should().BeTrue();
        var change = ChangeEvents.Should().ContainSingle().Subject;
        change.ChangeType.Should().Be(ProductChangeType.Deleted);
        change.Product.Should().BeNull();
    }

    [Fact]
    public async Task InventoryUpdate_ShouldPublishInventoryUpdatedEvent()
    {
        var message = Request();
        message.QuantityInStock = 3;
        _productServiceMock.Setup(s => s.UpdateInventoryAsync(message.ProductId!, 3)).ReturnsAsync(true);
        _productServiceMock.Setup(s => s.GetProductAsync(message.ProductId!)).ReturnsAsync(message);

        await CreateConsumer().HandleRequestAsync("products.inventory.update", message);

        SingleReply<ProductResponse>().Success.Should().BeTrue();
        ChangeEvents.Should().ContainSingle(e => e.ChangeType == ProductChangeType.InventoryUpdated);
    }

    [Fact]
    public async Task Request_WithoutReplyTo_ShouldNotReply()
    {
        var message = Request(replyTo: null);
        _productServiceMock.Setup(s => s.GetProductAsync(message.ProductId!)).ReturnsAsync(message);

        var response = await CreateConsumer().HandleRequestAsync("products.get", message);

        response.Success.Should().BeTrue();
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldEachGetTheirOwnDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContextPool<ProductDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddScoped<IProductDbContext>(sp => sp.GetRequiredService<ProductDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductService, ProductService>();
        var provider = services.BuildServiceProvider();

        var seeded = TestData.GetTestProductEntities(20);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            db.Products.AddRange(seeded);
            await db.SaveChangesAsync();
        }

        var consumer = CreateConsumer(provider);

        var requests = Enumerable.Range(0, 200).Select(i => Task.Run(() =>
        {
            var product = seeded[i % seeded.Count];
            return (i % 4) switch
            {
                0 => consumer.HandleRequestAsync("products.getall",
                    new ProductMessage { PageNumber = 1 + i % 3, PageSize = 7 }),
                1 => consumer.HandleRequestAsync("products.inventory.update",
                    new ProductMessage { ProductId = product.ProductId, QuantityInStock = i }),
                _ => consumer.HandleRequestAsync("products.get", new ProductMessage { ProductId = product.ProductId })
            };
        }));

        var responses = await Task.WhenAll(requests);

        responses.Should().AllSatisfy(r => r.Error.Should().BeNull());
        responses.Should().OnlyContain(r => r.Success);
    }
}
