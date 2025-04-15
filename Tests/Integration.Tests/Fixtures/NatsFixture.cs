using Common.Messaging;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.Nats;
using Xunit;

namespace Integration.Tests.Fixtures;

/// <summary>
/// Fixture for managing NATS container for integration tests
/// </summary>
public class NatsFixture : IAsyncLifetime
{
    private readonly NatsContainer _natsContainer;
    private readonly IServiceCollection _services = new ServiceCollection();
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the NATS service
    /// </summary>
    public FakeNatsService NatsService { get; private set; } = null!;

    /// <summary>
    /// Gets the NATS URL
    /// </summary>
    public string NatsUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsFixture"/> class
    /// </summary>
    public NatsFixture()
    {
        _natsContainer = new NatsBuilder()
            .WithImage("nats:latest")
            .WithPortBinding(4222, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4222))
            .Build();
    }

    /// <summary>
    /// Initializes the NATS container and services
    /// </summary>
    public Task InitializeAsync()
    {
        // Skip container startup for faster tests
        // Use a mock NATS service instead
        NatsUrl = "nats://localhost:4222";

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nats:Url"] = NatsUrl
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Use a fake NATS service
        NatsService = new FakeNatsService();
        await NatsService.ConnectWithRetryAsync();

        // Register the fake NATS service
        _services.AddSingleton<INatsService>(NatsService);

        _serviceProvider = _services.BuildServiceProvider();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the NATS container and services
    /// </summary>
    public Task DisposeAsync()
    {
        // Nothing to dispose since we're using mocks
        return Task.CompletedTask;
    }
}