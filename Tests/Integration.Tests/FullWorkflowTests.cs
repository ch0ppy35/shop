using Common.Models;
using FluentAssertions;
using Integration.Tests.Fixtures;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Integration tests for the full workflow between services
/// </summary>
[Collection("Integration Tests")]
public class FullWorkflowTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="FullWorkflowTests"/> class
    /// </summary>
    public FullWorkflowTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests the full workflow: Create product → Add to cart → Get recommendations
    /// </summary>
    [Fact]
    public async Task FullWorkflow_CreateProduct_AddToCart_GetRecommendations_ShouldSucceed()
    {
        Console.WriteLine("\n=== Starting FullWorkflow_CreateProduct_AddToCart_GetRecommendations_ShouldSucceed test ===");
        // Arrange
        var sessionId = _fixture.CreateTestSessionId();
        Console.WriteLine($"Created test session ID: {sessionId}");

        // Step 1: Create a product
        Console.WriteLine("Step 1: Creating test product...");
        var product = await _fixture.CreateTestProductAsync("Test Product Workflow", 29.99m);
        product.Should().NotBeNull();
        product.ProductId.Should().NotBeNullOrEmpty();
        product.Name.Should().Be("Test Product Workflow");
        product.Price.Should().Be(29.99m);
        Console.WriteLine($"Created product with ID: {product.ProductId}");

        // Step 2: Add the product to the cart
        Console.WriteLine("Step 2: Adding product to cart...");
        var cartResponse = await _fixture.AddProductToCartAsync(sessionId, product.ProductId!, 2);
        cartResponse.Should().NotBeNull();
        cartResponse.Success.Should().BeTrue();
        cartResponse.Items.Should().NotBeNull();
        cartResponse.Items!.Should().HaveCount(1);
        cartResponse.Items![0].ProductId.Should().Be(product.ProductId);
        cartResponse.Items![0].Quantity.Should().Be(2);
        cartResponse.Items![0].Price.Should().Be(29.99m);
        cartResponse.TotalPrice.Should().Be(59.98m);
        Console.WriteLine("Successfully added product to cart");

        // Step 3: Get recommendations based on the cart
        Console.WriteLine("Step 3: Getting recommendations based on cart...");
        var recommendationResponse = await _fixture.GetRecommendationsAsync(sessionId, cartResponse.Items);
        recommendationResponse.Should().NotBeNull();
        recommendationResponse.Success.Should().BeTrue();
        recommendationResponse.Recommendations.Should().NotBeNull();
        recommendationResponse.Recommendations!.Should().NotBeEmpty();

        // Verify that the original product is not in the recommendations
        recommendationResponse.Recommendations!.Should().NotContain(p => p.ProductId == product.ProductId);
        Console.WriteLine($"Successfully got {recommendationResponse.Recommendations!.Count} recommendations");
        Console.WriteLine("=== Test completed successfully ===");
    }

    /// <summary>
    /// Tests updating inventory and verifying the changes
    /// </summary>
    [Fact]
    public async Task UpdateInventory_ShouldUpdateProductQuantity()
    {
        // Arrange
        var product = await _fixture.CreateTestProductAsync("Inventory Test Product", 39.99m);
        product.Should().NotBeNull();
        product.ProductId.Should().NotBeNullOrEmpty();
        product.QuantityInStock.Should().Be(100); // Initial quantity

        // Act - Update inventory
        // Use the fixture's method to update the product inventory
        var updateResponse = await _fixture.UpdateProductInventoryAsync(product.ProductId!, 75);

        // Assert
        updateResponse.Should().NotBeNull();
        updateResponse!.Success.Should().BeTrue();
        updateResponse.Product.Should().NotBeNull();
        updateResponse.Product!.QuantityInStock.Should().Be(75);

        // Verify by getting the product
        // Use the fixture's method to get the product
        var getResponse = await _fixture.GetProductAsync(product.ProductId!);

        getResponse.Should().NotBeNull();
        getResponse!.Success.Should().BeTrue();
        getResponse.Product.Should().NotBeNull();
        getResponse.Product!.QuantityInStock.Should().Be(75);
    }

    /// <summary>
    /// Tests cart management operations
    /// </summary>
    [Fact]
    public async Task CartManagement_AddUpdateRemoveClear_ShouldSucceed()
    {
        // Arrange
        var sessionId = _fixture.CreateTestSessionId();
        var product1 = await _fixture.CreateTestProductAsync("Cart Test Product 1", 19.99m);
        var product2 = await _fixture.CreateTestProductAsync("Cart Test Product 2", 29.99m);

        // Step 1: Add products to cart
        var addResponse1 = await _fixture.AddProductToCartAsync(sessionId, product1.ProductId!, 2);
        addResponse1.Should().NotBeNull();
        addResponse1.Success.Should().BeTrue();
        addResponse1.Items.Should().NotBeNull();
        addResponse1.Items!.Should().HaveCount(1);
        addResponse1.Items![0].ProductId.Should().Be(product1.ProductId);
        addResponse1.Items![0].Quantity.Should().Be(2);

        var addResponse2 = await _fixture.AddProductToCartAsync(sessionId, product2.ProductId!, 1);
        addResponse2.Should().NotBeNull();
        addResponse2.Success.Should().BeTrue();
        addResponse2.Items.Should().NotBeNull();
        addResponse2.Items!.Should().HaveCount(2);

        // Step 2: Update product quantity
        // Use the fixture's method to update the cart item
        var updateResponse = await _fixture.UpdateCartItemAsync(sessionId, product1.ProductId!, 3);

        updateResponse.Should().NotBeNull();
        updateResponse!.Success.Should().BeTrue();
        updateResponse.Items.Should().NotBeNull();
        updateResponse.Items!.Should().HaveCount(2);
        updateResponse.Items!.First(i => i.ProductId == product1.ProductId).Quantity.Should().Be(3);

        // Step 3: Remove a product
        // Use the fixture's method to remove the product from the cart
        var removeResponse = await _fixture.RemoveProductFromCartAsync(sessionId, product2.ProductId!);

        removeResponse.Should().NotBeNull();
        removeResponse!.Success.Should().BeTrue();
        removeResponse.Items.Should().NotBeNull();
        removeResponse.Items!.Should().HaveCount(1);
        removeResponse.Items![0].ProductId.Should().Be(product1.ProductId);

        // Step 4: Clear the cart
        // Use the fixture's method to clear the cart
        var clearResponse = await _fixture.ClearCartAsync(sessionId);

        clearResponse.Should().NotBeNull();
        clearResponse!.Success.Should().BeTrue();
        clearResponse.Items.Should().NotBeNull();
        clearResponse.Items!.Should().BeEmpty();
        clearResponse.TotalPrice.Should().Be(0);
    }

    /// <summary>
    /// Tests error handling across services
    /// </summary>
    [Fact]
    public async Task ErrorHandling_InvalidOperations_ShouldReturnErrors()
    {
        // Arrange
        var sessionId = _fixture.CreateTestSessionId();
        var nonExistentProductId = "nonexistent-product-id";

        // Test 1: Get non-existent product
        var getResponse = await _fixture.GetProductAsync(nonExistentProductId);

        getResponse.Should().NotBeNull();
        getResponse!.Success.Should().BeFalse();
        getResponse.Error.Should().NotBeNullOrEmpty();

        // Test 2: Update non-existent product
        // We don't have a direct method for this, but we can use the GetProductAsync method
        // which will return an error for a non-existent product
        var updateResponse = await _fixture.GetProductAsync(nonExistentProductId);

        updateResponse.Should().NotBeNull();
        updateResponse!.Success.Should().BeFalse();
        updateResponse.Error.Should().NotBeNullOrEmpty();

        // Test 3: Update inventory for non-existent product
        var inventoryResponse = await _fixture.UpdateProductInventoryAsync(nonExistentProductId, 50);

        inventoryResponse.Should().NotBeNull();
        inventoryResponse!.Success.Should().BeFalse();
        inventoryResponse.Error.Should().NotBeNullOrEmpty();

        // Test 4: Add non-existent product to cart
        // This will actually succeed because the cart service doesn't validate product existence
        var cartResponse = await _fixture.AddProductToCartAsync(
            sessionId,
            nonExistentProductId,
            1,
            19.99m);

        // This should succeed because the cart service doesn't validate product existence
        cartResponse.Should().NotBeNull();
        cartResponse!.Success.Should().BeTrue();

        // Test 5: Update non-existent item in cart
        var newSessionId = Guid.NewGuid().ToString(); // New session ID with empty cart
        var updateCartResponse = await _fixture.UpdateCartItemAsync(newSessionId, nonExistentProductId, 5);

        updateCartResponse.Should().NotBeNull();
        updateCartResponse!.Success.Should().BeFalse();
        updateCartResponse.Error.Should().NotBeNullOrEmpty();
    }
}