using Common.Database.Models;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Products.Repositories;
using Products.Services;
using Products.Tests.TestHelpers;

namespace Products.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _loggerMock = new Mock<ILogger<ProductService>>();
        _repositoryMock = new Mock<IProductRepository>();

        _service = new ProductService(_loggerMock.Object, _repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllProductsAsync_ShouldReturnAllProducts()
    {
        var testEntities = TestData.GetTestProductEntities(3);
        _repositoryMock.Setup(r => r.GetAllProductsAsync())
            .ReturnsAsync(testEntities);

        var result = await _service.GetAllProductsAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        _repositoryMock.Verify(r => r.GetAllProductsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_ShouldReturnPaginatedProducts()
    {
        var testEntities = TestData.GetTestProductEntities(5);
        int totalCount = 20;
        _repositoryMock.Setup(r => r.GetPaginatedProductsAsync(2, 5))
            .ReturnsAsync((testEntities, totalCount));

        var (products, count, pages) = await _service.GetPaginatedProductsAsync(2, 5);

        products.Should().NotBeNull();
        products.Should().HaveCount(5);
        count.Should().Be(20);
        pages.Should().Be(4); // 20 items with 5 per page = 4 pages
        _repositoryMock.Verify(r => r.GetPaginatedProductsAsync(2, 5), Times.Once);
    }

    [Fact]
    public async Task GetProductAsync_ShouldReturnProduct_WhenProductExists()
    {
        var testEntity = TestData.GetTestProductEntity();
        _repositoryMock.Setup(r => r.GetProductByIdAsync(testEntity.ProductId))
            .ReturnsAsync(testEntity);

        var result = await _service.GetProductAsync(testEntity.ProductId);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(testEntity.ProductId);
        result.Name.Should().Be(testEntity.Name);
        _repositoryMock.Verify(r => r.GetProductByIdAsync(testEntity.ProductId), Times.Once);
    }

    [Fact]
    public async Task GetProductAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        _repositoryMock.Setup(r => r.GetProductByIdAsync("non-existent-id"))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _service.GetProductAsync("non-existent-id");

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetProductByIdAsync("non-existent-id"), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct()
    {
        var testMessage = TestData.GetTestProductMessage();
        var testEntity = ProductRepository.ToProductEntity(testMessage);

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(testEntity);

        var result = await _service.CreateProductAsync(testMessage);

        result.Should().NotBeNull();
        result.ProductId.Should().Be(testMessage.ProductId);
        result.Name.Should().Be(testMessage.Name);
        result.OperationType.Should().Be(ProductOperationType.Create);
        _repositoryMock.Verify(r => r.CreateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldGenerateProductId_WhenIdIsEmpty()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.ProductId = null; // Empty product ID

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync((ProductEntity entity) => entity); // Return the entity that was passed in

        var result = await _service.CreateProductAsync(testMessage);

        result.Should().NotBeNull();
        result.ProductId.Should().NotBeNullOrEmpty();
        result.Name.Should().Be(testMessage.Name);
        _repositoryMock.Verify(r => r.CreateProductAsync(It.Is<ProductEntity>(e => !string.IsNullOrEmpty(e.ProductId))), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProduct_WhenProductExists()
    {
        var testMessage = TestData.GetTestProductMessage();
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateProductAsync(testMessage);

        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var testMessage = TestData.GetTestProductMessage();
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(false);

        var result = await _service.UpdateProductAsync(testMessage);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnFalse_WhenProductIdIsEmpty()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.ProductId = null; // Empty product ID

        var result = await _service.UpdateProductAsync(testMessage);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Never);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldDeleteProduct_WhenProductExists()
    {
        _repositoryMock.Setup(r => r.DeleteProductAsync("test-id"))
            .ReturnsAsync(true);

        var result = await _service.DeleteProductAsync("test-id");

        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteProductAsync("test-id"), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        _repositoryMock.Setup(r => r.DeleteProductAsync("non-existent-id"))
            .ReturnsAsync(false);

        var result = await _service.DeleteProductAsync("non-existent-id");

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteProductAsync("non-existent-id"), Times.Once);
    }

    [Fact]
    public async Task GetInventoryAsync_ShouldReturnInventory_WhenProductExists()
    {
        var testEntity = TestData.GetTestProductEntity();
        _repositoryMock.Setup(r => r.GetProductByIdAsync(testEntity.ProductId))
            .ReturnsAsync(testEntity);

        var result = await _service.GetInventoryAsync(testEntity.ProductId);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(testEntity.ProductId);
        result.QuantityInStock.Should().Be(testEntity.QuantityInStock);
        result.OperationType.Should().Be(ProductOperationType.GetInventory);
        _repositoryMock.Verify(r => r.GetProductByIdAsync(testEntity.ProductId), Times.Once);
    }

    [Fact]
    public async Task GetInventoryAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        _repositoryMock.Setup(r => r.GetProductByIdAsync("non-existent-id"))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _service.GetInventoryAsync("non-existent-id");

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetProductByIdAsync("non-existent-id"), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_ShouldUpdateInventory_WhenProductExists()
    {
        var testEntity = TestData.GetTestProductEntity();
        var testMessage = TestData.GetTestProductMessage();
        testMessage.QuantityInStock = 75; // New inventory level

        _repositoryMock.Setup(r => r.GetProductByIdAsync(testMessage.ProductId!))
            .ReturnsAsync(testEntity);
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateInventoryAsync(testMessage);

        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(testMessage.ProductId!), Times.Once);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.Is<ProductEntity>(e => e.QuantityInStock == 75)), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        var testMessage = TestData.GetTestProductMessage();

        _repositoryMock.Setup(r => r.GetProductByIdAsync(testMessage.ProductId!))
            .ReturnsAsync((ProductEntity?)null);

        var result = await _service.UpdateInventoryAsync(testMessage);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(testMessage.ProductId!), Times.Once);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInventoryAsync_ShouldReturnFalse_WhenProductIdIsEmpty()
    {
        var testMessage = TestData.GetTestProductMessage();
        testMessage.ProductId = null; // Empty product ID

        var result = await _service.UpdateInventoryAsync(testMessage);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Never);
    }
}
