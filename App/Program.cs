using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from appsettings.json
var http = new HttpClient();
http.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddScoped(sp => http);

// Add configuration services
builder.Services.AddScoped<IConfiguration>(sp =>
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true)
        .Build());

// Configure HttpClient with API base address from configuration
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiBaseUrl = config["ApiBaseUrl"] ?? "http://localhost:8080";
    return new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
});

await builder.Build().RunAsync();
