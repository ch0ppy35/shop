using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;
using Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true)
    .Build();

// Register configuration
builder.Services.AddSingleton<IConfiguration>(configuration);

// Configure HttpClient with API base address from configuration
var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:8080";
Console.WriteLine($"Using API base URL from config: {apiBaseUrl}");

// Register HttpClient factory that can be dynamically configured
builder.Services.AddScoped(sp =>
{
    // Use the configuration from appsettings.json as default
    return new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
});

// Register services
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<ProductService>();

await builder.Build().RunAsync();
