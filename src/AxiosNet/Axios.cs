namespace AxiosNet;

/// <summary>
/// Default shared client, mirroring the axios top-level API:
/// <c>await Axios.Get&lt;User&gt;("https://api.test/users/1")</c>.
/// </summary>
public static class Axios
{
    private static readonly AxiosClient Instance = new();

    public static AxiosRequestConfig Defaults => Instance.Defaults;

    public static AxiosInterceptors Interceptors => Instance.Interceptors;

    /// <summary>Creates an isolated client with its own defaults and interceptors.</summary>
    public static AxiosClient Create(AxiosRequestConfig? config = null, HttpMessageHandler? handler = null) =>
        new(config, handler);

    public static Task<AxiosResponse<T>> Get<T>(string url, AxiosRequestConfig? config = null) =>
        Instance.Get<T>(url, config);

    public static Task<AxiosResponse<T>> Delete<T>(string url, AxiosRequestConfig? config = null) =>
        Instance.Delete<T>(url, config);

    public static Task<AxiosResponse<T>> Head<T>(string url, AxiosRequestConfig? config = null) =>
        Instance.Head<T>(url, config);

    public static Task<AxiosResponse<T>> Options<T>(string url, AxiosRequestConfig? config = null) =>
        Instance.Options<T>(url, config);

    public static Task<AxiosResponse<T>> Post<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Instance.Post<T>(url, data, config);

    public static Task<AxiosResponse<T>> Put<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Instance.Put<T>(url, data, config);

    public static Task<AxiosResponse<T>> Patch<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Instance.Patch<T>(url, data, config);

    public static Task<AxiosResponse<T>> Request<T>(AxiosRequestConfig config) =>
        Instance.Request<T>(config);
}
