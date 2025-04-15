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
    public TestableNatsService NatsService { get; private set; } = null!;

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
            .WithImage("nats:2.11-scratch")
            .WithPortBinding(4222, true)
            // .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4222))
            .Build();
    }

    /// <summary>
    /// Initializes the NATS container and services
    /// </summary>
    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting NATS container...");
        // Start the NATS container
        await _natsContainer.StartAsync();
        NatsUrl = _natsContainer.GetConnectionString();
        Console.WriteLine($"NATS container started at {NatsUrl}");

        // Configure services
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nats:Url"] = NatsUrl
            })
            .Build();

        _services.AddSingleton<IConfiguration>(configuration);
        _services.AddLogging(builder => builder.AddConsole());

        // Create and register the testable NATS service
        var logger = _services.BuildServiceProvider().GetRequiredService<ILogger<NatsService>>();
        Console.WriteLine("Creating TestableNatsService...");
        var natsService = new TestableNatsService(logger, configuration);
        Console.WriteLine("Connecting to NATS...");
        try
        {
            await natsService.ConnectWithRetryAsync(5);
            Console.WriteLine("Successfully connected to NATS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to NATS: {ex.Message}");
            throw;
        }

        // Store the service for test access
        NatsService = natsService;

        // Register the NATS service
        _services.AddSingleton<INatsService>(NatsService);

        _serviceProvider = _services.BuildServiceProvider();
    }

    /// <summary>
    /// Disposes the NATS container and services
    /// </summary>
    public async Task DisposeAsync()
    {
        if (NatsService != null)
        {
            await NatsService.DisposeAsync();
        }

        await _natsContainer.DisposeAsync();
    }
}