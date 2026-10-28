namespace AxiosNet.Models;

public sealed class AxiosProgressEvent
{
    public long Loaded { get; init; }

    public long? Total { get; init; }

    /// <summary>Fraction between 0 and 1, when the total is known.</summary>
    public double? Progress { get; init; }

    /// <summary>Bytes transferred since the previous event.</summary>
    public long Bytes { get; init; }

    /// <summary>Bytes per second since the previous event.</summary>
    public double? Rate { get; init; }

    public TimeSpan? Estimated { get; init; }

    public bool Upload { get; init; }

    public bool Download => !Upload;
}
