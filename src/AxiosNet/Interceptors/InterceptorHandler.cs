namespace AxiosNet;

internal sealed record InterceptorHandler<T>(
    int Id,
    Func<T, Task<T>>? Fulfilled,
    Func<Exception, Task<T>>? Rejected,
    InterceptorOptions? Options
);
