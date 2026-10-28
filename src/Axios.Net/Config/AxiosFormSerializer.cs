namespace AxiosNet.Config;

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
