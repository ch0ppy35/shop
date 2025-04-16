using Common.Messaging;
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
            .WithImage("nats:2.11")
            .WithPortBinding(4222, true)
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

            // Now that we're connected, set up the subscriptions for all registered handlers
            Console.WriteLine("Setting up NATS subscriptions for all subjects...");
            await SetupNatsSubscriptions(natsService);
            Console.WriteLine("NATS subscriptions setup complete");
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
    /// Sets up NATS subscriptions for all subjects
    /// </summary>
    private async Task SetupNatsSubscriptions(TestableNatsService natsService)
    {
        // Create subscriptions for all the common subjects
        var subjects = new[]
        {
            "products.get",
            "products.create",
            "products.update",
            "products.delete",
            "products.getall",
            "products.inventory.update",
            "cart.additem",
            "cart.updateitem",
            "cart.removeitem",
            "cart.get",
            "cart.clear",
            "recommendations.get"
        };

        // For each subject, ensure we have a subscription
        foreach (var subject in subjects)
        {
            try
            {
                // The handler is already registered in TestableNatsService constructor
                // We just need to create the subscription
                if (natsService.IsConnected)
                {
                    Console.WriteLine($"Setting up subscription for subject: {subject}");
                    // The RegisterHandler method will create the subscription
                    // We're just calling it again to ensure the subscription is created
                    // since the handlers were registered before the connection was established
                    await natsService.ResubscribeHandler(subject);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up subscription for subject {subject}: {ex.Message}");
            }
        }
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