using AxiosNet.Client;
using AxiosNet.Config;
using Microsoft.Extensions.DependencyInjection;

namespace AxiosNet.Extensions.DependencyInjection;

/// <summary>Dependency injection extensions for <see cref="AxiosClient"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AxiosClient"/> as a singleton. Its requests go through the <c>Axios.Net</c> named
    /// <see cref="IHttpClientFactory"/> client, so handlers can be chained with
    /// <c>AddHttpClient("Axios.Net").AddHttpMessageHandler&lt;T&gt;()</c>.
    /// </summary>
    /// <remarks>
    /// The handler comes from the factory, so <see cref="AxiosRequestConfig.MaxRedirects"/>,
    /// <see cref="AxiosRequestConfig.Decompress"/> and <see cref="AxiosRequestConfig.Proxy"/> are no longer read
    /// from the request config: configure them on the primary handler. The singleton keeps the handler it was
    /// created with, so it is not recycled and DNS changes are not picked up.
    /// </remarks>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="configure">Sets the defaults merged into every request of the client.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAxios(this IServiceCollection services, Action<AxiosRequestConfig>? configure = null)
    {
        services.AddHttpClient("Axios.Net");
        services.AddSingleton(sp =>
        {
            AxiosRequestConfig axiosRequestConfig = new();
            HttpMessageHandler handler = sp.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("Axios.Net");

            configure?.Invoke(axiosRequestConfig);
            
            return new AxiosClient(axiosRequestConfig, handler);
        });

        return services;
    }
}
