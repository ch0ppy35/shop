using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace App.Tests.TestHelpers;

/// <summary>
/// A custom mock implementation of ISnackbar for testing
/// </summary>
public class MockSnackbar : ISnackbar
{
    /// <summary>
    /// Tracks the number of times Add was called
    /// </summary>
    public int AddCallCount { get; private set; }

    /// <summary>
    /// The last message that was added
    /// </summary>
    public string? LastMessage { get; private set; }

    /// <summary>
    /// The last severity that was used
    /// </summary>
    public Severity LastSeverity { get; private set; }

    /// <summary>
    /// Gets the configuration
    /// </summary>
    public SnackbarConfiguration Configuration { get; } = new SnackbarConfiguration();

    /// <summary>
    /// Gets the shown snackbars
    /// </summary>
    public IEnumerable<Snackbar> ShownSnackbars => new List<Snackbar>();

    /// <summary>
    /// Event that is fired when snackbars are updated
    /// </summary>
    public event Action? OnSnackbarsUpdated { add { } remove { } }

    /// <summary>
    /// Adds a snackbar message
    /// </summary>
    public Snackbar Add(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? options = null, string? key = null)
    {
        AddCallCount++;
        LastMessage = message;
        LastSeverity = severity;

        // Create a mock Snackbar - we don't need the actual instance for testing
        return null!;
    }

    /// <summary>
    /// Adds a snackbar message with markup
    /// </summary>
    public Snackbar Add(MarkupString message, Severity severity = Severity.Normal, Action<SnackbarOptions>? options = null, string? key = null)
    {
        AddCallCount++;
        LastMessage = message.Value;
        LastSeverity = severity;

        // Create a mock Snackbar - we don't need the actual instance for testing
        return null!;
    }

    /// <summary>
    /// Adds a snackbar message with a render fragment
    /// </summary>
    public Snackbar Add(RenderFragment message, Severity severity = Severity.Normal, Action<SnackbarOptions>? options = null, string? key = null)
    {
        AddCallCount++;
        LastSeverity = severity;

        // Create a mock Snackbar - we don't need the actual instance for testing
        return null!;
    }

    /// <summary>
    /// Adds a snackbar message with a component
    /// </summary>
    public Snackbar Add<T>(Dictionary<string, object>? parameters = null, Severity severity = Severity.Normal, Action<SnackbarOptions>? options = null, string? key = null) where T : IComponent
    {
        AddCallCount++;
        LastSeverity = severity;

        // Create a mock Snackbar - we don't need the actual instance for testing
        return null!;
    }

    /// <summary>
    /// Clears all snackbars
    /// </summary>
    public void Clear()
    {
        // Not implemented for tests
    }

    /// <summary>
    /// Removes a snackbar
    /// </summary>
    public void Remove(Snackbar snackbar)
    {
        // Not implemented for tests
    }

    /// <summary>
    /// Removes a snackbar by key
    /// </summary>
    public void RemoveByKey(string key)
    {
        // Not implemented for tests
    }

    /// <summary>
    /// Disposes the snackbar
    /// </summary>
    public void Dispose()
    {
        // Not implemented for tests
    }
}
