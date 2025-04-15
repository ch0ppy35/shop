using System.Text.Json;
using Common.Models;
using FluentAssertions;

namespace Common.Tests.Models;

public class MessagesTests
{
    [Fact]
    public void BaseMessage_ShouldInitializeProperties()
    {
        var message = new ProductMessage();

        message.Id.Should().NotBeEmpty();
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ProductMessage_ShouldSerializeAndDeserialize()
    {
        var message = new ProductMessage
        {
            ProductId = "123",
            Name = "Test Product",
            Description = "Test Description",
            Price = 10.99m,
            QuantityInStock = 5,
            OperationType = ProductOperationType.Create
        };

        var json = JsonSerializer.Serialize(message);
        var deserializedMessage = JsonSerializer.Deserialize<ProductMessage>(json);

        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.ProductId.Should().Be("123");
        deserializedMessage.Name.Should().Be("Test Product");
        deserializedMessage.Description.Should().Be("Test Description");
        deserializedMessage.Price.Should().Be(10.99m);
        deserializedMessage.QuantityInStock.Should().Be(5);
        deserializedMessage.OperationType.Should().Be(ProductOperationType.Create);
    }


    [Fact]
    public void ProductOperationType_ShouldSerializeAsString()
    {
        var message = new ProductMessage
        {
            OperationType = ProductOperationType.Create
        };

        var json = JsonSerializer.Serialize(message);

        json.Should().Contain("\"OperationType\":\"Create\"");
    }

    [Fact]
    public void ProductMessage_ShouldHaveDefaultPaginationValues()
    {
        var message = new ProductMessage();

        message.PageNumber.Should().Be(1, "Default page number should be 1");
        message.PageSize.Should().Be(10, "Default page size should be 10");
    }

    [Fact]
    public void ProductMessage_ShouldSerializeAndDeserializePaginationProperties()
    {
        var message = new ProductMessage
        {
            ProductId = "123",
            Name = "Test Product",
            PageNumber = 2,
            PageSize = 25
        };

        var json = JsonSerializer.Serialize(message);
        var deserializedMessage = JsonSerializer.Deserialize<ProductMessage>(json);

        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.PageNumber.Should().Be(2);
        deserializedMessage.PageSize.Should().Be(25);
    }
}