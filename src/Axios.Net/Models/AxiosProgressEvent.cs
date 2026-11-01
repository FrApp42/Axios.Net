namespace AxiosNet.Models;

/// <summary>Progress of a request body upload or of a response download.</summary>
public sealed class AxiosProgressEvent
{
    /// <summary>Bytes transferred so far.</summary>
    public long Loaded { get; init; }

    /// <summary>Total number of bytes, when known.</summary>
    public long? Total { get; init; }

    /// <summary>Fraction between 0 and 1, when the total is known.</summary>
    public double? Progress { get; init; }

    /// <summary>Bytes transferred since the previous event.</summary>
    public long Bytes { get; init; }

    /// <summary>Bytes per second since the previous event.</summary>
    public double? Rate { get; init; }

    /// <summary>Estimated time remaining, when the rate and the total are known.</summary>
    public TimeSpan? Estimated { get; init; }

    /// <summary>True for an upload event.</summary>
    public bool Upload { get; init; }

    /// <summary>True for a download event.</summary>
    public bool Download => !Upload;
}
