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

        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("nats://localhost:4222");
        _configMock.Setup(x => x.GetSection("Nats:Url")).Returns(configSectionMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var service = new NatsService(_loggerMock.Object, _configMock.Object);

        service.Should().NotBeNull();
        service.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldUseEnvironmentVariable_WhenAvailable()
    {
        var originalValue = Environment.GetEnvironmentVariable("NATS_URL");
        try
        {
            Environment.SetEnvironmentVariable("NATS_URL", "nats://custom:4222");

            var service = new NatsService(_loggerMock.Object, _configMock.Object);

            service.Should().NotBeNull();
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
            if (originalValue != null)
                Environment.SetEnvironmentVariable("NATS_URL", originalValue);
            else
                Environment.SetEnvironmentVariable("NATS_URL", null);
        }
    }

    [Fact]
    public void Constructor_ShouldUseConfigurationValue_WhenEnvironmentVariableNotAvailable()
    {
        var originalValue = Environment.GetEnvironmentVariable("NATS_URL");
        try
        {
            Environment.SetEnvironmentVariable("NATS_URL", null);

            var service = new NatsService(_loggerMock.Object, _configMock.Object);

            service.Should().NotBeNull();
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
            Environment.SetEnvironmentVariable("NATS_URL", originalValue);
        }
    }

    [Fact]
    public void SubscribeAsync_ShouldLogQueueGroupInformation_WhenQueueGroupProvided()
    {
        var service = new NatsService(_loggerMock.Object, _configMock.Object);
        var queueGroup = "test-queue-group";
        var subject = "test.subject";


        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        service.LogQueueGroupInfo(subject, queueGroup);

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
        var originalValue = Environment.GetEnvironmentVariable("NATS_URL");
        try
        {
            Environment.SetEnvironmentVariable("NATS_URL", null);
            var emptyConfigMock = new Mock<IConfiguration>();
            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x.Value).Returns((string?)null);
            emptyConfigMock.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configSectionMock.Object);

            var service = new NatsService(_loggerMock.Object, emptyConfigMock.Object);

            service.Should().NotBeNull();
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
            if (originalValue != null)
                Environment.SetEnvironmentVariable("NATS_URL", originalValue);
            else
                Environment.SetEnvironmentVariable("NATS_URL", null);
        }
    }
}