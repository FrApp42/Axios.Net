namespace AxiosNet;

public sealed class AxiosInterceptors
{
    /// <summary>Runs before sending, last registered first (LIFO). Return the (modified) config.</summary>
    public InterceptorManager<AxiosRequestConfig> Request { get; } = new();

    /// <summary>Runs after the response (or error) arrives, first registered first (FIFO).</summary>
    public InterceptorManager<AxiosResponse> Response { get; } = new();
}
