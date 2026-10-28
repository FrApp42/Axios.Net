using System.Collections;

namespace AxiosNet;

/// <summary>
/// Thrown when a request fails: rejected status, timeout, cancellation, network error or bad configuration.
/// </summary>
public class AxiosException : Exception
{
    private const string Redacted = "[REDACTED ****]";

    /// <summary>One of <see cref="AxiosErrorCodes"/>.</summary>
    public string Code { get; }

    public AxiosRequestConfig Config { get; }

    /// <summary>The request that was sent, when it got that far.</summary>
    public HttpRequestMessage? Request { get; }

    /// <summary>
    /// Set when the server answered; <see cref="AxiosResponse.Data"/> holds the (transformed) body text.
    /// </summary>
    public AxiosResponse? Response { get; }

    public int? Status => Response?.Status;

    public bool IsAxiosError => true;

    public AxiosException(
        string message,
        string code,
        AxiosRequestConfig config,
        HttpRequestMessage? request = null,
        AxiosResponse? response = null,
        Exception? inner = null
    ) : base(message, inner)
    {
        Code = code;
        Config = config;
        Request = request;
        Response = response;
    }

    /// <summary>
    /// Serializable view of the error. Config keys listed in <see cref="AxiosRequestConfig.Redact"/>
    /// are masked (case-insensitive, recursive).
    /// </summary>
    public IDictionary<string, object?> ToJSON() => new Dictionary<string, object?>
    {
        ["message"] = Message,
        ["name"] = GetType().Name,
        ["code"] = Code,
        ["status"] = Status,
        ["config"] = ConfigToJson(),
        ["stack"] = StackTrace
    };

    private Dictionary<string, object?> ConfigToJson()
    {
        HashSet<string> redact = new(Config.Redact ?? [], StringComparer.OrdinalIgnoreCase);

        Dictionary<string, object?> config = new()
        {
            ["url"] = Config.Url,
            ["method"] = Config.Method?.Method,
            ["baseURL"] = Config.BaseUrl,
            ["headers"] = Config.Headers.ToJson(),
            ["params"] = Config.Params,
            ["data"] = Config.Data,
            ["timeout"] = Config.Timeout?.TotalMilliseconds
        };

        return (Dictionary<string, object?>)Mask(config, redact)!;
    }

    private static object? Mask(object? value, HashSet<string> redact)
    {
        switch (value)
        {
            case null or string:
                return value;
            case IDictionary dictionary:
                Dictionary<string, object?> copy = [];

                foreach (DictionaryEntry entry in dictionary)
                {
                    string key = entry.Key?.ToString() ?? string.Empty;

                    copy[key] = redact.Contains(key) ? Redacted : Mask(entry.Value, redact);
                }

                return copy;
            case IEnumerable list when value is not byte[]:
                return list.Cast<object?>().Select(item => Mask(item, redact)).ToList();
            default:
                return value;
        }
    }
}
