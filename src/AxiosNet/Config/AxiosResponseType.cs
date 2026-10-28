namespace AxiosNet;

public enum AxiosResponseType
{
    /// <summary>Body is deserialized from JSON into <c>T</c>.</summary>
    Json,
    Text,

    /// <summary>Body is returned as <c>byte[]</c>.</summary>
    Bytes,

    /// <summary>Body is returned as an unread <see cref="Stream"/>; the caller disposes it.</summary>
    Stream
}
