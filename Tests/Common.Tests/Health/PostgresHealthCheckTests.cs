using Common.Database;
using Common.Health;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Common.Tests.Health;

public class PostgresHealthCheckTests
{
    private readonly Mock<ILogger<PostgresHealthCheck>> _loggerMock;
    private readonly Mock<IDatabaseService> _databaseServiceMock;

    public PostgresHealthCheckTests()
    {
        _loggerMock = new Mock<ILogger<PostgresHealthCheck>>();
        _databaseServiceMock = new Mock<IDatabaseService>();
    }

    [Fact]
    public void Name_ShouldReturnPostgreSQL()
    {
        var healthCheck = new PostgresHealthCheck(_loggerMock.Object, _databaseServiceMock.Object);

        var result = healthCheck.Name;

        result.Should().Be("PostgreSQL");
    }

    [Fact]
    public void IsReady_ShouldReturnTrue_WhenDatabaseIsConnected()
    {
        _databaseServiceMock.Setup(x => x.TestConnectionAsync())
            .ReturnsAsync(true);

        var healthCheck = new PostgresHealthCheck(_loggerMock.Object, _databaseServiceMock.Object);

        var result = healthCheck.IsReady();

        result.Should().BeTrue();
    }

    [Fact]
    public void IsReady_ShouldReturnFalse_WhenDatabaseIsNotConnected()
    {
        _databaseServiceMock.Setup(x => x.TestConnectionAsync())
            .ReturnsAsync(false);

        var healthCheck = new PostgresHealthCheck(_loggerMock.Object, _databaseServiceMock.Object);

        var result = healthCheck.IsReady();

        result.Should().BeFalse();
    }

    [Fact]
    public void IsReady_ShouldReturnFalse_WhenDatabaseConnectionThrowsException()
    {
        _databaseServiceMock.Setup(x => x.TestConnectionAsync())
            .ThrowsAsync(new Exception("Test exception"));

        var healthCheck = new PostgresHealthCheck(_loggerMock.Object, _databaseServiceMock.Object);

        var result = healthCheck.IsReady();

        result.Should().BeFalse();
    }
}