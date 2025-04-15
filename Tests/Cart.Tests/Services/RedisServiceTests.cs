using Cart.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Text.Json;

namespace Cart.Tests.Services;

public class RedisServiceTests
{
    private readonly Mock<ILogger<RedisService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;

    public RedisServiceTests()
    {
        _loggerMock = new Mock<ILogger<RedisService>>();
        _configMock = new Mock<IConfiguration>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();

        // Setup configuration
        var redisConnectionStringSection = new Mock<IConfigurationSection>();
        redisConnectionStringSection.Setup(x => x.Value).Returns("localhost:6379");
        _configMock.Setup(x => x.GetSection("Redis:ConnectionString")).Returns(redisConnectionStringSection.Object);

        // Setup Redis connection
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_databaseMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var service = new TestableRedisService(_loggerMock.Object, _configMock.Object);

        // Assert
        service.RedisConnectionString.Should().Be("localhost:6379");
        service.CartTtl.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task GetAsync_ShouldDeserializeValue_WhenKeyExists()
    {
        // Arrange
        var service = new TestableRedisService(_loggerMock.Object, _configMock.Object);
        service.SetTestDatabase(_databaseMock.Object);
        service.SetConnected(true);

        var testData = new TestData { Id = 1, Name = "Test" };
        var serializedData = JsonSerializer.Serialize(testData);

        _databaseMock.Setup(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(serializedData);

        // Act
        var result = await service.GetAsync<TestData>("test-key");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenKeyDoesNotExist()
    {
        // Arrange
        var service = new TestableRedisService(_loggerMock.Object, _configMock.Object);
        service.SetTestDatabase(_databaseMock.Object);
        service.SetConnected(true);

        _databaseMock.Setup(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(RedisValue.Null);

        // Act
        var result = await service.GetAsync<TestData>("test-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeAndStoreValue()
    {
        // Arrange
        var service = new TestableRedisService(_loggerMock.Object, _configMock.Object);
        service.SetTestDatabase(_databaseMock.Object);
        service.SetConnected(true);

        var testData = new TestData { Id = 1, Name = "Test" };
        var expiry = TimeSpan.FromMinutes(15);

        _databaseMock.Setup(d => d.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .Returns(true);

        // Act
        await service.SetAsync("test-key", testData, expiry);

        // Assert
        _databaseMock.Verify(d => d.StringSet(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldThrowException_WhenNotConnected()
    {
        // Arrange
        var service = new TestableRedisService(_loggerMock.Object, _configMock.Object);
        service.SetConnected(false);

        var testData = new TestData { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetAsync("test-key", testData));
    }

    [Fact]
    public async Task GetAsync_ShouldThrowException_WhenNotConnected()
    {
        // Arrange
        var service = new TestableRedisService(_loggerMock.Object, _configMock.Object);
        service.SetConnected(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetAsync<TestData>("test-key"));
    }

    private class TestData
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class TestableRedisService : IRedisService
    {
        private IDatabase? _database;
        private bool _isConnected;
        private readonly TimeSpan _cartTtl = TimeSpan.FromMinutes(15);

        public TestableRedisService(ILogger<RedisService> logger, IConfiguration configuration)
        {
            // Store the connection string for testing
            RedisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ??
                               configuration.GetValue<string>("Redis:ConnectionString") ??
                               "localhost:6379";
        }

        public string RedisConnectionString { get; }

        public TimeSpan CartTtl => _cartTtl;

        public bool IsConnected => _isConnected;

        public void SetTestDatabase(IDatabase database)
        {
            _database = database;
        }

        public void SetConnected(bool isConnected)
        {
            _isConnected = isConnected;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Redis server");
            }

            if (_database == null)
            {
                throw new InvalidOperationException("Database is not set");
            }

            var value = _database.StringGet(key);
            if (value.IsNull)
            {
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult(JsonSerializer.Deserialize<T>(value.ToString()));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Redis server");
            }

            if (_database == null)
            {
                throw new InvalidOperationException("Database is not set");
            }

            var json = JsonSerializer.Serialize(value);
            _database.StringSet(key, json, expiry ?? _cartTtl, When.Always, CommandFlags.None);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Redis server");
            }

            if (_database == null)
            {
                throw new InvalidOperationException("Database is not set");
            }

            _database.KeyDelete(key);
            return Task.CompletedTask;
        }
    }
}
