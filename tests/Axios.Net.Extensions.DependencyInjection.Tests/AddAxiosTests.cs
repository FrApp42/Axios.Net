using System.Net;
using AxiosNet.Client;
using Microsoft.Extensions.DependencyInjection;

namespace AxiosNet.Extensions.DependencyInjection.Tests;

[TestClass]
public class AddAxiosTests
{
    private sealed class CountingHandler : DelegatingHandler
    {
        public int Calls { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"ok\"", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    [TestMethod]
    public void AddAxios_RegistersResolvableClient()
    {
        ServiceCollection services = new();
        services.AddAxios();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetRequiredService<AxiosClient>());
    }

    [TestMethod]
    public void AddAxios_AppliesConfiguredDefaults()
    {
        ServiceCollection services = new();
        services.AddAxios(c => c.BaseUrl = "https://api.test/v1/");

        using ServiceProvider provider = services.BuildServiceProvider();

        AxiosClient client = provider.GetRequiredService<AxiosClient>();

        Assert.AreEqual("https://api.test/v1/", client.Defaults.BaseUrl);
    }

    [TestMethod]
    public void AddAxios_ClientIsSingleton()
    {
        ServiceCollection services = new();
        services.AddAxios();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.AreSame(provider.GetRequiredService<AxiosClient>(), provider.GetRequiredService<AxiosClient>());
    }

    [TestMethod]
    public async Task AddAxios_UsesHandlersFromHttpClientFactory()
    {
        CountingHandler handler = new();
        ServiceCollection services = new();

        services.AddAxios(c => c.BaseUrl = "http://test");
        services.AddHttpClient("Axios.Net")
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();

        AxiosClient client = provider.GetRequiredService<AxiosClient>();

        await client.Get<string>("/x");

        Assert.AreEqual(1, handler.Calls);
        Assert.AreEqual("http://test/x", handler.LastUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task AddAxios_RunsDelegatingHandlersAddedToTheNamedClient()
    {
        CountingHandler counting = new();
        ServiceCollection services = new();

        services.AddAxios(c => c.BaseUrl = "http://test");
        services.AddHttpClient("Axios.Net")
            .AddHttpMessageHandler(() => counting)
            .ConfigurePrimaryHttpMessageHandler(() => new CountingHandler());

        using ServiceProvider provider = services.BuildServiceProvider();

        await provider.GetRequiredService<AxiosClient>().Get<string>("/x");

        Assert.AreEqual(1, counting.Calls);
    }
}
