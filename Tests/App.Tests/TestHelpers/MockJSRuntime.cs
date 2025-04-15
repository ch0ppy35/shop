using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace App.Tests.TestHelpers;

/// <summary>
/// A custom mock implementation of IJSRuntime for testing
/// </summary>
public class MockJSRuntime : IJSRuntime
{
    /// <summary>
    /// Invokes a JavaScript function and returns the result
    /// </summary>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        // Return default value for any type
        return new ValueTask<TValue>(default(TValue)!);
    }

    /// <summary>
    /// Invokes a JavaScript function and returns the result with cancellation token
    /// </summary>
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        // Return default value for any type
        return new ValueTask<TValue>(default(TValue)!);
    }
}
