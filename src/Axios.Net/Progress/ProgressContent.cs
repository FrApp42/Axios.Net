using System.Net;
using AxiosNet.Models;

namespace AxiosNet.Progress;

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

        foreach (KeyValuePair<string, IEnumerable<string>> header in inner.Headers)
        {
            Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken
    )
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
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
