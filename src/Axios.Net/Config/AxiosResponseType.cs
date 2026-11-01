namespace AxiosNet.Config;

/// <summary>How the response body is read.</summary>
public enum AxiosResponseType
{
    /// <summary>Body is deserialized from JSON into <c>T</c>.</summary>
    Json,
    /// <summary>Body is returned as a <c>string</c>.</summary>
    Text,

    /// <summary>Body is returned as <c>byte[]</c>.</summary>
    Bytes,

    /// <summary>Body is returned as an unread <see cref="Stream"/>; the caller disposes it.</summary>
    Stream
}
