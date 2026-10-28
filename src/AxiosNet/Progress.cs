using System.Net;
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
            Estimated = total.HasValue && rate > 0 ? TimeSpan.FromSeconds(Math.Max(total.Value - _loaded, 0) / rate) : null,
            Upload = upload
        });

        _reportedLoaded = _loaded;
        _reportedAt = now;
    }
}

/// <summary>Wraps request content to report upload progress while it is written to the wire.</summary>
internal sealed class ProgressContent : HttpContent
{
    private readonly HttpContent _inner;
    private readonly Action<AxiosProgressEvent> _callback;
    private readonly long? _length;

    public ProgressContent(HttpContent inner, Action<AxiosProgressEvent> callback)
    {
        _inner = inner;
        _callback = callback;
        _length = inner.Headers.ContentLength;

        foreach (var header in inner.Headers)
            Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        ProgressTracker tracker = new(_callback, true, _length);
        await _inner.CopyToAsync(new CountingStream(stream, tracker.Add), context, cancellationToken);
        tracker.Finish();
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _length ?? 0;
        return _length.HasValue;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class CountingStream(Stream inner, Action<long> onWrite) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() => inner.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        inner.Write(buffer, offset, count);
        onWrite(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await inner.WriteAsync(buffer, cancellationToken);
        onWrite(buffer.Length);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
}
