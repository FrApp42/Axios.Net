namespace AxiosNet;

/// <summary>
/// Configuration of a request. Used both as client defaults and per request;
/// per-request values override the defaults, headers are merged.
/// </summary>
public class AxiosRequestConfig
{
    /// <summary>Server URL, absolute or relative to <see cref="BaseUrl"/>.</summary>
    public string? Url { get; set; }

    public HttpMethod? Method { get; set; }

    /// <summary>Prepended to <see cref="Url"/> unless it is absolute.</summary>
    public string? BaseUrl { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Query string parameters. Null values are skipped, collections repeat the key.</summary>
    public Dictionary<string, object?> Params { get; set; } = [];

    /// <summary>
    /// Request body. A string is sent as text, a byte[] as binary, an <see cref="HttpContent"/>
    /// (e.g. <see cref="MultipartFormDataContent"/>) as is, anything else as JSON.
    /// </summary>
    public object? Data { get; set; }

    public TimeSpan? Timeout { get; set; }

    /// <summary>Decides whether a status code resolves (true) or throws (false). Defaults to 2xx.</summary>
    public Func<int, bool>? ValidateStatus { get; set; }

    /// <summary>Sets a Basic Authorization header.</summary>
    public (string Username, string Password)? Auth { get; set; }

    public CancellationToken CancellationToken { get; set; }

    internal AxiosRequestConfig Merge(AxiosRequestConfig? other)
    {
        AxiosRequestConfig merged = new()
        {
            Url = other?.Url ?? Url,
            Method = other?.Method ?? Method,
            BaseUrl = other?.BaseUrl ?? BaseUrl,
            Data = other?.Data ?? Data,
            Timeout = other?.Timeout ?? Timeout,
            ValidateStatus = other?.ValidateStatus ?? ValidateStatus,
            Auth = other?.Auth ?? Auth,
            CancellationToken = other?.CancellationToken ?? default
        };

        foreach (var header in Headers) merged.Headers[header.Key] = header.Value;
        foreach (var param in Params) merged.Params[param.Key] = param.Value;

        if (other != null)
        {
            foreach (var header in other.Headers) merged.Headers[header.Key] = header.Value;
            foreach (var param in other.Params) merged.Params[param.Key] = param.Value;
        }

        return merged;
    }
}
