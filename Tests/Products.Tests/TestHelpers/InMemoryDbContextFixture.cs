using Common.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Products.Tests.TestHelpers;

/// <summary>
/// Fixture for creating an in-memory database context for testing
/// </summary>
public class InMemoryDbContextFixture : IDisposable
{
    private readonly string _databaseName;
    private readonly DbContextOptions<ProductDbContext> _options;
    private readonly Mock<ILogger<ProductDbContext>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;

    /// <summary>
    /// Gets the database context
    /// </summary>
    public ProductDbContext Context { get; }

    /// <summary>
    /// Gets the logger mock
    /// </summary>
    public Mock<ILogger<ProductDbContext>> LoggerMock => _loggerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDbContextFixture"/> class.
    /// </summary>
    public InMemoryDbContextFixture()
    {
        _databaseName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        _loggerMock = new Mock<ILogger<ProductDbContext>>();
        _configMock = new Mock<IConfiguration>();

        var connectionString = "Host=localhost;Database=test;Username=test;Password=test";

        var connectionStringSection = new Mock<IConfigurationSection>();
        connectionStringSection.Setup(x => x.Value).Returns(connectionString);

        var connectionStringsSection = new Mock<IConfigurationSection>();
        connectionStringsSection.Setup(x => x.GetSection("DefaultConnection")).Returns(connectionStringSection.Object);

        _configMock.Setup(x => x.GetSection("ConnectionStrings")).Returns(connectionStringsSection.Object);

        Context = new ProductDbContext(_options, _loggerMock.Object, _configMock.Object);
    }

    /// <summary>
    /// Creates a new instance of the database context
    /// </summary>
    public ProductDbContext CreateContext()
    {
        return new ProductDbContext(_options, _loggerMock.Object, _configMock.Object);
    }

    /// <summary>
    /// Disposes the database context
    /// </summary>
    public void Dispose()
    {
        Context.Dispose();
    }
}
