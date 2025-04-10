using Common.Health;
using Common.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Common.Tests.Health;

public class HealthServiceTests
{
    private readonly Mock<ILogger<HealthService>> _loggerMock;
    private readonly NatsService _natsService;

    public HealthServiceTests()
    {
        _loggerMock = new Mock<ILogger<HealthService>>();

        // Create a real NatsService with a mock configuration
        var configMock = new Mock<IConfiguration>();
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("nats://localhost:4222");
        configMock.Setup(x => x.GetSection("Nats:Url")).Returns(configSectionMock.Object);

        _natsService = new NatsService(new Mock<ILogger<NatsService>>().Object, configMock.Object);
    }

    [Fact]
    public void IsHealthy_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var service = new HealthService(_loggerMock.Object, _natsService);

        // Act
        var result = service.IsHealthy();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnTrue_WhenNatsIsConnected()
    {
        // Arrange
        // Use reflection to set the IsConnected property
        var field = typeof(NatsService).GetField("_isConnected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_natsService, true);

        var service = new HealthService(_loggerMock.Object, _natsService);

        // Act
        var result = service.IsReady();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnFalse_WhenNatsIsNotConnected()
    {
        // Arrange
        // Use reflection to set the IsConnected property
        var field = typeof(NatsService).GetField("_isConnected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_natsService, false);

        var service = new HealthService(_loggerMock.Object, _natsService);

        // Act
        var result = service.IsReady();

        // Assert
        result.Should().BeFalse();
    }
}
