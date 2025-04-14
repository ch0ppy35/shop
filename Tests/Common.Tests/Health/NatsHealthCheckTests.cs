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
    private readonly Mock<INatsService> _natsServiceMock;

    public NatsHealthCheckTests()
    {
        _loggerMock = new Mock<ILogger<NatsHealthCheck>>();
        _natsServiceMock = new Mock<INatsService>();
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
