using System.Net;
using System.Text;

namespace AxiosNet.Tests;

[TestClass]
public class FeatureTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }

    private static HttpResponseMessage Ok(string body = "{}", string type = "application/json") =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, type) };

    private static (AxiosClient Client, FakeHandler Handler) Make(Func<HttpRequestMessage, HttpResponseMessage>? respond = null, AxiosRequestConfig? defaults = null)
    {
        FakeHandler handler = new(respond ?? (_ => Ok()));
        return (Axios.Create(defaults, handler), handler);
    }

    [TestMethod]
    public void Headers_AreCaseInsensitive()
    {
        AxiosHeaders headers = new() { { "Content-Type", "a" } };

        Assert.AreEqual("a", headers["content-type"]);
        Assert.IsTrue(headers.Has("CONTENT-TYPE"));
        headers["content-type"] = null;
        Assert.AreEqual(0, headers.Count);
    }

    [TestMethod]
    public async Task MethodHeaders_ApplyByMethod_AndRequestHeadersWin()
    {
        var (api, handler) = Make();
        api.Defaults.CommonHeaders["X-Common"] = "c";
        api.Defaults.MethodHeaders["post"] = new() { { "X-Post", "p" }, { "X-Common", "post-wins" } };

        await api.Post<string>("https://api.test/x", new { A = 1 }, new() { Headers = { ["X-Post"] = "request-wins" } });

        Assert.AreEqual("post-wins", handler.Last!.Headers.GetValues("X-Common").Single());
        Assert.AreEqual("request-wins", handler.Last.Headers.GetValues("X-Post").Single());
    }

    [TestMethod]
    public async Task Data_IsNotInheritedFromDefaults()
    {
        var (api, handler) = Make(defaults: new() { Data = "from-defaults" });

        await api.Post<string>("https://api.test/x");

        Assert.IsNull(handler.Last!.Content);
    }

    [TestMethod]
    public async Task Params_FollowIndexesOption_AndNestedObjects()
    {
        var (api, handler) = Make();
        Dictionary<string, object?> query = new() { ["ids"] = new[] { 1, 2 }, ["filter"] = new Dictionary<string, object?> { ["name"] = "a b" } };

        await api.Get<string>("https://api.test/x?q=1", new() { Params = query, ParamsSerializer = new() { Indexes = true } });
        Assert.AreEqual("https://api.test/x?q=1&ids[0]=1&ids[1]=2&filter[name]=a+b", handler.Last!.RequestUri!.AbsoluteUri);

        await api.Get<string>("https://api.test/x", new() { Params = query, ParamsSerializer = new() { Indexes = null } });
        Assert.AreEqual("https://api.test/x?ids=1&ids=2&filter[name]=a+b", handler.Last!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public async Task ParamsSerializer_CustomSerialize_Wins()
    {
        var (api, handler) = Make();

        await api.Get<string>("https://api.test/x", new()
        {
            Params = { ["a"] = 1 },
            ParamsSerializer = new() { Serialize = p => $"custom={p.Count}" }
        });

        Assert.AreEqual("https://api.test/x?custom=1", handler.Last!.RequestUri!.AbsoluteUri);
    }

    [TestMethod]
    public void Params_TooDeep_ThrowsDepthError()
    {
        var (api, _) = Make();
        Dictionary<string, object?> deep = new() { ["a"] = new Dictionary<string, object?> { ["b"] = new Dictionary<string, object?> { ["c"] = 1 } } };

        AxiosException ex = Assert.ThrowsExactly<AxiosException>(() => api.GetUri(new() { Url = "https://api.test/x", Params = deep, ParamsSerializer = new() { MaxDepth = 2 } }));

        Assert.AreEqual(AxiosErrorCodes.FormDataDepthExceeded, ex.Code);
    }

    [TestMethod]
    public void GetUri_AppliesBaseUrlAndAbsoluteUrlRules()
    {
        var (api, _) = Make(defaults: new() { BaseUrl = "https://api.test/v1" });

        Assert.AreEqual("https://api.test/v1/users", api.GetUri(new() { Url = "users" }));
        Assert.AreEqual("https://other.test/a", api.GetUri(new() { Url = "https://other.test/a" }));
        Assert.AreEqual("https://api.test/v1/https://other.test/a", api.GetUri(new() { Url = "https://other.test/a", AllowAbsoluteUrls = false }));
    }

    [TestMethod]
    public async Task InvalidUrl_ThrowsErrInvalidUrl()
    {
        var (api, _) = Make();

        AxiosException ex = await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Get<string>("http:/missing-slashes"));
        Assert.AreEqual(AxiosErrorCodes.InvalidUrl, ex.Code);

        ex = await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Get<string>("/relative-without-base"));
        Assert.AreEqual(AxiosErrorCodes.InvalidUrl, ex.Code);
    }

    [TestMethod]
    public async Task Body_Urlencoded_Multipart_AndStringDefault()
    {
        var (api, handler) = Make();

        await api.Post<string>("https://api.test/x", new { Name = "a b", Tags = new[] { "x", "y" } },
            new() { Headers = { ["Content-Type"] = "application/x-www-form-urlencoded" } });
        Assert.AreEqual("name=a+b&tags[]=x&tags[]=y", handler.LastBody);

        await api.Post<string>("https://api.test/x", "raw");
        Assert.AreEqual("application/x-www-form-urlencoded", handler.Last!.Content!.Headers.ContentType!.MediaType);

        await api.PostForm<string>("https://api.test/x", new Dictionary<string, object?>
        {
            ["name"] = "Ada",
            ["file"] = new AxiosFile(Encoding.UTF8.GetBytes("hello"), "a.txt", "text/plain")
        });
        Assert.AreEqual("multipart/form-data", handler.Last!.Content!.Headers.ContentType!.MediaType);
        StringAssert.Contains(handler.LastBody, "filename=a.txt");
        StringAssert.Contains(handler.LastBody, "hello");
    }

    [TestMethod]
    public async Task Auth_SetsBasicHeader_OverwritingCustom()
    {
        var (api, handler) = Make();

        await api.Get<string>("https://api.test/x", new() { Auth = ("user", "pass"), Headers = { ["Authorization"] = "custom" } });

        Assert.AreEqual("Basic dXNlcjpwYXNz", handler.Last!.Headers.Authorization!.ToString());
    }

    [TestMethod]
    public async Task Transforms_RunOnRequestAndResponse()
    {
        var (api, handler) = Make(_ => Ok("hello", "text/plain"));

        AxiosResponse<string> res = await api.Post<string>("https://api.test/x", "in", new()
        {
            TransformRequest = [(data, headers) => { headers["X-T"] = "1"; return data + "!"; }],
            TransformResponse = [(data, _, status) => $"{data}:{status}"]
        });

        Assert.AreEqual("in!", handler.LastBody);
        Assert.AreEqual("1", handler.Last!.Headers.GetValues("X-T").Single());
        Assert.AreEqual("hello:200", res.Data);
    }

    [TestMethod]
    public async Task ResponseTypes_Bytes_Stream_AndMetadata()
    {
        var (api, _) = Make(_ => Ok("abc", "text/plain"));

        AxiosResponse<byte[]> bytes = await api.Get<byte[]>("https://api.test/x");
        CollectionAssert.AreEqual("abc"u8.ToArray(), bytes.Data);
        Assert.AreEqual("text/plain; charset=utf-8", bytes.Headers["content-type"]);
        Assert.IsNotNull(bytes.Request);
        Assert.IsTrue(bytes.Duration >= TimeSpan.Zero);

        AxiosResponse<Stream> stream = await api.Get<Stream>("https://api.test/x");
        using StreamReader reader = new(stream.Data!);
        Assert.AreEqual("abc", await reader.ReadToEndAsync());
    }

    [TestMethod]
    public async Task ErrorCodes_4xx_5xx_AndToJsonRedacts()
    {
        var (api, _) = Make(r => new(r.RequestUri!.AbsolutePath == "/a" ? HttpStatusCode.BadRequest : HttpStatusCode.BadGateway));

        AxiosException bad = await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Get<string>("https://api.test/a"));
        Assert.AreEqual(AxiosErrorCodes.BadRequest, bad.Code);

        AxiosException gateway = await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Get<string>("https://api.test/b",
            new() { Redact = ["authorization"], Headers = { ["Authorization"] = "secret" } }));
        Assert.AreEqual(AxiosErrorCodes.BadResponse, gateway.Code);

        var config = (Dictionary<string, object?>)gateway.ToJSON()["config"]!;
        var headers = (Dictionary<string, object?>)config["headers"]!;
        Assert.AreEqual("[REDACTED ****]", headers["Authorization"]);
        Assert.IsTrue(Axios.IsAxiosError(gateway));
    }

    [TestMethod]
    public async Task MaxContentLength_AndMaxBodyLength_Throw()
    {
        var (api, _) = Make(_ => Ok("0123456789", "text/plain"));

        AxiosException big = await Assert.ThrowsExactlyAsync<AxiosException>(
            () => api.Get<string>("https://api.test/x", new() { MaxContentLength = 5 }));
        Assert.AreEqual(AxiosErrorCodes.BadResponse, big.Code);

        AxiosException body = await Assert.ThrowsExactlyAsync<AxiosException>(
            () => api.Post<string>("https://api.test/x", "0123456789", new() { MaxBodyLength = 5 }));
        Assert.AreEqual(AxiosErrorCodes.BadRequest, body.Code);
    }

    [TestMethod]
    public async Task Cancellation_ThrowsCanceledError()
    {
        var (api, _) = Make();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        CanceledError ex = await Assert.ThrowsExactlyAsync<CanceledError>(
            () => api.Get<string>("https://api.test/x", new() { CancellationToken = cts.Token }));

        Assert.IsTrue(Axios.IsCancel(ex));
        Assert.AreEqual(AxiosErrorCodes.Canceled, ex.Code);
    }

    [TestMethod]
    public async Task Timeout_UsesEtimedoutWhenClarified()
    {
        AxiosClient api = Axios.Create(handler: new SlowHandler());

        AxiosException ex = await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Get<string>("https://api.test/x",
            new() { Timeout = TimeSpan.FromMilliseconds(50), Transitional = new() { ClarifyTimeoutError = true } }));

        Assert.AreEqual(AxiosErrorCodes.TimedOut, ex.Code);
    }

    [TestMethod]
    public async Task DownloadAndUploadProgress_AreReported()
    {
        var (api, _) = Make(_ => Ok(new string('x', 200_000), "text/plain"));
        List<AxiosProgressEvent> download = [], upload = [];

        await api.Post<string>("https://api.test/x", new byte[50_000], new()
        {
            OnDownloadProgress = download.Add,
            OnUploadProgress = upload.Add
        });

        Assert.AreEqual(200_000, download.Last().Loaded);
        Assert.IsTrue(download.Last().Download);
        Assert.AreEqual(50_000, upload.Last().Loaded);
        Assert.IsTrue(upload.Last().Upload);
    }

    [TestMethod]
    public async Task Interceptors_OrderIsLifoForRequestsAndFifoForResponses()
    {
        var (api, _) = Make();
        List<string> order = [];

        api.Interceptors.Request.Use(c => { order.Add("req1"); return c; });
        api.Interceptors.Request.Use(c => { order.Add("req2"); return c; });
        api.Interceptors.Response.Use(r => { order.Add("res1"); return r; });
        api.Interceptors.Response.Use(r => { order.Add("res2"); return r; });

        await api.Get<string>("https://api.test/x");

        CollectionAssert.AreEqual(new[] { "req2", "req1", "res1", "res2" }, order);
    }

    [TestMethod]
    public async Task Interceptors_RunWhen_Clear_AndErrorRecovery()
    {
        var (api, handler) = Make(r => new(r.Method == HttpMethod.Delete ? HttpStatusCode.NotFound : HttpStatusCode.OK));
        api.Interceptors.Request.Use(c => { c.Headers["X-Get"] = "1"; return c; },
            options: new() { RunWhen = c => c.Method == HttpMethod.Get });

        await api.Get<string>("https://api.test/x");
        Assert.IsTrue(handler.Last!.Headers.Contains("X-Get"));
        await api.Post<string>("https://api.test/x");
        Assert.IsFalse(handler.Last!.Headers.Contains("X-Get"));

        // A rejection handler can turn an error into a response.
        api.Interceptors.Response.Use(r => r, e => ((AxiosException)e).Response!);
        AxiosResponse<string> recovered = await api.Delete<string>("https://api.test/x");
        Assert.AreEqual(404, recovered.Status);

        api.Interceptors.Response.Clear();
        await Assert.ThrowsExactlyAsync<AxiosException>(() => api.Delete<string>("https://api.test/x"));
    }

    [TestMethod]
    public async Task Interceptors_SynchronousThrow_StillDispatchesWhenHandled()
    {
        var (api, handler) = Make();
        api.Interceptors.Request.Use(
            c => throw new InvalidOperationException("boom"),
            e => new AxiosRequestConfig(),
            new() { Synchronous = true });

        await api.Get<string>("https://api.test/x");

        Assert.IsNotNull(handler.Last);
    }

    [TestMethod]
    public void MergeConfig_MergesHeadersAndParams_ButNotUrlOrData()
    {
        AxiosRequestConfig a = new() { Url = "a", Data = "a", Headers = { ["X-A"] = "1" }, Params = { ["p"] = 1 }, Timeout = TimeSpan.FromSeconds(1) };
        AxiosRequestConfig b = new() { Headers = { ["X-B"] = "2" }, Params = { ["q"] = 2 } };

        AxiosRequestConfig merged = Axios.MergeConfig(a, b);

        Assert.IsNull(merged.Url);
        Assert.IsNull(merged.Data);
        Assert.AreEqual("1", merged.Headers["X-A"]);
        Assert.AreEqual("2", merged.Headers["X-B"]);
        Assert.AreEqual(2, merged.Params.Count);
        Assert.AreEqual(TimeSpan.FromSeconds(1), merged.Timeout);
    }

    private sealed class SlowHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new(HttpStatusCode.OK);
        }
    }
}
