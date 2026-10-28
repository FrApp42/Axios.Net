using System.Diagnostics;

namespace AxiosNet;

/// <summary>Turns raw byte counts into throttled <see cref="AxiosProgressEvent"/> callbacks.</summary>
internal sealed class ProgressTracker(Action<AxiosProgressEvent> callback, bool upload, long? total)
{
    private const int ThrottleMs = 100;

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private long _loaded;
    private long _reportedLoaded;
    private long _reportedAt;

    public void Add(long bytes)
    {
        _loaded += bytes;

        bool done = total.HasValue && _loaded >= total.Value;

        if (done || _clock.ElapsedMilliseconds - _reportedAt >= ThrottleMs)
            Report();
    }

    /// <summary>Flushes a pending event, e.g. when the total is unknown.</summary>
    public void Finish()
    {
        if (_loaded != _reportedLoaded) Report();
    }

    private void Report()
    {
        long now = _clock.ElapsedMilliseconds;
        long bytes = _loaded - _reportedLoaded;
        double seconds = Math.Max(now - _reportedAt, 1) / 1000d;
        double rate = bytes / seconds;

        callback(new AxiosProgressEvent
        {
            Loaded = _loaded,
            Total = total,
            Progress = total is > 0 ? Math.Min((double)_loaded / total.Value, 1) : null,
            Bytes = bytes,
            Rate = rate,
            Estimated = total.HasValue && rate > 0
                ? TimeSpan.FromSeconds(Math.Max(total.Value - _loaded, 0) / rate)
                : null,
            Upload = upload
        });

        _reportedLoaded = _loaded;
        _reportedAt = now;
    }
}
