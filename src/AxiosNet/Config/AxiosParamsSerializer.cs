namespace AxiosNet.Config;

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
