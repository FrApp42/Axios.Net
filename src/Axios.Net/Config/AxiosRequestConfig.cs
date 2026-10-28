using AxiosNet.Errors;
using AxiosNet.Models;

namespace AxiosNet.Config;

/// <summary>
/// Configuration of a request. Used both as client defaults and per request.
/// Merge order: library defaults, then client defaults, then the request config.
/// </summary>
public class AxiosRequestConfig
{
    /// <summary>Server URL, absolute or relative to <see cref="BaseUrl"/>.</summary>
    public string? Url { get; set; }

    /// <summary>HTTP method, GET when unset.</summary>
    public HttpMethod? Method { get; set; }

    /// <summary>Prepended to <see cref="Url"/> unless it is absolute.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>When false, absolute URLs are still prefixed by <see cref="BaseUrl"/>. Defaults to true.</summary>
    public bool? AllowAbsoluteUrls { get; set; }

    /// <summary>Headers sent with this request (override <see cref="MethodHeaders"/>).</summary>
    public AxiosHeaders Headers { get; set; } = new();

    /// <summary>
    /// Default headers per lowercase method name, plus "common" applied to every request
    /// (axios' <c>defaults.headers.common</c> / <c>defaults.headers.post</c>).
    /// </summary>
    public Dictionary<string, AxiosHeaders> MethodHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Shortcut for <c>MethodHeaders["common"]</c>.</summary>
    public AxiosHeaders CommonHeaders =>
        MethodHeaders.TryGetValue("common", out AxiosHeaders? h) ? h : MethodHeaders["common"] = new();

    /// <summary>
    /// Query string parameters. Null values are skipped; arrays and nested objects follow
    /// <see cref="ParamsSerializer"/>.
    /// </summary>
    public Dictionary<string, object?> Params { get; set; } = [];

    public AxiosParamsSerializer? ParamsSerializer { get; set; }

    /// <summary>
    /// Request body, never inherited from defaults. A string is sent as is, a byte[] or Stream as binary,
    /// an <see cref="HttpContent"/> as is, anything else as JSON, or urlencoded / multipart when
    /// the Content-Type header asks for it.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>Runs on <see cref="Data"/> before it is turned into content; may edit the headers.</summary>
    public List<Func<object?, AxiosHeaders, object?>>? TransformRequest { get; set; }

    /// <summary>
    /// Runs on the raw body (string, or byte[] for <see cref="AxiosResponseType.Bytes"/>)
    /// before it is converted to <c>T</c>. Arguments: data, response headers, status.
    /// </summary>
    public List<Func<object?, AxiosHeaders, int, object?>>? TransformResponse { get; set; }

    /// <summary>Aborts the request when exceeded. Unset or zero means no timeout.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Sets a Basic Authorization header, overwriting any custom one.</summary>
    public (string Username, string Password)? Auth { get; set; }

    /// <summary>Inferred from <c>T</c> when unset (string, byte[], Stream, otherwise JSON).</summary>
    public AxiosResponseType? ResponseType { get; set; }

    /// <summary>Encoding used to decode the body. Defaults to the response charset, then UTF-8.</summary>
    public System.Text.Encoding? ResponseEncoding { get; set; }

    /// <summary>Decides whether a status code resolves (true) or throws (false). Defaults to 2xx.</summary>
    public Func<int, bool>? ValidateStatus { get; set; }

    /// <summary>Maximum response size in bytes, -1 for unlimited. Exceeding it throws ERR_BAD_RESPONSE.</summary>
    public long? MaxContentLength { get; set; }

    /// <summary>Maximum request body size in bytes, -1 for unlimited, when the length is known.</summary>
    public long? MaxBodyLength { get; set; }

    /// <summary>Maximum redirects followed; 0 disables redirects. Needs the built-in handler.</summary>
    public int? MaxRedirects { get; set; }

    /// <summary>
    /// Automatically decompress gzip, deflate and brotli responses. Defaults to true. Needs the built-in handler.
    /// </summary>
    public bool? Decompress { get; set; }

    /// <summary>
    /// Proxy settings; <see cref="AxiosProxy.Disabled"/> ignores system proxies. Needs the built-in handler.
    /// </summary>
    public AxiosProxy? Proxy { get; set; }

    public Action<AxiosProgressEvent>? OnUploadProgress { get; set; }

    public Action<AxiosProgressEvent>? OnDownloadProgress { get; set; }

    /// <summary>Config keys masked (case-insensitive, recursive) by <see cref="AxiosException.ToJSON"/>.</summary>
    public List<string>? Redact { get; set; }

    public AxiosTransitional? Transitional { get; set; }

    public AxiosFormSerializer? FormSerializer { get; set; }

    /// <summary>Cancels the request (axios' <c>signal</c>).</summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Returns a new config where <paramref name="other"/> wins over this one.
    /// Headers and params are merged key by key; Url, Method and Data only come from <paramref name="other"/>.
    /// </summary>
    internal AxiosRequestConfig Merge(AxiosRequestConfig? other)
    {
        AxiosRequestConfig merged = new()
        {
            Url = other?.Url,
            Method = other?.Method,
            Data = other?.Data,
            BaseUrl = other?.BaseUrl ?? BaseUrl,
            AllowAbsoluteUrls = other?.AllowAbsoluteUrls ?? AllowAbsoluteUrls,
            ParamsSerializer = other?.ParamsSerializer ?? ParamsSerializer,
            TransformRequest = other?.TransformRequest ?? TransformRequest,
            TransformResponse = other?.TransformResponse ?? TransformResponse,
            Timeout = other?.Timeout ?? Timeout,
            Auth = other?.Auth ?? Auth,
            ResponseType = other?.ResponseType ?? ResponseType,
            ResponseEncoding = other?.ResponseEncoding ?? ResponseEncoding,
            ValidateStatus = other?.ValidateStatus ?? ValidateStatus,
            MaxContentLength = other?.MaxContentLength ?? MaxContentLength,
            MaxBodyLength = other?.MaxBodyLength ?? MaxBodyLength,
            MaxRedirects = other?.MaxRedirects ?? MaxRedirects,
            Decompress = other?.Decompress ?? Decompress,
            Proxy = other?.Proxy ?? Proxy,
            OnUploadProgress = other?.OnUploadProgress ?? OnUploadProgress,
            OnDownloadProgress = other?.OnDownloadProgress ?? OnDownloadProgress,
            Redact = other?.Redact ?? Redact,
            Transitional = other?.Transitional ?? Transitional,
            FormSerializer = other?.FormSerializer ?? FormSerializer,
            CancellationToken = other is not null && other.CancellationToken.CanBeCanceled
                ? other.CancellationToken
                : CancellationToken,
            Headers = Headers.Concat(other?.Headers ?? new())
        };

        foreach (KeyValuePair<string, object?> param in Params)
        {
            merged.Params[param.Key] = param.Value;
        }
        
        foreach (KeyValuePair<string, object?> param in other?.Params ?? [])
        {
            merged.Params[param.Key] = param.Value;
        }

        foreach ((string method, AxiosHeaders headers) in MethodHeaders)
        {
            merged.MethodHeaders[method] = headers.Clone();
        }

        foreach ((string method, AxiosHeaders headers) in other?.MethodHeaders ?? [])
        {
            merged.MethodHeaders[method] = merged.MethodHeaders.TryGetValue(method, out AxiosHeaders? existing)
                ? existing.Concat(headers)
                : headers.Clone();
        }

        return merged;
    }
}
