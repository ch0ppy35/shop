using Microsoft.JSInterop;

namespace Frontend.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public class ConfigurationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class
    /// </summary>
    public ConfigurationService(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the API base URL from JavaScript or configuration
    /// </summary>
    public async Task<string> GetApiBaseUrlAsync()
    {
        try
        {
            // Try to get the API base URL from JavaScript
            var jsApiBaseUrl = await _jsRuntime.InvokeAsync<string>("configHelper.getApiBaseUrl");
            
            if (!string.IsNullOrEmpty(jsApiBaseUrl))
            {
                Console.WriteLine($"Using API base URL from JavaScript: {jsApiBaseUrl}");
                return jsApiBaseUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting API base URL from JavaScript: {ex.Message}");
        }

        // Fall back to configuration
        var configApiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:8080";
        Console.WriteLine($"Using API base URL from configuration: {configApiBaseUrl}");
        return configApiBaseUrl;
    }
}
