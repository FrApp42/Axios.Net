global using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Text;

namespace AxiosNet.Tests;

[TestClass]
public class AxiosTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            await Task.Yield();
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private record User(int Id, string Name);

    [TestMethod]
    public async Task Get_DeserializesJson_AndBuildsUrl()
    {
        FakeHandler handler = new(_ => Json("""{"id":1,"name":"Ada"}"""));
        AxiosClient api = Axios.Create(new() { BaseUrl = "https://api.test/v1/" }, handler);

        AxiosResponse<User> res = await api.Get<User>("/users", new()
        {
            Params = { ["page"] = 2, ["tags"] = new[] { "a", "b c" }, ["skip"] = null, ["ok"] = true }
        });

        Assert.AreEqual(200, res.Status);
        Assert.AreEqual("Ada", res.Data!.Name);
        Assert.AreEqual("https://api.test/v1/users?page=2&tags=a&tags=b%20c&ok=true", handler.Last!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task Post_SendsJson_AndMergesHeaders()
    {
        FakeHandler handler = new(_ => Json("""{"id":2,"name":"Bob"}"""));
        AxiosClient api = Axios.Create(new() { Headers = { ["X-Default"] = "d" } }, handler);

        await api.Post<User>("https://api.test/users", new { Name = "Bob" }, new() { Headers = { ["X-Req"] = "r" } });

        Assert.AreEqual("""{"name":"Bob"}""", handler.LastBody);
        Assert.AreEqual("application/json", handler.Last!.Content!.Headers.ContentType!.MediaType);
        Assert.AreEqual("d", handler.Last.Headers.GetValues("X-Default").Single());
        Assert.AreEqual("r", handler.Last.Headers.GetValues("X-Req").Single());
    }

    [TestMethod]
    public async Task BadStatus_ThrowsAxiosException_WithResponse()
    {
        AxiosClient api = Axios.Create(handler: new FakeHandler(_ => Json("""{"error":"nope"}""", HttpStatusCode.NotFound)));

        AxiosException ex = await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Get<User>("https://api.test/x"));

        Assert.AreEqual(404, ex.Status);
        Assert.AreEqual("ERR_BAD_RESPONSE", ex.Code);
        Assert.AreEqual("""{"error":"nope"}""", ex.Response!.Data);
    }

    [TestMethod]
    public async Task ValidateStatus_AllowsCustomStatus()
    {
        AxiosClient api = Axios.Create(handler: new FakeHandler(_ => new(HttpStatusCode.NotFound)));

        AxiosResponse<string> res = await api.Get<string>("https://api.test/x", new() { ValidateStatus = s => s < 500 });

        Assert.AreEqual(404, res.Status);
        Assert.IsNull(res.Data);
    }

    [TestMethod]
    public async Task Interceptors_RunAndCanBeEjected()
    {
        FakeHandler handler = new(_ => Json("{}"));
        AxiosClient api = Axios.Create(handler: handler);
        int id = api.Interceptors.Request.Use(c => { c.Headers["Authorization"] = "Bearer t"; return c; });

        await api.Get<string>("https://api.test/x");
        Assert.AreEqual("Bearer t", handler.Last!.Headers.Authorization!.ToString());

        api.Interceptors.Request.Eject(id);
        await api.Get<string>("https://api.test/x");
        Assert.IsNull(handler.Last!.Headers.Authorization);
    }

    [TestMethod]
    public async Task Timeout_ThrowsEconnaborted()
    {
        AxiosClient api = Axios.Create(handler: new DelayHandler());

        AxiosException ex = await Assert.ThrowsExactlyAsync<AxiosException>(
            () => api.Get<string>("https://api.test/x", new() { Timeout = TimeSpan.FromMilliseconds(50) }));

        Assert.AreEqual("ECONNABORTED", ex.Code);
    }

    private sealed class DelayHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new(HttpStatusCode.OK);
        }
    }
}
