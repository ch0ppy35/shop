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
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddCommonServices();
        var serviceProvider = services.BuildServiceProvider();

        var natsService = serviceProvider.GetService<INatsService>();
        natsService.Should().NotBeNull();

        var concreteNatsService = serviceProvider.GetService<NatsService>();
        concreteNatsService.Should().NotBeNull();

        var healthService = serviceProvider.GetService<HealthService>();
        healthService.Should().NotBeNull();

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();

        var exception = Record.Exception(() => logger.LogInformation("Test message"));
        exception.Should().BeNull();
    }

    [Fact]
    public void AddCommonServices_ShouldRegisterServicesAsSingletons()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddCommonServices();
        var serviceProvider = services.BuildServiceProvider();

        var natsService1 = serviceProvider.GetRequiredService<INatsService>();
        var natsService2 = serviceProvider.GetRequiredService<INatsService>();
        natsService1.Should().BeSameAs(natsService2);

        var concreteNatsService1 = serviceProvider.GetRequiredService<NatsService>();
        var concreteNatsService2 = serviceProvider.GetRequiredService<NatsService>();
        concreteNatsService1.Should().BeSameAs(concreteNatsService2);

        natsService1.Should().BeSameAs(concreteNatsService1);

        var healthService1 = serviceProvider.GetRequiredService<HealthService>();
        var healthService2 = serviceProvider.GetRequiredService<HealthService>();
        healthService1.Should().BeSameAs(healthService2);
    }
}
