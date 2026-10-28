namespace AxiosNet;

public class AxiosResponse
{
    public int Status { get; init; }

    public string? StatusText { get; init; }

    /// <summary>Response and content headers.</summary>
    public AxiosHeaders Headers { get; init; } = new();

    /// <summary>The config the request was sent with.</summary>
    public AxiosRequestConfig Config { get; init; } = new();

    /// <summary>The request that was sent.</summary>
    public HttpRequestMessage? Request { get; init; }

    /// <summary>Approximate time taken until the response was fully received.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Untyped body: the converted value, or the (transformed) body text on error responses.</summary>
    public object? Data { get; init; }
}

public class AxiosResponse<T> : AxiosResponse
{
    /// <summary>Body converted to <typeparamref name="T"/> (default when empty).</summary>
    public new T? Data
    {
        get => base.Data is T value ? value : default;
        init => base.Data = value;
    }
}
