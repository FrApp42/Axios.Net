namespace AxiosNet;

public class AxiosResponse
{
    public int Status { get; init; }

    public string? StatusText { get; init; }

    /// <summary>Response and content headers (case-insensitive).</summary>
    public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The config the request was sent with.</summary>
    public AxiosRequestConfig Config { get; init; } = new();

    /// <summary>Untyped body: the deserialized value, or the raw text on error responses.</summary>
    public object? Data { get; init; }
}

public class AxiosResponse<T> : AxiosResponse
{
    /// <summary>Body deserialized as <typeparamref name="T"/> (default when empty).</summary>
    public new T? Data
    {
        get => (T?)base.Data;
        init => base.Data = value;
    }
}
