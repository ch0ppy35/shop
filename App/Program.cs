using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;
using Frontend.Services;
using MudBlazor;
using MudBlazor.Services;

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

// Register JavaScript interop service
builder.Services.AddScoped<IJavaScriptInterop, JavaScriptInterop>();

// Register session service
builder.Services.AddScoped<SessionService>();

// Register session HTTP message handler
builder.Services.AddScoped<SessionHttpMessageHandler>();

// Register HttpClient with the session message handler
builder.Services.AddScoped(sp =>
{
    // Get the session message handler
    var sessionHandler = sp.GetRequiredService<SessionHttpMessageHandler>();

    // Set the inner handler
    sessionHandler.InnerHandler = new HttpClientHandler();

    // Create the HttpClient with the handler
    var client = new HttpClient(sessionHandler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };

    return client;
});

// Register MudBlazor services
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// Register services
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<IConfirmService, ConfirmService>();

await builder.Build().RunAsync();
