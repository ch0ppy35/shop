namespace Frontend.Services;

/// <summary>
/// A single API round trip as seen from the browser
/// </summary>
public sealed record RequestTiming(DateTimeOffset At, string Method, string Path, int Status, double ElapsedMs);

/// <summary>
/// Keeps the most recent API round trips so the UI can show how fast the backend answers
/// </summary>
public class RequestTimeline
{
    private const int Capacity = 25;
    private readonly LinkedList<RequestTiming> _entries = new();

    /// <summary>
    /// Raised after each recorded request
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the recorded requests, newest first
    /// </summary>
    public IEnumerable<RequestTiming> Entries => _entries;

    /// <summary>
    /// Gets the most recent request, if any
    /// </summary>
    public RequestTiming? Latest => _entries.First?.Value;

    /// <summary>
    /// Records a completed request
    /// </summary>
    public void Record(RequestTiming timing)
    {
        _entries.AddFirst(timing);
        while (_entries.Count > Capacity)
        {
            _entries.RemoveLast();
        }

        Changed?.Invoke();
    }
}
