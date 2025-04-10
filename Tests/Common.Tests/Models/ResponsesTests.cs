using System.Text.Json;
using Common.Models;
using FluentAssertions;

namespace Common.Tests.Models;

public class ResponsesTests
{
    [Fact]
    public void BaseResponse_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var response = new BaseResponse
        {
            Success = true,
            Message = "Operation successful",
            Error = null
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserializedResponse = JsonSerializer.Deserialize<BaseResponse>(json);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Success.Should().BeTrue();
        deserializedResponse.Message.Should().Be("Operation successful");
        deserializedResponse.Error.Should().BeNull();
    }

    [Fact]
    public void ProductResponse_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var product = new ProductMessage
        {
            ProductId = "123",
            Name = "Test Product",
            Description = "Test Description",
            Price = 10.99m,
            Quantity = 5,
            OperationType = ProductOperationType.Create
        };

        var response = new ProductResponse
        {
            Success = true,
            Message = "Product retrieved successfully",
            Error = null,
            Product = product
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserializedResponse = JsonSerializer.Deserialize<ProductResponse>(json);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Success.Should().BeTrue();
        deserializedResponse.Message.Should().Be("Product retrieved successfully");
        deserializedResponse.Error.Should().BeNull();
        deserializedResponse.Product.Should().NotBeNull();
        deserializedResponse.Product!.ProductId.Should().Be("123");
        deserializedResponse.Product.Name.Should().Be("Test Product");
        deserializedResponse.Product.Description.Should().Be("Test Description");
        deserializedResponse.Product.Price.Should().Be(10.99m);
        deserializedResponse.Product.Quantity.Should().Be(5);
        deserializedResponse.Product.OperationType.Should().Be(ProductOperationType.Create);
    }

    [Fact]
    public void ProductListResponse_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var products = new List<ProductMessage>
        {
            new ProductMessage
            {
                ProductId = "123",
                Name = "Test Product 1",
                Description = "Test Description 1",
                Price = 10.99m,
                Quantity = 5,
                OperationType = ProductOperationType.Create
            },
            new ProductMessage
            {
                ProductId = "456",
                Name = "Test Product 2",
                Description = "Test Description 2",
                Price = 20.99m,
                Quantity = 10,
                OperationType = ProductOperationType.Create
            }
        };

        var response = new ProductListResponse
        {
            Success = true,
            Message = "Products retrieved successfully",
            Error = null,
            Products = products
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserializedResponse = JsonSerializer.Deserialize<ProductListResponse>(json);

        // Assert
        deserializedResponse.Should().NotBeNull();
        deserializedResponse!.Success.Should().BeTrue();
        deserializedResponse.Message.Should().Be("Products retrieved successfully");
        deserializedResponse.Error.Should().BeNull();
        deserializedResponse.Products.Should().NotBeNull();
        deserializedResponse.Products!.Count.Should().Be(2);
        deserializedResponse.Products[0].ProductId.Should().Be("123");
        deserializedResponse.Products[0].Name.Should().Be("Test Product 1");
        deserializedResponse.Products[1].ProductId.Should().Be("456");
        deserializedResponse.Products[1].Name.Should().Be("Test Product 2");
    }


}
