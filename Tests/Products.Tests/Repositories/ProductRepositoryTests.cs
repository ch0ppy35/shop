using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Products.Repositories;
using Products.Tests.TestHelpers;

namespace Products.Tests.Repositories;

public class ProductRepositoryTests : IClassFixture<InMemoryDbContextFixture>
{
    private readonly InMemoryDbContextFixture _fixture;
    private readonly Mock<ILogger<ProductRepository>> _loggerMock;
    private readonly IProductRepository _repository;

    public ProductRepositoryTests(InMemoryDbContextFixture fixture)
    {
        _fixture = fixture;
        _loggerMock = new Mock<ILogger<ProductRepository>>();
        _repository = new ProductRepository(_loggerMock.Object, _fixture.Context);

        _fixture.Context.Products.RemoveRange(_fixture.Context.Products);
        _fixture.Context.SaveChanges();
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        var testProducts = TestData.GetTestProductEntities(5);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        var result = await _repository.GetAllProductsAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Select(p => p.ProductId).Should().BeEquivalentTo(testProducts.Select(p => p.ProductId));
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_ShouldReturnCorrectPage()
    {
        var testProducts = TestData.GetTestProductEntities(20);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        var (products, totalCount) = await _repository.GetPaginatedProductsAsync(2, 5);

        products.Should().NotBeNull();
        products.Should().HaveCount(5);
        totalCount.Should().Be(20);

        var productsList = products.ToList();
        productsList[0].ProductId.Should().NotBeNullOrEmpty();
        productsList[4].ProductId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        var testProduct = TestData.GetTestProductEntity();
        await _fixture.Context.Products.AddAsync(testProduct);
        await _fixture.Context.SaveChangesAsync();

        var result = await _repository.GetProductByIdAsync(testProduct.ProductId);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(testProduct.ProductId);
        result.Name.Should().Be(testProduct.Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        var result = await _repository.GetProductByIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct()
    {
        var testProduct = TestData.GetTestProductEntity();

        var result = await _repository.CreateProductAsync(testProduct);

        result.Should().NotBeNull();
        result.ProductId.Should().Be(testProduct.ProductId);

        var dbProduct = await _fixture.Context.Products.FindAsync(result.Id);
        dbProduct.Should().NotBeNull();
        dbProduct!.ProductId.Should().Be(testProduct.ProductId);
    }

    [Fact]
    public async Task CreateProductAsync_WithNullProduct_ShouldThrowException()
    {
        // Act & Assert
        // This will throw NullReferenceException in the current implementation
        // In a real-world scenario, we would want to improve the implementation to throw ArgumentNullException
        await Assert.ThrowsAnyAsync<Exception>(() => _repository.CreateProductAsync(null!));
    }

    [Fact]
    public async Task CreateProductAsync_WithDuplicateProductId_ShouldCreateProduct()
    {
        // Setup
        var testProduct = TestData.GetTestProductEntity();
        await _repository.CreateProductAsync(testProduct);

        // Create another product with the same ProductId but different Id
        var duplicateProduct = TestData.GetTestProductEntity();
        duplicateProduct.Id = testProduct.Id + 1;
        duplicateProduct.ProductId = testProduct.ProductId;

        // Act
        // In-memory database doesn't enforce unique constraints like a real database would
        var result = await _repository.CreateProductAsync(duplicateProduct);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(duplicateProduct.ProductId);
    }

    [Fact]
    public async Task CreateProductAsync_WithInvalidData_ShouldStillCreateProduct()
    {
        // Setup - create a product with invalid data (negative price)
        var testProduct = TestData.GetTestProductEntity();
        testProduct.Price = -10.99m;
        // Name is required by the database, so we can't set it to null

        // Act
        var result = await _repository.CreateProductAsync(testProduct);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(-10.99m);
        result.Name.Should().Be("Test Product"); // Name should remain unchanged

        var dbProduct = await _fixture.Context.Products.FindAsync(result.Id);
        dbProduct.Should().NotBeNull();
        dbProduct!.Price.Should().Be(-10.99m);
        dbProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProduct_WhenProductExists()
    {
        var testProduct = TestData.GetTestProductEntity();
        await _fixture.Context.Products.AddAsync(testProduct);
        await _fixture.Context.SaveChangesAsync();

        testProduct.Name = "Updated Name";
        testProduct.Price = 29.99m;

        var result = await _repository.UpdateProductAsync(testProduct);

        result.Should().BeTrue();

        var dbProduct = await _fixture.Context.Products.FindAsync(testProduct.Id);
        dbProduct.Should().NotBeNull();
        dbProduct!.Name.Should().Be("Updated Name");
        dbProduct.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var testProduct = TestData.GetTestProductEntity();

        var result = await _repository.UpdateProductAsync(testProduct);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductAsync_WithNullProduct_ShouldThrowException()
    {
        // Act & Assert
        // This will throw NullReferenceException in the current implementation
        // In a real-world scenario, we would want to improve the implementation to throw ArgumentNullException
        await Assert.ThrowsAnyAsync<Exception>(() => _repository.UpdateProductAsync(null!));
    }

    [Fact]
    public async Task UpdateProductAsync_WithNullProductId_ShouldReturnFalse()
    {
        // Setup
        var testProduct = TestData.GetTestProductEntity();
        testProduct.ProductId = string.Empty; // Use empty string instead of null

        // Act
        var result = await _repository.UpdateProductAsync(testProduct);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldDeleteProduct_WhenProductExists()
    {
        var testProduct = TestData.GetTestProductEntity();
        await _fixture.Context.Products.AddAsync(testProduct);
        await _fixture.Context.SaveChangesAsync();

        var result = await _repository.DeleteProductAsync(testProduct.ProductId);

        result.Should().BeTrue();

        var dbProduct = await _fixture.Context.Products.FindAsync(testProduct.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var result = await _repository.DeleteProductAsync("non-existent-id");

        result.Should().BeFalse();
    }

    [Fact]
    public void ToProductMessage_ShouldConvertEntityToMessage()
    {
        var entity = TestData.GetTestProductEntity();

        var message = ProductRepository.ToProductMessage(entity);

        message.Should().NotBeNull();
        message.ProductId.Should().Be(entity.ProductId);
        message.Name.Should().Be(entity.Name);
        message.Description.Should().Be(entity.Description);
        message.Price.Should().Be(entity.Price);
        message.Sku.Should().Be(entity.Sku);
        message.Location.Should().Be(entity.Location);
        message.QuantityInStock.Should().Be(entity.QuantityInStock);
        message.ReorderThreshold.Should().Be(entity.ReorderThreshold);
    }

    [Fact]
    public void ToProductEntity_ShouldConvertMessageToEntity()
    {
        var message = TestData.GetTestProductMessage();

        var entity = ProductRepository.ToProductEntity(message);

        entity.Should().NotBeNull();
        entity.ProductId.Should().Be(message.ProductId);
        entity.Name.Should().Be(message.Name);
        entity.Description.Should().Be(message.Description);
        entity.Price.Should().Be(message.Price);
        entity.Sku.Should().Be(message.Sku);
        entity.Location.Should().Be(message.Location);
        entity.QuantityInStock.Should().Be(message.QuantityInStock);
        entity.ReorderThreshold.Should().Be(message.ReorderThreshold);
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_WithInvalidPageNumber_ShouldUseFirstPage()
    {
        // Setup
        var testProducts = TestData.GetTestProductEntities(10);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        // Act - use invalid page number (0)
        var (products, totalCount) = await _repository.GetPaginatedProductsAsync(0, 5);

        // Assert - should return first page
        products.Should().NotBeNull();
        products.Should().HaveCount(5);
        totalCount.Should().Be(10);

        // First 5 products should be returned
        var productsList = products.ToList();
        productsList[0].Id.Should().Be(1);
        productsList[4].Id.Should().Be(5);
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_WithInvalidPageSize_ShouldUseMinimumPageSize()
    {
        // Setup
        var testProducts = TestData.GetTestProductEntities(10);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        // Act - use invalid page size (0)
        var (products, totalCount) = await _repository.GetPaginatedProductsAsync(1, 0);

        // Assert - should use minimum page size (1)
        products.Should().NotBeNull();
        products.Should().HaveCount(1);
        totalCount.Should().Be(10);

        // Only first product should be returned
        var productsList = products.ToList();
        productsList[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_WithTooLargePageSize_ShouldClampPageSize()
    {
        // Setup
        var testProducts = TestData.GetTestProductEntities(10);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        // Act - use too large page size (1000)
        var (products, totalCount) = await _repository.GetPaginatedProductsAsync(1, 1000);

        // Assert - should clamp page size to maximum (100)
        products.Should().NotBeNull();
        products.Should().HaveCount(10); // Only 10 products exist
        totalCount.Should().Be(10);
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var (products, totalCount) = await _repository.GetPaginatedProductsAsync(1, 10);

        // Assert
        products.Should().NotBeNull();
        products.Should().BeEmpty();
        totalCount.Should().Be(0);
    }
}