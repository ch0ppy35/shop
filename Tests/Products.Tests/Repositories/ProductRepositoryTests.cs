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

        // Ensure the database is clean before each test
        _fixture.Context.Products.RemoveRange(_fixture.Context.Products);
        _fixture.Context.SaveChanges();
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        // Arrange
        var testProducts = TestData.GetTestProductEntities(5);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result.Select(p => p.ProductId).Should().BeEquivalentTo(testProducts.Select(p => p.ProductId));
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_ShouldReturnCorrectPage()
    {
        // Arrange
        var testProducts = TestData.GetTestProductEntities(20);
        await _fixture.Context.Products.AddRangeAsync(testProducts);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var (products, totalCount) = await _repository.GetPaginatedProductsAsync(2, 5);

        // Assert
        products.Should().NotBeNull();
        products.Should().HaveCount(5);
        totalCount.Should().Be(20);

        // Verify we got the second page (items 6-10)
        var productsList = products.ToList();
        productsList[0].ProductId.Should().Be("test-product-6");
        productsList[4].ProductId.Should().Be("test-product-10");
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var testProduct = TestData.GetTestProductEntity();
        await _fixture.Context.Products.AddAsync(testProduct);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProductByIdAsync(testProduct.ProductId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(testProduct.ProductId);
        result.Name.Should().Be(testProduct.Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Act
        var result = await _repository.GetProductByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct()
    {
        // Arrange
        var testProduct = TestData.GetTestProductEntity();

        // Act
        var result = await _repository.CreateProductAsync(testProduct);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(testProduct.ProductId);

        // Verify the product was added to the database
        var dbProduct = await _fixture.Context.Products.FindAsync(result.Id);
        dbProduct.Should().NotBeNull();
        dbProduct!.ProductId.Should().Be(testProduct.ProductId);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProduct_WhenProductExists()
    {
        // Arrange
        var testProduct = TestData.GetTestProductEntity();
        await _fixture.Context.Products.AddAsync(testProduct);
        await _fixture.Context.SaveChangesAsync();

        // Update the product
        testProduct.Name = "Updated Name";
        testProduct.Price = 29.99m;

        // Act
        var result = await _repository.UpdateProductAsync(testProduct);

        // Assert
        result.Should().BeTrue();

        // Verify the product was updated in the database
        var dbProduct = await _fixture.Context.Products.FindAsync(testProduct.Id);
        dbProduct.Should().NotBeNull();
        dbProduct!.Name.Should().Be("Updated Name");
        dbProduct.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        // Arrange
        var testProduct = TestData.GetTestProductEntity();

        // Act
        var result = await _repository.UpdateProductAsync(testProduct);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldDeleteProduct_WhenProductExists()
    {
        // Arrange
        var testProduct = TestData.GetTestProductEntity();
        await _fixture.Context.Products.AddAsync(testProduct);
        await _fixture.Context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteProductAsync(testProduct.ProductId);

        // Assert
        result.Should().BeTrue();

        // Verify the product was deleted from the database
        var dbProduct = await _fixture.Context.Products.FindAsync(testProduct.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        // Act
        var result = await _repository.DeleteProductAsync("non-existent-id");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToProductMessage_ShouldConvertEntityToMessage()
    {
        // Arrange
        var entity = TestData.GetTestProductEntity();

        // Act
        var message = ProductRepository.ToProductMessage(entity);

        // Assert
        message.Should().NotBeNull();
        message.ProductId.Should().Be(entity.ProductId);
        message.Name.Should().Be(entity.Name);
        message.Description.Should().Be(entity.Description);
        message.Price.Should().Be(entity.Price);
        message.Quantity.Should().Be(entity.Quantity);
        message.Sku.Should().Be(entity.Sku);
        message.Location.Should().Be(entity.Location);
        message.QuantityInStock.Should().Be(entity.QuantityInStock);
        message.ReorderThreshold.Should().Be(entity.ReorderThreshold);
    }

    [Fact]
    public void ToProductEntity_ShouldConvertMessageToEntity()
    {
        // Arrange
        var message = TestData.GetTestProductMessage();

        // Act
        var entity = ProductRepository.ToProductEntity(message);

        // Assert
        entity.Should().NotBeNull();
        entity.ProductId.Should().Be(message.ProductId);
        entity.Name.Should().Be(message.Name);
        entity.Description.Should().Be(message.Description);
        entity.Price.Should().Be(message.Price);
        entity.Quantity.Should().Be(message.Quantity);
        entity.Sku.Should().Be(message.Sku);
        entity.Location.Should().Be(message.Location);
        entity.QuantityInStock.Should().Be(message.QuantityInStock);
        entity.ReorderThreshold.Should().Be(message.ReorderThreshold);
    }
}
