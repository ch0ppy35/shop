using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Frontend;
using Frontend.Services;
using MudBlazor;
using MudBlazor.Services;

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

builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<IConfirmService, ConfirmService>();

await builder.Build().RunAsync();
