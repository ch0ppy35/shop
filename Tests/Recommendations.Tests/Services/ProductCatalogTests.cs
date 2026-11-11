using Common.Messaging;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Recommendations.Services;
using Xunit;

namespace Recommendations.Tests.Services;

public class ProductCatalogTests
{
    private readonly Mock<INatsService> _nats = new();
    private readonly ProductCatalog _catalog;

    public ProductCatalogTests()
    {
        _catalog = new ProductCatalog(NullLogger<ProductCatalog>.Instance, _nats.Object);
    }

    private void ServePages(params List<ProductMessage>[] pages)
    {
        _nats.Setup(n => n.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
                It.IsAny<ProductMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, ProductMessage m, TimeSpan? _, CancellationToken _) => new ProductListResponse
            {
                Success = true, Products = pages[m.PageNumber - 1], PageNumber = m.PageNumber, TotalPages = pages.Length
            });
    }

    private void VerifyLoads(int times) =>
        _nats.Verify(n => n.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
            It.IsAny<ProductMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(times));

    private static ProductMessage P(string id, decimal price = 10m) => new() { ProductId = id, Name = id, Price = price };

    [Fact]
    public async Task GetProducts_ShouldLoadEveryPage_OnlyOnce()
    {
        ServePages(new List<ProductMessage> { P("a"), P("b") }, new List<ProductMessage> { P("c") });

        (await _catalog.GetProductsAsync(null)).Select(p => p.ProductId).Should().BeEquivalentTo("a", "b", "c");
        await _catalog.GetProductsAsync(null);

        VerifyLoads(2);
    }

    [Fact]
    public async Task Apply_ShouldUpdateAndDelete_WithoutReloading()
    {
        ServePages(new List<ProductMessage> { P("a"), P("b") });
        await _catalog.GetProductsAsync(null);

        _catalog.Apply(new ProductChangedEvent
            { ProductId = "a", ChangeType = ProductChangeType.Updated, Product = P("a", 99m) });
        _catalog.Apply(new ProductChangedEvent { ProductId = "b", ChangeType = ProductChangeType.Deleted });
        _catalog.Apply(new ProductChangedEvent
            { ProductId = "c", ChangeType = ProductChangeType.Created, Product = P("c") });

        var products = await _catalog.GetProductsAsync(null);
        products.Should().HaveCount(2);
        products.Single(p => p.ProductId == "a").Price.Should().Be(99m);
        VerifyLoads(1);
    }

    [Fact]
    public async Task FailedLoad_ShouldBeRetriedOnTheNextCall()
    {
        _nats.SetupSequence(n => n.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
                It.IsAny<ProductMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException())
            .ReturnsAsync(new ProductListResponse { Success = true, Products = new() { P("a") }, TotalPages = 1 });

        (await _catalog.GetProductsAsync(null)).Should().BeEmpty();
        (await _catalog.GetProductsAsync(null)).Should().ContainSingle();
    }

    [Fact]
    public async Task ChangeDuringLoad_ShouldNotBeOverwrittenByTheStaleLoad()
    {
        var release = new TaskCompletionSource<ProductListResponse>();
        _nats.SetupSequence(n => n.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
                It.IsAny<ProductMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(release.Task)
            .ReturnsAsync(new ProductListResponse { Success = true, Products = new() { P("a", 50m) }, TotalPages = 1 });

        var loading = _catalog.GetProductsAsync(null);
        _catalog.Apply(new ProductChangedEvent
            { ProductId = "a", ChangeType = ProductChangeType.Updated, Product = P("a", 50m) });
        release.SetResult(new ProductListResponse { Success = true, Products = new() { P("a", 10m) }, TotalPages = 1 });

        (await loading).Single().Price.Should().Be(50m, "the event is newer than the in-flight load");
        (await _catalog.GetProductsAsync(null)).Single().Price.Should().Be(50m);
    }
}
