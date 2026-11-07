using Frontend;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true)
    .Build();

builder.Services.AddSingleton<IConfiguration>(configuration);

var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:8080";
Console.WriteLine($"Using API base URL from config: {apiBaseUrl}");

builder.Services.AddScoped<IJavaScriptInterop, JavaScriptInterop>();

builder.Services.AddScoped<SessionService>();

builder.Services.AddSingleton<RequestTimeline>();

builder.Services.AddScoped<SessionHttpMessageHandler>();

builder.Services.AddScoped(sp =>
{
    var sessionHandler = sp.GetRequiredService<SessionHttpMessageHandler>();

    sessionHandler.InnerHandler = new HttpClientHandler();

    var client = new HttpClient(sessionHandler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };

    return client;
});

builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<CartState>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<IConfirmService, ConfirmService>();

await builder.Build().RunAsync();