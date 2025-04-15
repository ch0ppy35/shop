using Common.Health;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Common.Tests.Health;

public class HealthServiceTests
{
    private readonly Mock<ILogger<HealthService>> _loggerMock;

    public HealthServiceTests()
    {
        _loggerMock = new Mock<ILogger<HealthService>>();
    }

    [Fact]
    public void IsHealthy_ShouldAlwaysReturnTrue()
    {
        var service = new HealthService(_loggerMock.Object);

        var result = service.IsHealthy();

        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnTrue_WhenNoHealthChecksRegistered()
    {
        var service = new HealthService(_loggerMock.Object);

        var result = service.IsReady();

        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnTrue_WhenAllHealthChecksAreReady()
    {
        var service = new HealthService(_loggerMock.Object);

        var healthCheck1Mock = new Mock<IHealthCheck>();
        healthCheck1Mock.Setup(x => x.Name).Returns("Test1");
        healthCheck1Mock.Setup(x => x.IsReady()).Returns(true);

        var healthCheck2Mock = new Mock<IHealthCheck>();
        healthCheck2Mock.Setup(x => x.Name).Returns("Test2");
        healthCheck2Mock.Setup(x => x.IsReady()).Returns(true);

        service.RegisterHealthCheck(healthCheck1Mock.Object);
        service.RegisterHealthCheck(healthCheck2Mock.Object);

        var result = service.IsReady();

        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnFalse_WhenAnyHealthCheckIsNotReady()
    {
        var service = new HealthService(_loggerMock.Object);

        var healthCheck1Mock = new Mock<IHealthCheck>();
        healthCheck1Mock.Setup(x => x.Name).Returns("Test1");
        healthCheck1Mock.Setup(x => x.IsReady()).Returns(true);

        var healthCheck2Mock = new Mock<IHealthCheck>();
        healthCheck2Mock.Setup(x => x.Name).Returns("Test2");
        healthCheck2Mock.Setup(x => x.IsReady()).Returns(false);

        service.RegisterHealthCheck(healthCheck1Mock.Object);
        service.RegisterHealthCheck(healthCheck2Mock.Object);

        var result = service.IsReady();

        result.Should().BeFalse();
    }
}