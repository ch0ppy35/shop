using System.Text.Json;
using Common.Models;
using FluentAssertions;

namespace Common.Tests.Models;

public class MessagesTests
{
    [Fact]
    public void BaseMessage_ShouldInitializeProperties()
    {
        // Arrange & Act
        var message = new ProductMessage();

        // Assert
        message.Id.Should().NotBeEmpty();
        message.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ProductMessage_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var message = new ProductMessage
        {
            ProductId = "123",
            Name = "Test Product",
            Description = "Test Description",
            Price = 10.99m,
            Quantity = 5,
            OperationType = ProductOperationType.Create
        };

        // Act
        var json = JsonSerializer.Serialize(message);
        var deserializedMessage = JsonSerializer.Deserialize<ProductMessage>(json);

        // Assert
        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.ProductId.Should().Be("123");
        deserializedMessage.Name.Should().Be("Test Product");
        deserializedMessage.Description.Should().Be("Test Description");
        deserializedMessage.Price.Should().Be(10.99m);
        deserializedMessage.Quantity.Should().Be(5);
        deserializedMessage.OperationType.Should().Be(ProductOperationType.Create);
    }



    [Fact]
    public void ProductOperationType_ShouldSerializeAsString()
    {
        // Arrange
        var message = new ProductMessage
        {
            OperationType = ProductOperationType.Create
        };

        // Act
        var json = JsonSerializer.Serialize(message);

        // Assert
        json.Should().Contain("\"OperationType\":\"Create\"");
    }


}
