using AxiosNet.Client;
using AxiosNet.Config;
using Microsoft.Extensions.DependencyInjection;

namespace AxiosNet.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
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
