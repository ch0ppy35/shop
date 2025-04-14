using Common.Health;
using Common.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace Common.Tests.Health;

public class NatsHealthCheckTests
{
    private readonly Mock<ILogger<NatsHealthCheck>> _loggerMock;
    private readonly Mock<NatsService> _natsServiceMock;

    public NatsHealthCheckTests()
    {
        _loggerMock = new Mock<ILogger<NatsHealthCheck>>();
        _natsServiceMock = new Mock<NatsService>(
            new Mock<ILogger<NatsService>>().Object,
            new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object);
    }

    [Fact]
    public void Name_ShouldReturnNATS()
    {
        // Arrange
        var healthCheck = new NatsHealthCheck(_loggerMock.Object, _natsServiceMock.Object);

        // Act
        var result = healthCheck.Name;

        // Assert
        result.Should().Be("NATS");
    }

    [Fact]
    public void IsReady_ShouldReturnTrue_WhenNatsIsConnected()
    {
        // Arrange
        // Setup the IsConnected property to return true
        var isConnectedProperty = typeof(NatsService).GetProperty("IsConnected");
        _natsServiceMock.Setup(x => x.IsConnected).Returns(true);

        var healthCheck = new NatsHealthCheck(_loggerMock.Object, _natsServiceMock.Object);

        // Act
        var result = healthCheck.IsReady();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnFalse_WhenNatsIsNotConnected()
    {
        // Arrange
        // Setup the IsConnected property to return false
        _natsServiceMock.Setup(x => x.IsConnected).Returns(false);

        var healthCheck = new NatsHealthCheck(_loggerMock.Object, _natsServiceMock.Object);

        // Act
        var result = healthCheck.IsReady();

        // Assert
        result.Should().BeFalse();
    }
}
