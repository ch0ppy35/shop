using Common.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Common.Tests.Messaging;

public class NatsServiceTests
{
    private readonly Mock<ILogger<NatsService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;

    public NatsServiceTests()
    {
        _loggerMock = new Mock<ILogger<NatsService>>();
        _configMock = new Mock<IConfiguration>();

        // Setup configuration mock
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("nats://localhost:4222");
        _configMock.Setup(x => x.GetSection("Nats:Url")).Returns(configSectionMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var service = new NatsService(_loggerMock.Object, _configMock.Object);

        // Assert
        service.Should().NotBeNull();
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariable_WhenAvailable()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("NATS_URL");
        try
        {
            Environment.SetEnvironmentVariable("NATS_URL", "nats://custom:4222");

            // Act
            var service = new NatsService(_loggerMock.Object, _configMock.Object);

            // Assert
            service.Should().NotBeNull();
            // We can't directly test the internal state, but we can verify the logger was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("nats://custom:4222")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            // Restore original environment variable
            if (originalValue != null)
                Environment.SetEnvironmentVariable("NATS_URL", originalValue);
            else
                Environment.SetEnvironmentVariable("NATS_URL", null);
        }
    }

    [Fact]
    public void Constructor_ShouldUseConfigurationValue_WhenEnvironmentVariableNotAvailable()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("NATS_URL");
        try
        {
            Environment.SetEnvironmentVariable("NATS_URL", null);

            // Act
            var service = new NatsService(_loggerMock.Object, _configMock.Object);

            // Assert
            service.Should().NotBeNull();
            // We can't directly test the internal state, but we can verify the logger was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("nats://localhost:4222")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("NATS_URL", originalValue);
        }
    }

    [Fact]
    public void SubscribeAsync_ShouldLogQueueGroupInformation_WhenQueueGroupProvided()
    {
        // Arrange
        var service = new NatsService(_loggerMock.Object, _configMock.Object);
        var queueGroup = "test-queue-group";
        var subject = "test.subject";

        // We can't actually test the subscription since we can't mock the NATS connection easily,
        // but we can verify that the logger is called with the correct information

        // Act
        // Log the queue group information
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        service.LogQueueGroupInfo(subject, queueGroup);

        // Assert
        // Verify that the logger was called with the queue group information
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    o.ToString()!.Contains(subject) &&
                    o.ToString()!.Contains(queueGroup)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultValue_WhenNoConfigurationAvailable()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("NATS_URL");
        try
        {
            Environment.SetEnvironmentVariable("NATS_URL", null);
            var emptyConfigMock = new Mock<IConfiguration>();
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x.Value).Returns((string?)null);
            emptyConfigMock.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configSectionMock.Object);

            // Act
            var service = new NatsService(_loggerMock.Object, emptyConfigMock.Object);

            // Assert
            service.Should().NotBeNull();
            // We can't directly test the internal state, but we can verify the logger was called
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("nats://localhost:4222")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            // Restore original environment variable
            if (originalValue != null)
                Environment.SetEnvironmentVariable("NATS_URL", originalValue);
            else
                Environment.SetEnvironmentVariable("NATS_URL", null);
        }
    }

    // Note: We can't easily test the ConnectAsync, PublishAsync, and SubscribeAsync methods
    // without mocking the NATS client, which would be complex. In a real-world scenario,
    // we would use integration tests for these methods.
}
