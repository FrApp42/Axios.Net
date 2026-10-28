# AxiosNet

A .NET HTTP client with an [axios](https://axios-http.com)-like syntax.

```csharp
using AxiosNet;

// Direct requests
AxiosResponse<User> res = await Axios.Get<User>("https://api.test/users/1");
Console.WriteLine($"{res.Status} {res.Data!.Name}");

await Axios.Post<User>("https://api.test/users", new { Name = "Ada" });

// Configured instance (axios.create)
AxiosClient api = Axios.Create(new()
{
    BaseUrl = "https://api.test/v1",
    Timeout = TimeSpan.FromSeconds(10),
    Headers = { ["Authorization"] = "Bearer token" }
});

var list = await api.Get<List<User>>("/users", new() { Params = { ["page"] = 2 } });

// Interceptors
api.Interceptors.Request.Use(c => { c.Headers["X-Trace"] = Guid.NewGuid().ToString(); return c; });
api.Interceptors.Error.Use(e => { Console.WriteLine(e.Status); return e; });

// Errors (non-2xx status, timeout, cancellation, network failure)
try { await api.Get<User>("/missing"); }
catch (AxiosException e) { Console.WriteLine($"{e.Code} {e.Status} {e.Response?.Data}"); }
```

- `T` = `string` or `byte[]`: raw body; otherwise the body is read as JSON (System.Text.Json, web defaults).
- `data`: `string` is sent as text, `byte[]` as binary, `HttpContent` (e.g. `MultipartFormDataContent`) as is, any other object as JSON.
- Use `ValidateStatus` to avoid the exception for specific status codes.
