using Cart.Services;
using Common.Messaging;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cart.Tests.Services;

public class CartConsumerServiceTests
{
    private readonly Mock<ICartStore> _storeMock = new();
    private readonly Mock<INatsService> _natsMock = new();
    private readonly List<(string Subject, object Message)> _published = new();
    private readonly CartConsumerService _consumer;
    private readonly string _sessionId = Guid.NewGuid().ToString();

    public CartConsumerServiceTests()
    {
        _natsMock
            .Setup(n => n.PublishAsync(It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .Callback(new InvocationAction(inv => _published.Add(((string)inv.Arguments[0], inv.Arguments[1]!))))
            .Returns(Task.CompletedTask);

        var cartService = new CartService(NullLogger<CartService>.Instance, _storeMock.Object);
        _consumer = new CartConsumerService(NullLogger<CartConsumerService>.Instance, _natsMock.Object, cartService);
    }

    private CartMessage Message(string? productId = "p1") => new()
    {
        SessionId = _sessionId, ProductId = productId, Name = "Mouse", Price = 20m, Quantity = 2, ReplyTo = "reply"
    };

    [Fact]
    public async Task AddItem_ShouldReplyAndPublishCartChanged()
    {
        var items = new List<CartItem> { new() { ProductId = "p1", Name = "Mouse", Price = 20m, Quantity = 2 } };
        _storeMock.Setup(s => s.AddItemAsync(_sessionId, It.IsAny<CartItem>())).ReturnsAsync(items);

        await _consumer.HandleRequestAsync("cart.additem", Message());

        _published.Should().Contain(p => p.Subject == "reply");
        var change = _published.Should().ContainSingle(p => p.Subject == Subjects.CartChanged(_sessionId))
            .Which.Message.Should().BeOfType<CartChangedEvent>().Subject;
        change.ItemCount.Should().Be(2);
        change.TotalPrice.Should().Be(40m);
        change.SessionId.Should().Be(_sessionId);
    }

    [Fact]
    public async Task GetCart_ShouldNotPublishCartChanged()
    {
        _storeMock.Setup(s => s.GetItemsAsync(_sessionId)).ReturnsAsync(new List<CartItem>());

        await _consumer.HandleRequestAsync("cart.get", Message());

        _published.Should().ContainSingle(p => p.Subject == "reply");
    }

    [Fact]
    public async Task FailedWrite_ShouldNotPublishCartChanged()
    {
        _storeMock.Setup(s => s.RemoveItemAsync(_sessionId, "p1")).ReturnsAsync((List<CartItem>?)null);
        _storeMock.Setup(s => s.GetItemsAsync(_sessionId)).ReturnsAsync(new List<CartItem>());

        var response = await _consumer.HandleRequestAsync("cart.removeitem", Message());

        response.Success.Should().BeFalse();
        _published.Should().ContainSingle(p => p.Subject == "reply");
    }

    [Fact]
    public async Task MissingProductId_ShouldReplyWithError_InsteadOfLeavingTheCallerToTimeOut()
    {
        var response = await _consumer.HandleRequestAsync("cart.additem", Message(productId: null));

        response.Success.Should().BeFalse();
        response.Error.Should().Contain("Product ID");
        _published.Should().ContainSingle(p => p.Subject == "reply");
        _storeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NonGuidSessionId_ShouldNotBeUsedAsASubject()
    {
        var message = Message();
        message.SessionId = "evil.>";
        _storeMock.Setup(s => s.ClearAsync("evil.>")).Returns(Task.CompletedTask);

        await _consumer.HandleRequestAsync("cart.clear", message);

        _published.Should().OnlyContain(p => p.Subject == "reply");
    }
}
