using Common.Health;
using Common.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Tests;

public class ServiceExtensionsTests
{
    [Fact]
    public void AddCommonServices_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add configuration
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddCommonServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Verify NatsService is registered
        var natsService = serviceProvider.GetService<NatsService>();
        natsService.Should().NotBeNull();

        // Verify HealthService is registered
        var healthService = serviceProvider.GetService<HealthService>();
        healthService.Should().NotBeNull();

        // Verify logging is configured
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        // Create a logger and verify it works
        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();

        // We can't directly check if it's using JsonLogger, but we can verify it doesn't throw
        var exception = Record.Exception(() => logger.LogInformation("Test message"));
        exception.Should().BeNull();
    }

    [Fact]
    public void AddCommonServices_ShouldRegisterServicesAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();

        // Add configuration
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddCommonServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Get services twice and verify they are the same instance
        var natsService1 = serviceProvider.GetRequiredService<NatsService>();
        var natsService2 = serviceProvider.GetRequiredService<NatsService>();
        natsService1.Should().BeSameAs(natsService2);

        var healthService1 = serviceProvider.GetRequiredService<HealthService>();
        var healthService2 = serviceProvider.GetRequiredService<HealthService>();
        healthService1.Should().BeSameAs(healthService2);
    }
}
