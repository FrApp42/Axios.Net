using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;

namespace AxiosNet;

public class AxiosClient
{
    private readonly HttpClient _http;
    private readonly bool _customHandler;
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    /// <summary>Defaults applied to every request of this client.</summary>
    public AxiosRequestConfig Defaults { get; }

    public AxiosInterceptors Interceptors { get; } = new();

    public JsonSerializerOptions JsonOptions { get; set; } = new(JsonSerializerDefaults.Web);

    /// <param name="defaults">Defaults merged into every request.</param>
    /// <param name="handler">
    /// Custom message handler (the equivalent of an axios adapter). When set, the handler-level options
    /// <see cref="AxiosRequestConfig.MaxRedirects"/>, <see cref="AxiosRequestConfig.Decompress"/> and
    /// <see cref="AxiosRequestConfig.Proxy"/> are the handler's responsibility and ignored here.
    /// </param>
    public AxiosClient(AxiosRequestConfig? defaults = null, HttpMessageHandler? handler = null)
    {
        Defaults = defaults ?? new();

        _customHandler = handler is not null;
        _http = handler is null ? CreateClient(null, null, true) : new HttpClient(handler);
        _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan; // timeouts are per request
    }

    #region Request aliases

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

    /// <summary>POST with <c>Content-Type: multipart/form-data</c>.</summary>
    public Task<AxiosResponse<T>> PostForm<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Post, url, data, AsForm(config));

    public Task<AxiosResponse<T>> PutForm<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Put, url, data, AsForm(config));

    public Task<AxiosResponse<T>> PatchForm<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Send<T>(HttpMethod.Patch, url, data, AsForm(config));

    /// <summary>Returns the final URL (base URL, params and serializer applied) without sending anything.</summary>
    public string GetUri(AxiosRequestConfig? config = null) =>
        AxiosSerializer.BuildUrl(Defaults.Merge(config));

    #endregion

    /// <summary>Sends a request fully described by <paramref name="config"/>.</summary>
    public async Task<AxiosResponse<T>> Request<T>(AxiosRequestConfig config)
    {
        AxiosRequestConfig merged = Defaults.Merge(config);
        AxiosRequestConfig current = merged;
        Exception? error = null;

        // Request interceptors run last registered first.
        foreach (var handler in Interceptors.Request.Snapshot().Reverse())
        {
            if (handler.Options?.RunWhen is { } runWhen && !runWhen(merged))
            {
                continue;
            }

            try
            {
                if (error is null)
                {
                    if (handler.Fulfilled is not null) current = await handler.Fulfilled(current);
                }
                else if (handler.Rejected is not null)
                {
                    current = await handler.Rejected(error);
                    error = null;
                }
            }
            catch (Exception ex) when (handler.Options?.Synchronous == true && error is null)
            {
                if (handler.Rejected is null)
                {
                    error = ex;
                }
                else
                {
                    try
                    {
                        // Handled: the returned config is ignored and dispatch uses the last valid one.
                        await handler.Rejected(ex);
                    }
                    catch (Exception rethrown)
                    {
                        error = rethrown;
                    }
                }

                break;
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        AxiosResponse? response = null;

        if (error is null)
        {
            try
            {
                response = await Execute<T>(current);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        // Response interceptors run first registered first.
        foreach (var handler in Interceptors.Response.Snapshot())
        {
            try
            {
                if (error is null)
                {
                    if (handler.Fulfilled is not null) response = await handler.Fulfilled(response!);
                }
                else if (handler.Rejected is not null)
                {
                    response = await handler.Rejected(error);
                    error = null;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();

        return response as AxiosResponse<T> ?? Coerce<T>(response!);
    }

    /// <summary>
    /// Types a response that is not an <see cref="AxiosResponse{T}"/>, typically an error response
    /// recovered by a rejection interceptor, whose body is still raw text.
    /// </summary>
    private AxiosResponse<T> Coerce<T>(AxiosResponse response)
    {
        T? data = default;

        if (response.Data is T typed)
        {
            data = typed;
        }
        else if (response.Data is string text)
        {
            try
            {
                data = JsonSerializer.Deserialize<T>(text, JsonOptions);
            }
            catch (JsonException)
            {
                // The body does not fit T: keep the default, the raw text stays on the untyped response.
            }
        }

        return new AxiosResponse<T>
        {
            Status = response.Status,
            StatusText = response.StatusText,
            Headers = response.Headers,
            Config = response.Config,
            Request = response.Request,
            Duration = response.Duration,
            Data = data
        };
    }

    private Task<AxiosResponse<T>> Send<T>(HttpMethod method, string url, object? data, AxiosRequestConfig? config)
    {
        AxiosRequestConfig request = new AxiosRequestConfig().Merge(config);
        request.Url = url;
        request.Method = method;

        if (data is not null)
        {
            request.Data = data;
        }

        return Request<T>(request);
    }

    private static AxiosRequestConfig AsForm(AxiosRequestConfig? config)
    {
        AxiosRequestConfig form = new AxiosRequestConfig().Merge(config);
        form.Headers["Content-Type"] = "multipart/form-data";

        return form;
    }

    private async Task<AxiosResponse<T>> Execute<T>(AxiosRequestConfig config)
    {
        Stopwatch clock = Stopwatch.StartNew();
        HttpMethod method = config.Method ?? HttpMethod.Get;
        AxiosHeaders headers = EffectiveHeaders(config, method);

        string url = AxiosSerializer.BuildUrl(config);

        HttpContent? content = AxiosSerializer.BuildContent(config, headers, JsonOptions);

        if (
            content is not null && config.MaxBodyLength is >= 0 and long maxBody &&
            content.Headers.ContentLength > maxBody
        )
        {
            throw new AxiosException(
                $"Request body larger than maxBodyLength limit ({maxBody})", AxiosErrorCodes.BadRequest, config);
        }

        if (
            content is not null && config.OnUploadProgress is { } onUpload
        )
        {
            content = new ProgressContent(content, onUpload);
        }

        using HttpRequestMessage message = new(method, url) { Content = content };

        if (config.Auth is var (user, password))
        {
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));

            headers.Set("Authorization", $"Basic {token}");
        }

        foreach (var (name, value) in headers)
        {
            if (!message.Headers.TryAddWithoutValidation(name, value))
            {
                content?.Headers.TryAddWithoutValidation(name, value);
            }
        }

        bool stream = (config.ResponseType ?? InferType(typeof(T))) == AxiosResponseType.Stream;

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(config.CancellationToken);

        if (config.Timeout is { } timeout && timeout > TimeSpan.Zero)
        {
            cts.CancelAfter(timeout);
        }

        HttpResponseMessage http;

        byte[] body = [];

        try
        {
            http = await GetHttpClient(config).SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!stream || !IsValid(config, (int)http.StatusCode))
            {
                body = await ReadBody(http.Content, config, cts.Token);
            }
        }
        catch (OperationCanceledException ex) when (config.CancellationToken.IsCancellationRequested)
        {
            throw new CanceledError(config, request: message, inner: ex);
        }
        catch (OperationCanceledException ex)
        {
            bool clarify = config.Transitional?.ClarifyTimeoutError == true;

            throw new AxiosException(
                $"timeout of {config.Timeout?.TotalMilliseconds}ms exceeded",
                clarify ? AxiosErrorCodes.TimedOut : AxiosErrorCodes.Timeout,
                config, message, inner: ex
            );
        }
        catch (HttpRequestException ex)
        {
            throw new AxiosException(ex.Message, AxiosErrorCodes.Network, config, message, inner: ex);
        }

        int status = (int)http.StatusCode;

        AxiosHeaders responseHeaders = new();

        foreach (var header in http.Headers)
        {
            responseHeaders.Set(header.Key.ToLowerInvariant(), string.Join(", ", header.Value));
        }

        foreach (var header in http.Content.Headers)
        {
            responseHeaders.Set(header.Key.ToLowerInvariant(), string.Join(", ", header.Value));
        }

        if (!IsValid(config, status))
        {
            using (http)
            {
                AxiosResponse failed = new()
                {
                    Status = status,
                    StatusText = http.ReasonPhrase,
                    Headers = responseHeaders,
                    Config = config,
                    Request = message,
                    Duration = clock.Elapsed,
                    Data = ApplyTransforms(
                        body.Length == 0
                            ? null
                            : DecodeText(body, http.Content, config),
                        responseHeaders,
                        status,
                        config
                    )
                };

                throw new AxiosException(
                    $"Request failed with status code {status}",
                    status is >= 400 and < 500 ? AxiosErrorCodes.BadRequest : AxiosErrorCodes.BadResponse,
                    config, message, failed
                );
            }
        }

        if (stream)
        {
            // The caller owns the stream, which keeps the connection (and so the response) alive.
            return new AxiosResponse<T>
            {
                Status = status,
                StatusText = http.ReasonPhrase,
                Headers = responseHeaders,
                Config = config,
                Request = message,
                Duration = clock.Elapsed,
                Data = (T)(object)await http.Content.ReadAsStreamAsync(cts.Token)
            };
        }

        using (http)
        {
            return new AxiosResponse<T>
            {
                Status = status,
                StatusText = http.ReasonPhrase,
                Headers = responseHeaders,
                Config = config,
                Request = message,
                Duration = clock.Elapsed,
                Data = ConvertData<T>(body, http.Content, responseHeaders, status, config)
            };
        }
    }

    #region Helpers

    private static bool IsValid(AxiosRequestConfig config, int status) =>
        (config.ValidateStatus ?? (s => s is >= 200 and < 300))(status);

    private static AxiosHeaders EffectiveHeaders(AxiosRequestConfig config, HttpMethod method)
    {
        AxiosHeaders headers = new() { { "Accept", "application/json, text/plain, */*" } };

        if (config.MethodHeaders.TryGetValue("common", out var common))
        {
            headers.Set(common);
        }

        if (config.MethodHeaders.TryGetValue(method.Method.ToLowerInvariant(), out var specific))
        {
            headers.Set(specific);
        }

        return headers.Set(config.Headers);
    }

    private static AxiosResponseType InferType(Type type) =>
        type == typeof(byte[]) ? AxiosResponseType.Bytes
        : type == typeof(string) ? AxiosResponseType.Text
        : typeof(Stream).IsAssignableFrom(type) ? AxiosResponseType.Stream
        : AxiosResponseType.Json;

    private static async Task<byte[]> ReadBody(HttpContent content, AxiosRequestConfig config, CancellationToken token)
    {
        long max = config.MaxContentLength ?? -1;
        long? total = content.Headers.ContentLength;

        if (max >= 0 && total > max)
        {
            throw new AxiosException(
                $"maxContentLength size of {max} exceeded",
                AxiosErrorCodes.BadResponse,
                config
            );
        }

        ProgressTracker? tracker = config.OnDownloadProgress is { } onDownload
            ? new(onDownload, false, total)
            : null;

        await using Stream source = await content.ReadAsStreamAsync(token);

        using MemoryStream buffer = new();

        byte[] chunk = new byte[81920];

        int read;

        while ((read = await source.ReadAsync(chunk, token)) > 0)
        {
            buffer.Write(chunk, 0, read);

            if (max >= 0 && buffer.Length > max)
            {
                throw new AxiosException(
                    $"maxContentLength size of {max} exceeded",
                    AxiosErrorCodes.BadResponse,
                    config
                );
            }

            tracker?.Add(read);
        }

        tracker?.Finish();

        return buffer.ToArray();
    }

    private static string DecodeText(byte[] body, HttpContent content, AxiosRequestConfig config)
    {
        Encoding encoding = config.ResponseEncoding ??
                            TryGetEncoding(content.Headers.ContentType?.CharSet) ?? Encoding.UTF8;

        return encoding.GetString(body);
    }

    private static Encoding? TryGetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            return null;
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static object? ApplyTransforms(object? data, AxiosHeaders headers, int status, AxiosRequestConfig config)
    {
        if (config.TransformResponse is { } transforms)
        {
            foreach (var transform in transforms)
            {
                data = transform(data, headers, status);
            }
        }

        return data;
    }

    private T? ConvertData<T>(
        byte[] body,
        HttpContent content,
        AxiosHeaders headers,
        int status,
        AxiosRequestConfig config
    )
    {
        AxiosResponseType type = config.ResponseType ?? InferType(typeof(T));

        bool explicitJson = config.ResponseType == AxiosResponseType.Json;

        AxiosTransitional transitional = config.Transitional ?? new();

        object? data = type == AxiosResponseType.Bytes
            ? body
            : body.Length == 0
                ? null
                : DecodeText(body, content, config);

        data = ApplyTransforms(data, headers, status, config);

        switch (data)
        {
            case null:
                return default;
            case T typed:
                return typed;
            case byte[] bytes when typeof(T) == typeof(string):
                return (T)(object)Encoding.UTF8.GetString(bytes);
            case string text when typeof(T) == typeof(object) && !transitional.ForcedJsonParsing:
                return (T)(object)text;
            case string text:
                try
                {
                    return JsonSerializer.Deserialize<T>(text, JsonOptions);
                }
                catch (JsonException ex)
                {
                    if (explicitJson && transitional.SilentJsonParsing)
                    {
                        return default;
                    }

                    if (typeof(T) == typeof(object))
                    {
                        return (T)(object)text;
                    }

                    throw new AxiosException(
                        $"Response body is not valid JSON for {typeof(T).Name}: {ex.Message}",
                        AxiosErrorCodes.BadResponse,
                        config,
                        inner: ex
                    );
                }
            default:
                // A transform produced another object: map it to T through JSON.
                return JsonSerializer.Deserialize<T>(
                    JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions),
                    JsonOptions
                );
        }
    }

    private HttpClient GetHttpClient(AxiosRequestConfig config)
    {
        bool decompress = config.Decompress ?? true;

        if (_customHandler || (config.MaxRedirects is null && config.Proxy is null && decompress))
        {
            return _http;
        }

        AxiosProxy? proxy = config.Proxy;

        string proxyKey = proxy is null ? string.Empty
            : ReferenceEquals(proxy, AxiosProxy.Disabled) ? "disabled"
            : $"{proxy.Protocol}://{proxy.Host}:{proxy.Port}/{proxy.Auth?.Username}";

        return _clients.GetOrAdd(
            $"{config.MaxRedirects}|{proxyKey}|{decompress}",
            _ => CreateClient(config.MaxRedirects, proxy, decompress)
        );
    }

    private static HttpClient CreateClient(int? maxRedirects, AxiosProxy? proxy, bool decompress)
    {
        SocketsHttpHandler handler = new()
        {
            AutomaticDecompression = decompress ? DecompressionMethods.All : DecompressionMethods.None
        };

        if (maxRedirects is { } redirects)
        {
            if (redirects <= 0)
            {
                handler.AllowAutoRedirect = false;
            }
            else
            {
                handler.MaxAutomaticRedirections = redirects;
            }
        }

        if (proxy is not null)
        {
            if (ReferenceEquals(proxy, AxiosProxy.Disabled))
            {
                handler.UseProxy = false;
            }
            else
            {
                handler.UseProxy = true;
                handler.Proxy = proxy.ToWebProxy();
            }
        }

        return new HttpClient(handler) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    }

    #endregion
}
