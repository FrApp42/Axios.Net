using System.Reflection;
using System.Text.Json;

namespace AxiosNet;

/// <summary>
/// Default shared client, mirroring the axios top-level API:
/// <c>await Axios.Get&lt;User&gt;("https://api.test/users/1")</c>.
/// </summary>
public static class Axios
{
    private static readonly AxiosClient Instance = new();

    /// <summary>Library version.</summary>
    public static string Version { get; } =
        typeof(Axios).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.0";

    public static AxiosRequestConfig Defaults => Instance.Defaults;

    public static AxiosInterceptors Interceptors => Instance.Interceptors;

    /// <summary>Creates an isolated client with its own defaults and interceptors.</summary>
    public static AxiosClient Create(AxiosRequestConfig? config = null, HttpMessageHandler? handler = null) =>
        new(config, handler);

    public static Task<AxiosResponse<T>> Request<T>(AxiosRequestConfig config) =>
        Instance.Request<T>(config);

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

    public static Task<AxiosResponse<T>> PostForm<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Instance.PostForm<T>(url, data, config);

    public static Task<AxiosResponse<T>> PutForm<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Instance.PutForm<T>(url, data, config);

    public static Task<AxiosResponse<T>> PatchForm<T>(string url, object? data = null, AxiosRequestConfig? config = null) =>
        Instance.PatchForm<T>(url, data, config);

    /// <summary>Returns the final URL for <paramref name="config"/> without sending a request.</summary>
    public static string GetUri(AxiosRequestConfig? config = null) => Instance.GetUri(config);

    /// <summary>Waits for every request, like <c>axios.all</c>.</summary>
    public static Task<T[]> All<T>(params IEnumerable<Task<T>> requests) => Task.WhenAll(requests);

    /// <summary>True when <paramref name="error"/> comes from axios (any <see cref="AxiosException"/>).</summary>
    public static bool IsAxiosError(object? error) => error is AxiosException;

    /// <summary>True when the request was canceled through its cancellation token.</summary>
    public static bool IsCancel(object? error) => error is CanceledError;

    /// <summary>Merges two configs; <paramref name="overrides"/> wins, headers and params are merged key by key.</summary>
    public static AxiosRequestConfig MergeConfig(AxiosRequestConfig config, AxiosRequestConfig? overrides) =>
        config.Merge(overrides);

    /// <summary>Serializes an object into multipart form data, like <c>axios.toFormData</c>.</summary>
    public static MultipartFormDataContent ToFormData(object data, AxiosFormSerializer? options = null) =>
        AxiosSerializer.BuildMultipart(data, new() { FormSerializer = options }, new(JsonSerializerDefaults.Web));
}
