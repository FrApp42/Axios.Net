namespace AxiosNet.Interceptors;

public sealed class InterceptorManager<T>
{
    private readonly List<InterceptorHandler<T>> _handlers = [];
    private int _nextId;

    /// <summary>
    /// Registers an interceptor and returns its id for <see cref="Eject"/>. <paramref name="onRejected"/>
    /// receives the current error and returns a value to recover, or throws to keep the error flowing.
    /// </summary>
    public int Use(
        Func<T, Task<T>>? onFulfilled,
        Func<Exception, Task<T>>? onRejected = null,
        InterceptorOptions? options = null
    )
    {
        lock (_handlers)
        {
            int id = _nextId++;

            _handlers.Add(new(id, onFulfilled, onRejected, options));

            return id;
        }
    }

    /// <summary>
    /// Synchronous variant of <see cref="Use(Func{T, Task{T}}?, Func{Exception, Task{T}}?, InterceptorOptions?)"/>.
    /// </summary>
    public int Use(Func<T, T> onFulfilled, Func<Exception, T>? onRejected = null, InterceptorOptions? options = null)
    {
        return Use(
            value => Task.FromResult(onFulfilled(value)),
            onRejected is null ? null : error => Task.FromResult(onRejected(error)),
            options
        );
    }

    /// <summary>Removes one interceptor. Unknown or already ejected ids are ignored.</summary>
    public void Eject(int id)
    {
        lock (_handlers) _handlers.RemoveAll(h => h.Id == id);
    }

    /// <summary>Removes every interceptor of this chain.</summary>
    public void Clear()
    {
        lock (_handlers) _handlers.Clear();
    }

    internal InterceptorHandler<T>[] Snapshot()
    {
        lock (_handlers) return [.. _handlers];
    }
}
