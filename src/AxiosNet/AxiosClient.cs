using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AxiosNet;

public class AxiosClient
{
    private readonly HttpClient _http;

    /// <summary>Defaults applied to every request of this client.</summary>
    public AxiosRequestConfig Defaults { get; }

    public AxiosInterceptors Interceptors { get; } = new();

    public JsonSerializerOptions JsonOptions { get; set; } = new(JsonSerializerDefaults.Web);

    public AxiosClient(AxiosRequestConfig? defaults = null, HttpMessageHandler? handler = null)
    {
        Defaults = defaults ?? new();
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan; // timeouts are per request
    }

    public Task<AxiosResponse<T>> Get<T>(string url, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Get, url, null, config);

    public Task<AxiosResponse<T>> Delete<T>(string url, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Delete, url, null, config);

    public Task<AxiosResponse<T>> Head<T>(string url, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Head, url, null, config);

    public Task<AxiosResponse<T>> Options<T>(string url, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Options, url, null, config);

    public Task<AxiosResponse<T>> Post<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Post, url, data, config);

    public Task<AxiosResponse<T>> Put<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Put, url, data, config);

    public Task<AxiosResponse<T>> Patch<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Patch, url, data, config);

    /// <summary>Sends a request fully described by <paramref name="config"/> (Url and Method required).</summary>
    public async Task<AxiosResponse<T>> Request<T>(AxiosRequestConfig config)
    {
        AxiosRequestConfig merged = Defaults.Merge(config);
        merged = await Interceptors.Request.RunAsync(merged);

        try
        {
            return await Execute<T>(merged);
        }
        catch (AxiosException ex)
        {
            throw await Interceptors.Error.RunAsync(ex);
        }
    }

    private Task<AxiosResponse<T>> Send<T>(HttpMethod method, string url, object? data, AxiosRequestConfig? config)
    {
        AxiosRequestConfig request = Defaults.Merge(config);
        request.Url = url;
        request.Method = method;
        if (data is not null) request.Data = data;

        // Defaults are merged again inside Request, which is harmless (idempotent).
        return Request<T>(request);
    }

    private async Task<AxiosResponse<T>> Execute<T>(AxiosRequestConfig config)
    {
        using HttpRequestMessage message = new(config.Method ?? HttpMethod.Get, BuildUrl(config));
        HttpContent? content = BuildContent(config.Data);
        message.Content = content;

        if (config.Auth is var (user, password))
        {
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        foreach (var (key, value) in config.Headers)
        {
            if (message.Headers.TryAddWithoutValidation(key, value)) continue;
            if (content is null) continue;

            content.Headers.Remove(key);
            content.Headers.TryAddWithoutValidation(key, value);
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(config.CancellationToken);
        if (config.Timeout is { } limit) timeout.CancelAfter(limit);

        HttpResponseMessage http;
        try
        {
            http = await _http.SendAsync(message, HttpCompletionOption.ResponseContentRead, timeout.Token);
        }
        catch (OperationCanceledException ex) when (config.CancellationToken.IsCancellationRequested)
        {
            throw new AxiosException("Request canceled", "ERR_CANCELED", config, inner: ex);
        }
        catch (OperationCanceledException ex)
        {
            throw new AxiosException($"timeout of {config.Timeout?.TotalMilliseconds}ms exceeded", "ECONNABORTED", config, inner: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new AxiosException(ex.Message, "ERR_NETWORK", config, inner: ex);
        }

        using (http)
        {
            byte[] body = await http.Content.ReadAsByteArrayAsync(config.CancellationToken);
            int status = (int)http.StatusCode;
            bool valid = (config.ValidateStatus ?? (s => s is >= 200 and < 300))(status);

            Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
            foreach (var h in http.Headers) headers[h.Key] = string.Join(", ", h.Value);
            foreach (var h in http.Content.Headers) headers[h.Key] = string.Join(", ", h.Value);

            if (!valid)
            {
                AxiosResponse failed = new()
                {
                    Status = status,
                    StatusText = http.ReasonPhrase,
                    Headers = headers,
                    Config = config,
                    Data = Encoding.UTF8.GetString(body)
                };

                throw new AxiosException($"Request failed with status code {status}", "ERR_BAD_RESPONSE", config, failed);
            }

            AxiosResponse<T> response = new()
            {
                Status = status,
                StatusText = http.ReasonPhrase,
                Headers = headers,
                Config = config,
                Data = Deserialize<T>(body, http.Content.Headers.ContentType?.MediaType, config)
            };

            await Interceptors.Response.RunAsync(response);

            return response;
        }
    }

    private T? Deserialize<T>(byte[] body, string? mediaType, AxiosRequestConfig config)
    {
        if (typeof(T) == typeof(byte[])) return (T)(object)body;
        if (body.Length == 0) return default;

        if (typeof(T) == typeof(string)) return (T)(object)Encoding.UTF8.GetString(body);

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new AxiosException(
                $"Response body is not valid {typeof(T).Name} JSON ({mediaType ?? "unknown content type"}): {ex.Message}",
                "ERR_BAD_RESPONSE", config, inner: ex);
        }
    }

    private HttpContent? BuildContent(object? data) => data switch
    {
        null => null,
        HttpContent content => content,
        string text => new StringContent(text, Encoding.UTF8, "text/plain"),
        byte[] bytes => new ByteArrayContent(bytes) { Headers = { ContentType = new("application/octet-stream") } },
        _ => new StringContent(JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8, "application/json")
    };

    private static string BuildUrl(AxiosRequestConfig config)
    {
        string url = config.Url ?? string.Empty;

        if (!string.IsNullOrEmpty(config.BaseUrl) && !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            url = $"{config.BaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";

        List<string> pairs = [];
        foreach (var (key, value) in config.Params)
        {
            if (value is null) continue;

            IEnumerable<object?> values = value is System.Collections.IEnumerable list and not string
                ? list.Cast<object?>()
                : [value];

            foreach (object? item in values.Where(v => v is not null))
                pairs.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(Format(item!))}");
        }

        if (pairs.Count == 0) return url;

        return url + (url.Contains('?') ? "&" : "?") + string.Join("&", pairs);
    }

    private static string Format(object value) => value switch
    {
        bool b => b ? "true" : "false",
        DateTime d => d.ToString("o"),
        DateTimeOffset d => d.ToString("o"),
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
