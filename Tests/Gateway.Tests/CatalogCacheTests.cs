using Common.Messaging;
using Common.Models;
using FluentAssertions;
using Gateway.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Gateway.Tests;

public class CatalogCacheTests
{
    private readonly Mock<INatsService> _nats = new();

    private CatalogCache CreateCache(IDistributedCache? level2 = null)
    {
        var services = new ServiceCollection();
        if (level2 != null)
        {
            services.AddSingleton(level2);
        }

        services.AddHybridCache();
        var hybrid = services.BuildServiceProvider().GetRequiredService<HybridCache>();
        return new CatalogCache(hybrid, _nats.Object, NullLogger<CatalogCache>.Instance);
    }

    private void ServeProduct(string id, ProductResponse response) =>
        _nats.Setup(n => n.RequestAsync<ProductMessage, ProductResponse>("products.get",
                It.Is<ProductMessage>(m => m.ProductId == id), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ProductResponse
            {
                Success = response.Success, Error = response.Error, SessionId = response.SessionId,
                Product = response.Product
            });

    private void VerifyProductFetches(string id, int times) =>
        _nats.Verify(n => n.RequestAsync<ProductMessage, ProductResponse>("products.get",
            It.Is<ProductMessage>(m => m.ProductId == id), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()),
            Times.Exactly(times));

    private static ProductResponse Found(string id, decimal price = 10m) => new()
    {
        Success = true, SessionId = "someone-elses-session",
        Product = new ProductMessage { ProductId = id, Name = id, Price = price }
    };

    [Fact]
    public async Task GetProduct_ShouldServeRepeatReadsFromCache_WithoutTheFirstCallersSession()
    {
        ServeProduct("a", Found("a"));
        var cache = CreateCache();

        await cache.GetProductAsync("a");
        var second = await cache.GetProductAsync("a");

        second!.Product!.ProductId.Should().Be("a");
        second.SessionId.Should().BeNull("cached responses are shared by every shopper");
        VerifyProductFetches("a", 1);
    }

    [Fact]
    public async Task GetProduct_ShouldNotCacheFailures()
    {
        ServeProduct("missing", new ProductResponse { Success = false, Error = "Product with ID missing not found" });
        var cache = CreateCache();

        var first = await cache.GetProductAsync("missing");
        await cache.GetProductAsync("missing");

        first!.Error.Should().Contain("not found");
        VerifyProductFetches("missing", 2);
    }

    [Fact]
    public async Task Invalidate_ShouldDropTheProductAndEveryCatalogEntry_ButNotOtherProducts()
    {
        ServeProduct("a", Found("a"));
        ServeProduct("b", Found("b"));
        _nats.Setup(n => n.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
                It.IsAny<ProductMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ProductListResponse { Success = true, Products = new() });
        var cache = CreateCache();

        await cache.GetProductAsync("a");
        await cache.GetProductAsync("b");
        await cache.GetPageAsync(1, 12);

        await cache.InvalidateAsync(new ProductChangedEvent { ProductId = "a", ChangeType = ProductChangeType.Updated });

        await cache.GetProductAsync("a");
        await cache.GetProductAsync("b");
        await cache.GetPageAsync(1, 12);

        VerifyProductFetches("a", 2);
        VerifyProductFetches("b", 1);
        _nats.Verify(n => n.RequestAsync<ProductMessage, ProductListResponse>("products.getall",
            It.IsAny<ProductMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetRecommendations_ShouldShareOneEntry_RegardlessOfIdOrder()
    {
        _nats.Setup(n => n.RequestAsync<RecommendationMessage, RecommendationResponse>("recommendations.get",
                It.IsAny<RecommendationMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new RecommendationResponse { Success = true, Recommendations = new() });
        var cache = CreateCache();

        await cache.GetRecommendationsAsync(new[] { "a", "b" }, 4);
        await cache.GetRecommendationsAsync(new[] { "b", "a", "a" }, 4);

        _nats.Verify(n => n.RequestAsync<RecommendationMessage, RecommendationResponse>("recommendations.get",
            It.Is<RecommendationMessage>(m => m.CartItems!.Count == 2 && m.SessionId == null),
            It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProduct_ShouldStillWork_WhenValkeyIsDown()
    {
        ServeProduct("a", Found("a"));
        var brokenValkey = new Mock<IDistributedCache>();
        brokenValkey.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        brokenValkey.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        var cache = CreateCache(brokenValkey.Object);

        var response = await cache.GetProductAsync("a");

        response!.Success.Should().BeTrue();
    }
}
