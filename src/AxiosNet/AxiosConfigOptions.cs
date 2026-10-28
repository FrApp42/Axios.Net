using System.Net;

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

/// <summary>Controls how <see cref="AxiosRequestConfig.Params"/> are turned into a query string.</summary>
public class AxiosParamsSerializer
{
    /// <summary>
    /// Encodes each key and value. Defaults to an encodeURIComponent-like encoder (space as +, keeps []:$,).
    /// </summary>
    public Func<string, string>? Encode { get; set; }

    /// <summary>Serializes the whole params object; takes precedence over every other option.</summary>
    public Func<IReadOnlyDictionary<string, object?>, string>? Serialize { get; set; }

    /// <summary>Array format: null = <c>a=1&amp;a=2</c>, false = <c>a[]=1</c>, true = <c>a[0]=1</c>.</summary>
    public bool? Indexes { get; set; } = false;

    /// <summary>Maximum nesting depth; exceeded depth throws ERR_FORM_DATA_DEPTH_EXCEEDED.</summary>
    public int MaxDepth { get; set; } = 100;
}

/// <summary>Controls multipart and urlencoded serialization of object bodies.</summary>
public class AxiosFormSerializer
{
    /// <summary>Use dot notation (<c>a.b</c>) instead of brackets (<c>a[b]</c>).</summary>
    public bool Dots { get; set; }

    /// <summary>Keep special key endings such as <c>{}</c> (values of such keys are sent as JSON).</summary>
    public bool MetaTokens { get; set; } = true;

    /// <summary>Array format, same meaning as <see cref="AxiosParamsSerializer.Indexes"/>.</summary>
    public bool? Indexes { get; set; } = false;

    public int MaxDepth { get; set; } = 100;
}

public class AxiosTransitional
{
    /// <summary
    /// >With an explicit <see cref="AxiosResponseType.Json"/>, a JSON parse failure yields default instead of throwing.
    /// </summary>
    public bool SilentJsonParsing { get; set; } = true;

    /// <summary>When false, <c>T = object</c> returns the raw text instead of parsing it as JSON.</summary>
    public bool ForcedJsonParsing { get; set; } = true;

    /// <summary>Timeouts fail with ETIMEDOUT instead of ECONNABORTED.</summary>
    public bool ClarifyTimeoutError { get; set; }
}

public class AxiosProxy
{
    /// <summary>Pass as <see cref="AxiosRequestConfig.Proxy"/> to ignore system and environment proxies.</summary>
    public static readonly AxiosProxy Disabled = new();

    public string Protocol { get; set; } = "http";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public (string Username, string Password)? Auth { get; set; }

    internal WebProxy ToWebProxy() => new($"{Protocol}://{Host}:{Port}")
    {
        Credentials = Auth is var (user, password) ? new NetworkCredential(user, password) : null
    };
}

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

/// <summary>A file part for multipart bodies.</summary>
public sealed class AxiosFile
{
    public object Content { get; }

    public string FileName { get; }

    public string? ContentType { get; }

    public AxiosFile(byte[] content, string fileName, string? contentType = null)
        => (Content, FileName, ContentType) = (content, fileName, contentType);

    public AxiosFile(Stream content, string fileName, string? contentType = null)
        => (Content, FileName, ContentType) = (content, fileName, contentType);
}
