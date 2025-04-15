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
}
