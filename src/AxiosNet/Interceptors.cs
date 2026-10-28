namespace AxiosNet;

public sealed class InterceptorManager<T>
{
    private readonly List<(int Id, Func<T, Task<T>> Handler)> _handlers = [];
    private int _nextId;

    /// <summary>Registers an interceptor and returns its id for <see cref="Eject"/>.</summary>
    public int Use(Func<T, Task<T>> handler)
    {
        int id = _nextId++;
        lock (_handlers) _handlers.Add((id, handler));
        return id;
    }

    public int Use(Func<T, T> handler) => Use(value => Task.FromResult(handler(value)));

    public void Eject(int id)
    {
        lock (_handlers) _handlers.RemoveAll(h => h.Id == id);
    }

    internal async Task<T> RunAsync(T value)
    {
        (int Id, Func<T, Task<T>> Handler)[] snapshot;
        lock (_handlers) snapshot = [.. _handlers];

        foreach (var (_, handler) in snapshot)
            value = await handler(value);

        return value;
    }
}

public sealed class AxiosInterceptors
{
    /// <summary>Runs before sending, in registration order. Return the (modified) config.</summary>
    public InterceptorManager<AxiosRequestConfig> Request { get; } = new();

    /// <summary>Runs on every resolved response (before typed deserialization).</summary>
    public InterceptorManager<AxiosResponse> Response { get; } = new();

    /// <summary>Runs on every <see cref="AxiosException"/> before it is thrown. Return the exception to throw.</summary>
    public InterceptorManager<AxiosException> Error { get; } = new();
}
