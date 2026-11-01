using AxiosNet.Config;

namespace AxiosNet.Models;

/// <summary>The response to a request, with an untyped body.</summary>
public class AxiosResponse
{
    /// <summary>HTTP status code.</summary>
    public int Status { get; init; }

    /// <summary>Reason phrase sent by the server.</summary>
    public string? StatusText { get; init; }

    /// <summary>Response and content headers.</summary>
    public AxiosHeaders Headers { get; init; } = new();

    /// <summary>The config the request was sent with.</summary>
    public AxiosRequestConfig Config { get; init; } = new();

    /// <summary>The request that was sent.</summary>
    public HttpRequestMessage? Request { get; init; }

    /// <summary>Approximate time taken until the response was fully received.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Untyped body: the converted value, or the (transformed) body text on error responses.</summary>
    public object? Data { get; init; }
}
