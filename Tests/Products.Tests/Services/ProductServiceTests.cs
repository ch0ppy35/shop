using Common.Database.Models;
using Common.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Products.Repositories;
using Products.Services;
using Products.Tests.TestHelpers;
using System.Data.Common;
using System.Net.Sockets;

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
    public async Task GetPaginatedProductsAsync_WithDifferentPageSizes_ShouldReturnCorrectPages()
    {
        // Setup for page size 10
        var testEntities10 = TestData.GetTestProductEntities(10);
        int totalCount = 25;
        _repositoryMock.Setup(r => r.GetPaginatedProductsAsync(1, 10))
            .ReturnsAsync((testEntities10, totalCount));

        // Act
        var (products10, count10, pages10) = await _service.GetPaginatedProductsAsync(1, 10);

        // Assert
        products10.Should().HaveCount(10);
        count10.Should().Be(25);
        pages10.Should().Be(3); // 25 items with 10 per page = 3 pages

        // Setup for page size 25
        var testEntities25 = TestData.GetTestProductEntities(25);
        _repositoryMock.Setup(r => r.GetPaginatedProductsAsync(1, 25))
            .ReturnsAsync((testEntities25, totalCount));

        // Act
        var (products25, count25, pages25) = await _service.GetPaginatedProductsAsync(1, 25);

        // Assert
        products25.Should().HaveCount(25);
        count25.Should().Be(25);
        pages25.Should().Be(1); // 25 items with 25 per page = 1 page
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_WithPageBeyondAvailableData_ShouldReturnEmptyList()
    {
        // Setup for page beyond available data
        var emptyList = new List<ProductEntity>();
        int totalCount = 10;
        _repositoryMock.Setup(r => r.GetPaginatedProductsAsync(3, 5))
            .ReturnsAsync((emptyList, totalCount));

        // Act
        var (products, count, pages) = await _service.GetPaginatedProductsAsync(3, 5);

        // Assert
        products.Should().NotBeNull();
        products.Should().BeEmpty();
        count.Should().Be(10);
        pages.Should().Be(2); // 10 items with 5 per page = 2 pages
    }

    [Fact]
    public async Task GetPaginatedProductsAsync_WithEmptyDatabase_ShouldReturnEmptyListAndZeroCounts()
    {
        // Setup for empty database
        var emptyList = new List<ProductEntity>();
        int totalCount = 0;
        _repositoryMock.Setup(r => r.GetPaginatedProductsAsync(1, 10))
            .ReturnsAsync((emptyList, totalCount));

        // Act
        var (products, count, pages) = await _service.GetPaginatedProductsAsync(1, 10);

        // Assert
        products.Should().NotBeNull();
        products.Should().BeEmpty();
        count.Should().Be(0);
        pages.Should().Be(0); // 0 items = 0 pages
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
        _repositoryMock.Verify(r => r.CreateProductAsync(It.Is<ProductEntity>(e => !string.IsNullOrEmpty(e.ProductId))),
            Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldHandleDbException()
    {
        var testMessage = TestData.GetTestProductMessage();

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ThrowsAsync(new DbUpdateException("Database error", new Exception("Inner exception")));

        // The service should propagate the exception since it doesn't catch database exceptions
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.CreateProductAsync(testMessage));

        _repositoryMock.Verify(r => r.CreateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldHandleGenericException()
    {
        var testMessage = TestData.GetTestProductMessage();

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ThrowsAsync(new InvalidOperationException("Some unexpected error"));

        // The service should propagate the exception since it doesn't catch generic exceptions
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateProductAsync(testMessage));

        _repositoryMock.Verify(r => r.CreateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithNullName_ShouldStillCreateProduct()
    {
        // Setup
        var testMessage = TestData.GetTestProductMessage();
        testMessage.Name = null; // Null name

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync((ProductEntity entity) => entity);

        // Act
        var result = await _service.CreateProductAsync(testMessage);

        // Assert
        result.Should().NotBeNull();
        // In .NET, string properties are initialized to empty string when set to null
        result.Name.Should().BeEmpty();
        _repositoryMock.Verify(r => r.CreateProductAsync(It.Is<ProductEntity>(e => e.Name == string.Empty)),
            Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithNegativePrice_ShouldStillCreateProduct()
    {
        // Setup
        var testMessage = TestData.GetTestProductMessage();
        testMessage.Price = -10.99m; // Negative price

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync((ProductEntity entity) => entity);

        // Act
        var result = await _service.CreateProductAsync(testMessage);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(-10.99m);
        _repositoryMock.Verify(r => r.CreateProductAsync(It.Is<ProductEntity>(e => e.Price == -10.99m)), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithNetworkFailure_ShouldPropagateException()
    {
        // Setup
        var testMessage = TestData.GetTestProductMessage();

        _repositoryMock.Setup(r => r.CreateProductAsync(It.IsAny<ProductEntity>()))
            .ThrowsAsync(new SocketException((int)SocketError.ConnectionRefused));

        // Act & Assert
        await Assert.ThrowsAsync<SocketException>(() => _service.CreateProductAsync(testMessage));

        _repositoryMock.Verify(r => r.CreateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
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
    public async Task UpdateProductAsync_WithDbUpdateException_ShouldPropagateException()
    {
        // Setup
        var testMessage = TestData.GetTestProductMessage();

        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ThrowsAsync(new DbUpdateException("Database update error", new Exception("Inner exception")));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.UpdateProductAsync(testMessage));

        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
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
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.Is<ProductEntity>(e => e.QuantityInStock == 75)),
            Times.Once);
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

    [Theory]
    [InlineData("test-id-1", -10)]
    [InlineData("test-id-2", -1)]
    public async Task UpdateInventoryAsync_WithNegativeQuantity_ShouldStillUpdate(string id, int quantity)
    {
        // Setup
        var existingProduct = TestData.GetTestProductEntity(id);
        existingProduct.QuantityInStock = 50; // Initial quantity

        _repositoryMock.Setup(r => r.GetProductByIdAsync(id))
            .ReturnsAsync(existingProduct);
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateInventoryAsync(id, quantity);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(id), Times.Once);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.Is<ProductEntity>(e =>
            e.ProductId == id && e.QuantityInStock == quantity)), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_WithZeroQuantity_ShouldUpdate()
    {
        // Setup
        var id = "test-product-id";
        var existingProduct = TestData.GetTestProductEntity(id);
        existingProduct.QuantityInStock = 50; // Initial quantity

        _repositoryMock.Setup(r => r.GetProductByIdAsync(id))
            .ReturnsAsync(existingProduct);
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateInventoryAsync(id, 0);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(id), Times.Once);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.Is<ProductEntity>(e =>
            e.ProductId == id && e.QuantityInStock == 0)), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_WithExtremelyLargeQuantity_ShouldUpdate()
    {
        // Setup
        var id = "test-product-id";
        var existingProduct = TestData.GetTestProductEntity(id);
        existingProduct.QuantityInStock = 50; // Initial quantity
        var extremelyLargeQuantity = int.MaxValue;

        _repositoryMock.Setup(r => r.GetProductByIdAsync(id))
            .ReturnsAsync(existingProduct);
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateInventoryAsync(id, extremelyLargeQuantity);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(id), Times.Once);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.Is<ProductEntity>(e =>
            e.ProductId == id && e.QuantityInStock == extremelyLargeQuantity)), Times.Once);
    }

    [Fact]
    public async Task UpdateInventoryAsync_WithNullId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.UpdateInventoryAsync(null!, 10);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInventoryAsync_WithEmptyId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.UpdateInventoryAsync("", 10);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.GetProductByIdAsync(It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInventoryAsync_WithDbUpdateException_ShouldPropagateException()
    {
        // Setup
        var id = "test-product-id";
        var existingProduct = TestData.GetTestProductEntity(id);

        _repositoryMock.Setup(r => r.GetProductByIdAsync(id))
            .ReturnsAsync(existingProduct);
        _repositoryMock.Setup(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()))
            .ThrowsAsync(new DbUpdateException("Database update error", new Exception("Inner exception")));

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.UpdateInventoryAsync(id, 10));

        _repositoryMock.Verify(r => r.GetProductByIdAsync(id), Times.Once);
        _repositoryMock.Verify(r => r.UpdateProductAsync(It.IsAny<ProductEntity>()), Times.Once);
    }
}