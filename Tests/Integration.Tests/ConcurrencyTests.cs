using Common.Models;
using FluentAssertions;
using Integration.Tests.Fixtures;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Integration tests for concurrent operations
/// </summary>
[Collection("Integration Tests")]
public class ConcurrencyTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyTests"/> class
    /// </summary>
    public ConcurrencyTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests concurrent product creation
    /// </summary>
    [Fact]
    public async Task ConcurrentProductCreation_ShouldCreateAllProducts()
    {
        // Arrange
        const int concurrentOperations = 10;
        var tasks = new List<Task<ProductMessage>>();

        // Act - Create multiple products concurrently
        for (int i = 0; i < concurrentOperations; i++)
        {
            tasks.Add(_fixture.CreateTestProductAsync($"Concurrent Product {i}", 10.00m + i));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentOperations);
        results.Select(p => p.ProductId).Should().OnlyHaveUniqueItems();

        // Verify all products were created by getting them
        foreach (var product in results)
        {
            var getResponse = await _fixture.NatsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.get",
                new ProductMessage
                {
                    ProductId = product.ProductId,
                    OperationType = ProductOperationType.Get
                });

            getResponse.Should().NotBeNull();
            getResponse!.Success.Should().BeTrue();
            getResponse.Product.Should().NotBeNull();
            getResponse.Product!.ProductId.Should().Be(product.ProductId);
        }
    }

    /// <summary>
    /// Tests concurrent cart operations
    /// </summary>
    [Fact]
    public async Task ConcurrentCartOperations_ShouldHandleConcurrency()
    {
        // Arrange
        var sessionId = _fixture.CreateTestSessionId();
        var product = await _fixture.CreateTestProductAsync("Concurrency Test Product", 25.99m);

        // Add the product to the cart first
        await _fixture.AddProductToCartAsync(sessionId, product.ProductId!, 1);

        const int concurrentOperations = 10;
        var tasks = new List<Task<CartResponse>>();

        // Act - Update the cart quantity concurrently
        for (int i = 0; i < concurrentOperations; i++)
        {
            int quantity = i + 1;
            tasks.Add(_fixture.NatsService.RequestAsync<CartMessage, CartResponse>(
                "cart.updateitem",
                new CartMessage
                {
                    SessionId = sessionId,
                    ProductId = product.ProductId,
                    Quantity = quantity,
                    OperationType = CartOperationType.UpdateItem
                })!);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentOperations);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Get the final cart state
        var finalCart = await _fixture.GetCartAsync(sessionId);
        finalCart.Should().NotBeNull();
        finalCart.Success.Should().BeTrue();
        finalCart.Items.Should().NotBeNull();
        finalCart.Items!.Should().HaveCount(1);

        // The final quantity should be the last update (concurrentOperations)
        finalCart.Items![0].Quantity.Should().Be(concurrentOperations);
    }

    /// <summary>
    /// Tests concurrent inventory updates
    /// </summary>
    [Fact]
    public async Task ConcurrentInventoryUpdates_ShouldHandleConcurrency()
    {
        // Arrange
        var product = await _fixture.CreateTestProductAsync("Inventory Concurrency Test", 29.99m);
        product.QuantityInStock.Should().Be(100); // Initial quantity

        const int concurrentOperations = 5;
        var tasks = new List<Task<ProductResponse>>();

        // Act - Update inventory concurrently
        for (int i = 0; i < concurrentOperations; i++)
        {
            int quantity = 100 - (i * 10); // Decreasing quantities: 100, 90, 80, 70, 60
            tasks.Add(_fixture.NatsService.RequestAsync<ProductMessage, ProductResponse>(
                "products.inventory.update",
                new ProductMessage
                {
                    ProductId = product.ProductId,
                    QuantityInStock = quantity,
                    OperationType = ProductOperationType.UpdateInventory
                })!);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrentOperations);
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());

        // Get the final product state
        var finalProduct = await _fixture.NatsService.RequestAsync<ProductMessage, ProductResponse>(
            "products.get",
            new ProductMessage
            {
                ProductId = product.ProductId,
                OperationType = ProductOperationType.Get
            });

        finalProduct.Should().NotBeNull();
        finalProduct!.Success.Should().BeTrue();
        finalProduct.Product.Should().NotBeNull();

        // The final quantity should be the last update (60)
        finalProduct.Product!.QuantityInStock.Should().Be(60);
    }

    /// <summary>
    /// Tests concurrent operations across different services
    /// </summary>
    [Fact]
    public async Task ConcurrentCrossServiceOperations_ShouldHandleConcurrency()
    {
        // Arrange
        var sessionId = _fixture.CreateTestSessionId();
        var product = await _fixture.CreateTestProductAsync("Cross-Service Test", 39.99m);

        // Add the product to the cart
        await _fixture.AddProductToCartAsync(sessionId, product.ProductId!, 5);

        // Act - Perform operations on different services concurrently
        var inventoryTask = _fixture.NatsService.RequestAsync<ProductMessage, ProductResponse>(
            "products.inventory.update",
            new ProductMessage
            {
                ProductId = product.ProductId,
                QuantityInStock = 75,
                OperationType = ProductOperationType.UpdateInventory
            });

        var cartTask = _fixture.NatsService.RequestAsync<CartMessage, CartResponse>(
            "cart.updateitem",
            new CartMessage
            {
                SessionId = sessionId,
                ProductId = product.ProductId,
                Quantity = 10,
                OperationType = CartOperationType.UpdateItem
            });

        var recommendationTask = _fixture.GetRecommendationsAsync(sessionId);

        // Wait for all tasks to complete
        await Task.WhenAll(inventoryTask, cartTask, recommendationTask);

        // Assert
        var inventoryResult = await inventoryTask;
        var cartResult = await cartTask;
        var recommendationResult = await recommendationTask;

        inventoryResult.Should().NotBeNull();
        inventoryResult!.Success.Should().BeTrue();
        inventoryResult.Product!.QuantityInStock.Should().Be(75);

        cartResult.Should().NotBeNull();
        cartResult!.Success.Should().BeTrue();
        cartResult.Items!.Should().HaveCount(1);
        cartResult.Items![0].Quantity.Should().Be(10);

        recommendationResult.Should().NotBeNull();
        recommendationResult!.Success.Should().BeTrue();
    }
}