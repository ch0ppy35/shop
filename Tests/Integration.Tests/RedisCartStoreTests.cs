using Cart.Services;
using Common.Models;
using FluentAssertions;
using Integration.Tests.Fixtures;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Runs the cart store against a real Valkey container, because atomicity can't be shown with mocks
/// </summary>
public class RedisCartStoreTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _redis;
    private readonly RedisCartStore _store;

    public RedisCartStoreTests(RedisFixture redis)
    {
        _redis = redis;
        _store = new RedisCartStore(redis.RedisService);
    }

    private static string NewSession() => Guid.NewGuid().ToString();

    private static CartItem Mouse(int quantity = 1) =>
        new() { ProductId = "mouse", Name = "Mouse", Price = 19.50m, Quantity = quantity };

    [Fact]
    public async Task ConcurrentAdds_ShouldNotLoseUpdates()
    {
        var session = NewSession();
        const int adds = 100;

        await Task.WhenAll(Enumerable.Range(0, adds).Select(_ => Task.Run(() => _store.AddItemAsync(session, Mouse()))));

        var items = await _store.GetItemsAsync(session);
        items.Should().ContainSingle().Which.Quantity.Should().Be(adds);
    }

    [Fact]
    public async Task AddItem_ShouldReturnTheCartAfterTheWrite()
    {
        var session = NewSession();

        await _store.AddItemAsync(session, Mouse(2));
        var items = await _store.AddItemAsync(session,
            new CartItem { ProductId = "kb", Name = "Keyboard", Price = 80m, Quantity = 1 });

        items.Should().HaveCount(2);
        items.Should().ContainSingle(i => i.ProductId == "mouse").Which.Should()
            .BeEquivalentTo(new { Name = "Mouse", Price = 19.50m, Quantity = 2 });
    }

    [Fact]
    public async Task SetQuantity_ShouldReturnNull_AndNotCreateTheLine_WhenProductIsNotInCart()
    {
        var session = NewSession();

        var result = await _store.SetQuantityAsync(session, "mouse", 3);

        result.Should().BeNull();
        (await _store.GetItemsAsync(session)).Should().BeEmpty();
    }

    [Fact]
    public async Task SetQuantity_ToZero_ShouldRemoveTheLine()
    {
        var session = NewSession();
        await _store.AddItemAsync(session, Mouse(4));

        var items = await _store.SetQuantityAsync(session, "mouse", 0);

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAndClear_ShouldEmptyTheCart()
    {
        var session = NewSession();
        await _store.AddItemAsync(session, Mouse());

        (await _store.RemoveItemAsync(session, "mouse")).Should().BeEmpty();
        (await _store.RemoveItemAsync(session, "mouse")).Should().BeNull();

        await _store.AddItemAsync(session, Mouse());
        await _store.ClearAsync(session);
        (await _store.GetItemsAsync(session)).Should().BeEmpty();
    }

    [Fact]
    public async Task Writes_ShouldSetTheTtlOnBothKeys()
    {
        var session = NewSession();
        await _store.AddItemAsync(session, Mouse());

        var db = _redis.RedisService.Database;
        var quantityTtl = await db.KeyTimeToLiveAsync(RedisCartStore.QuantityKey(session));
        var itemsTtl = await db.KeyTimeToLiveAsync(RedisCartStore.ItemsKey(session));

        quantityTtl.Should().BeCloseTo(_redis.RedisService.CartTtl, TimeSpan.FromSeconds(5));
        itemsTtl.Should().BeCloseTo(_redis.RedisService.CartTtl, TimeSpan.FromSeconds(5));
    }
}
