using System.Collections;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AxiosNet;

/// <summary>Query string, urlencoded and multipart serialization shared by the client and the public helpers.</summary>
internal static partial class AxiosSerializer
{
    [GeneratedRegex(@"^([a-z][a-z\d+\-.]*:)?//", RegexOptions.IgnoreCase)]
    private static partial Regex AbsoluteUrl();

    /// <summary>encodeURIComponent, then restores the characters axios leaves readable.</summary>
    public static string DefaultEncode(string value) => Uri.EscapeDataString(value)
        .Replace("%3A", ":", StringComparison.OrdinalIgnoreCase)
        .Replace("%24", "$")
        .Replace("%2C", ",", StringComparison.OrdinalIgnoreCase)
        .Replace("%20", "+")
        .Replace("%5B", "[", StringComparison.OrdinalIgnoreCase)
        .Replace("%5D", "]", StringComparison.OrdinalIgnoreCase);

    public static string BuildUrl(AxiosRequestConfig config)
    {
        string url = config.Url ?? string.Empty;

        bool absolute = AbsoluteUrl().IsMatch(url);

        if (
            url.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new AxiosException($"Invalid URL: {url}", AxiosErrorCodes.InvalidUrl, config);
        }

        if (!string.IsNullOrEmpty(config.BaseUrl) && (!absolute || config.AllowAbsoluteUrls == false))
        {
            url = url.Length == 0
                ? config.BaseUrl
                : $"{config.BaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
        }

        int hash = url.IndexOf('#');

        if (hash >= 0)
        {
            url = url[..hash];
        }

        string query = BuildQuery(config);

        if (query.Length > 0)
        {
            url += (url.Contains('?') ? "&" : "?") + query;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
        {
            throw new AxiosException($"Invalid URL: {url}", AxiosErrorCodes.InvalidUrl, config);
        }

        return url;
    }

    private static string BuildQuery(AxiosRequestConfig config)
    {
        if (config.Params.Count == 0)
        {
            return string.Empty;
        }

        AxiosParamsSerializer? options = config.ParamsSerializer;

        if (options?.Serialize is { } custom)
        {
            return custom(config.Params);
        }

        Func<string, string> encode = options?.Encode ?? DefaultEncode;

        List<KeyValuePair<string, object>> pairs = Flatten(
            config.Params,
            false,
            options is null
                ? false
                : options.Indexes, true,
            options?.MaxDepth ?? 100,
            config, new(JsonSerializerDefaults.Web)
        );

        return string.Join(
            "&", pairs.Select(p => $"{encode(p.Key)}={encode(Stringify(p.Value))}")
        );
    }

    public static string BuildUrlEncoded(object data, AxiosRequestConfig config, JsonSerializerOptions json)
    {
        AxiosFormSerializer form = config.FormSerializer ?? new();

        List<KeyValuePair<string, object>> pairs = Flatten(
            Root(data, json),
            form.Dots,
            form.Indexes,
            form.MetaTokens,
            form.MaxDepth,
            config,
            json
        );

        return string.Join(
            "&", pairs.Select(p => $"{DefaultEncode(p.Key)}={DefaultEncode(Stringify(p.Value))}")
        );
    }

    public static MultipartFormDataContent BuildMultipart(
        object data,
        AxiosRequestConfig config,
        JsonSerializerOptions json
    )
    {
        AxiosFormSerializer form = config.FormSerializer ?? new();
        MultipartFormDataContent multipart = new();

        foreach (
            (string key, object value) in Flatten(
                Root(data, json),
                form.Dots,
                form.Indexes,
                form.MetaTokens,
                form.MaxDepth,
                config,
                json
            )
        )
        {
            switch (value)
            {
                case AxiosFile { Content: byte[] bytes } file:
                    multipart.Add(
                        WithType(new ByteArrayContent(bytes), file.ContentType), key, file.FileName
                    );
                    break;
                case AxiosFile { Content: Stream stream } file:
                    multipart.Add(
                        WithType(new StreamContent(stream), file.ContentType), key, file.FileName
                    );
                    break;
                case byte[] bytes:
                    multipart.Add(new ByteArrayContent(bytes), key, "blob");
                    break;
                case Stream stream:
                    multipart.Add(new StreamContent(stream), key, "blob");
                    break;
                case HttpContent content:
                    multipart.Add(content, key);
                    break;
                default:
                    multipart.Add(new StringContent(Stringify(value)) { Headers = { ContentType = null } }, key);
                    break;
            }
        }

        return multipart;
    }

    /// <summary>
    /// Builds the request body and consumes the Content-Type header: it is applied to the content
    /// (multipart keeps its own boundary), so the caller must not send it a second time.
    /// </summary>
    public static HttpContent? BuildContent(AxiosRequestConfig config, AxiosHeaders headers, JsonSerializerOptions json)
    {
        object? data = config.Data;

        if (config.TransformRequest is { } transforms)
        {
            foreach (Func<object?, AxiosHeaders, object?> transform in transforms)
            {
                data = transform(data, headers);
            }
        }

        string? contentType = headers.Get("Content-Type");

        headers.Delete("Content-Type");

        HttpContent? content;

        switch (data)
        {
            case null:
                return null;
            case HttpContent http:
                content = http;
                break;
            case byte[] bytes:
                content = new ByteArrayContent(bytes);
                contentType ??= "application/octet-stream";
                break;
            case Stream stream:
                content = new StreamContent(stream);
                contentType ??= "application/octet-stream";
                break;
            case string text:
                content = new StringContent(text, Encoding.UTF8);
                contentType ??= "application/x-www-form-urlencoded";
                break;
            default:
                if (contentType?.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return BuildMultipart(data, config, json);
                }

                if (
                    contentType?.Contains(
                        "application/x-www-form-urlencoded",
                        StringComparison.OrdinalIgnoreCase
                    ) == true
                )
                {
                    content = new StringContent(BuildUrlEncoded(data, config, json), Encoding.UTF8);
                }
                else
                {
                    content = new StringContent(JsonSerializer.Serialize(data, json), Encoding.UTF8);

                    contentType ??= "application/json";
                }

                break;
        }

        if (contentType is not null && MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? parsed))
        {
            content.Headers.ContentType = parsed;
        }

        return content;
    }

    private static HttpContent WithType(HttpContent content, string? contentType)
    {
        if (contentType is not null && MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? parsed))
        {
            content.Headers.ContentType = parsed;
        }

        return content;
    }

    private static IEnumerable<KeyValuePair<string, object?>> Root(object data, JsonSerializerOptions json)
    {
        if (data is IDictionary dictionary)
        {
            List<KeyValuePair<string, object?>> entries = [];

            foreach (DictionaryEntry entry in dictionary)
            {
                entries.Add(new(entry.Key.ToString() ?? string.Empty, entry.Value));
            }

            return entries;
        }

        JsonElement element = JsonSerializer.SerializeToElement(data, json);

        return element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().Select(p => new KeyValuePair<string, object?>(p.Name, p.Value))
            : [];
    }

    /// <summary>
    /// Flattens nested values into key/leaf pairs: <c>a[b]</c> or <c>a.b</c>, arrays per <paramref name="indexes"/>
    /// (null: repeated key, false: <c>a[]</c>, true: <c>a[0]</c>). Null values are skipped.
    /// </summary>
    public static List<KeyValuePair<string, object>> Flatten(
        IEnumerable<KeyValuePair<string, object?>> root,
        bool dots,
        bool? indexes,
        bool metaTokens,
        int maxDepth,
        AxiosRequestConfig config,
        JsonSerializerOptions json
    )
    {
        List<KeyValuePair<string, object>> result = [];

        foreach ((string key, object? value) in root)
        {
            Walk(key, value, 1);
        }

        return result;

        void Walk(string key, object? value, int depth)
        {
            if (value is null)
            {
                return;
            }

            if (depth > maxDepth)
            {
                throw new AxiosException(
                    $"Object is too deeply nested ({maxDepth} levels max)",
                    AxiosErrorCodes.FormDataDepthExceeded,
                    config
                );
            }

            if (key.EndsWith("{}", StringComparison.Ordinal))
            {
                result.Add(
                    new(metaTokens ? key : key[..^2], JsonSerializer.Serialize(value, json))
                );

                return;
            }

            switch (value)
            {
                case string or bool or char or Guid or DateTime or DateTimeOffset or Enum or byte[] or Stream
                    or HttpContent or AxiosFile:
                case IFormattable:
                    result.Add(new(key, value));
                    break;
                case JsonElement element:
                    WalkElement(key, element, depth);
                    break;
                case IDictionary dictionary:
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        Walk(Join(key, entry.Key.ToString() ?? string.Empty), entry.Value, depth + 1);
                    }

                    break;
                case IEnumerable list:
                    WalkList(key, list.Cast<object?>(), depth);
                    break;
                default:
                    WalkElement(key, JsonSerializer.SerializeToElement(value, json), depth);
                    break;
            }
        }

        void WalkElement(string key, JsonElement element, int depth)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    if (depth + 1 > maxDepth)
                    {
                        throw new AxiosException(
                            $"Object is too deeply nested ({maxDepth} levels max)",
                            AxiosErrorCodes.FormDataDepthExceeded,
                            config
                        );
                    }

                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        WalkElement(Join(key, property.Name), property.Value, depth + 1);
                    }

                    break;
                case JsonValueKind.Array:
                    WalkList(key, element.EnumerateArray().Cast<object?>(), depth);
                    break;
                case JsonValueKind.String:
                    result.Add(new(key, element.GetString()!));
                    break;
                case JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False:
                    result.Add(new(key, element.GetRawText()));
                    break;
            }
        }

        void WalkList(string key, IEnumerable<object?> items, int depth)
        {
            int index = 0;

            foreach (object? item in items)
            {
                string itemKey = indexes switch
                {
                    null => key,
                    false => key + "[]",
                    true => $"{key}[{index}]"
                };

                Walk(itemKey, item, depth + 1);

                index++;
            }
        }

        string Join(string key, string name) => dots ? $"{key}.{name}" : $"{key}[{name}]";
    }

    private static string Stringify(object value) => value switch
    {
        bool b => b ? "true" : "false",
        DateTime d => d.ToString("o", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("o", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
