using AxiosNet.Config;

namespace AxiosNet.Interceptors;

public sealed class InterceptorOptions
{
    /// <summary>
    /// Run without yielding to the scheduler. If such a request interceptor throws, its rejection handler
    /// is called and the remaining request interceptors are skipped; when the handler returns normally the
    /// request is still dispatched with the last valid config.
    /// </summary>
    public bool Synchronous { get; set; }

    /// <summary>Request interceptors only: the interceptor is skipped when this returns false.</summary>
    public Func<AxiosRequestConfig, bool>? RunWhen { get; set; }
}
