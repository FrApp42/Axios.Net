# Axios.Net

A .NET HTTP client with an [axios](https://axios.rest)-like syntax.

```csharp
using AxiosNet.Client;   // Axios, AxiosClient
using AxiosNet.Config;   // AxiosRequestConfig, AxiosProxy, ...
using AxiosNet.Errors;   // AxiosException, CanceledError, AxiosErrorCodes
using AxiosNet.Models;   // AxiosResponse<T>, AxiosHeaders, AxiosFile, AxiosProgressEvent

// Direct requests
AxiosResponse<User> res = await Axios.Get<User>("https://api.test/users/1");
Console.WriteLine($"{res.Status} {res.Data!.Name} in {res.Duration.TotalMilliseconds}ms");

await Axios.Post<User>("https://api.test/users", new { Name = "Ada" });

// Configured instance (axios.create)
AxiosClient api = Axios.Create(new()
{
    BaseUrl = "https://api.test/v1",
    Timeout = TimeSpan.FromSeconds(10),
    Headers = { ["X-App"] = "demo" }
});
api.Defaults.CommonHeaders["Authorization"] = "Bearer token"; // axios defaults.headers.common
api.Defaults.MethodHeaders["post"] = new() { { "X-Post", "1" } }; // axios defaults.headers.post

var list = await api.Get<List<User>>("/users", new() { Params = { ["page"] = 2 } });
```

## API

| axios | AxiosNet |
|---|---|
| `axios.request(config)` | `Axios.Request<T>(config)` |
| `get / delete / head / options` | `Get<T> / Delete<T> / Head<T> / Options<T>(url, config)` |
| `post / put / patch` | `Post<T> / Put<T> / Patch<T>(url, data, config)` |
| `postForm / putForm / patchForm` | `PostForm<T> / PutForm<T> / PatchForm<T>` |
| `axios.getUri(config)` | `Axios.GetUri(config)` |
| `axios.create(config)` | `Axios.Create(config, handler)` |
| `axios.defaults` / `instance.defaults` | `Axios.Defaults` / `client.Defaults` |
| `axios.all` | `Axios.All(tasks)` |
| `axios.isAxiosError / isCancel` | `Axios.IsAxiosError / IsCancel` |
| `axios.mergeConfig` | `Axios.MergeConfig` |
| `axios.toFormData` | `Axios.ToFormData` |
| `AxiosHeaders` | `AxiosHeaders` |
| `CanceledError` | `CanceledError` |

Config precedence: library defaults, then client defaults, then the request config.
`Url`, `Method` and `Data` are never inherited from defaults; headers and params are merged key by key.

## Namespaces

The package and assembly are named `Axios.Net`; the root namespace is `AxiosNet` so that it does not clash with the static `Axios` class.

| Namespace | Types |
|---|---|
| `AxiosNet.Client` | `Axios`, `AxiosClient` |
| `AxiosNet.Config` | `AxiosRequestConfig`, `AxiosParamsSerializer`, `AxiosFormSerializer`, `AxiosTransitional`, `AxiosProxy`, `AxiosResponseType` |
| `AxiosNet.Errors` | `AxiosException`, `CanceledError`, `AxiosErrorCodes` |
| `AxiosNet.Models` | `AxiosResponse`, `AxiosResponse<T>`, `AxiosHeaders`, `AxiosFile`, `AxiosProgressEvent` |
| `AxiosNet.Interceptors` | `AxiosInterceptors`, `InterceptorManager<T>`, `InterceptorOptions` |

## Request config

`Url`, `Method`, `BaseUrl`, `AllowAbsoluteUrls`, `Headers`, `MethodHeaders` / `CommonHeaders`, `Params`,
`ParamsSerializer` (`Encode`, `Serialize`, `Indexes`, `MaxDepth`), `Data`, `TransformRequest`,
`TransformResponse`, `Timeout`, `Auth`, `ResponseType`, `ResponseEncoding`, `ValidateStatus`,
`MaxContentLength`, `MaxBodyLength`, `MaxRedirects`, `Decompress`, `Proxy`, `OnUploadProgress`,
`OnDownloadProgress`, `Redact`, `Transitional` (`SilentJsonParsing`, `ForcedJsonParsing`,
`ClarifyTimeoutError`), `FormSerializer` (`Dots`, `MetaTokens`, `Indexes`, `MaxDepth`) and
`CancellationToken` (axios' `signal`).

## Bodies and responses

- `Data`: `string` is sent as is (`application/x-www-form-urlencoded` unless a Content-Type header is set, like axios),
  `byte[]` / `Stream` as binary, `HttpContent` as is, any other object as JSON.
  Set `Content-Type: application/x-www-form-urlencoded` or `multipart/form-data` (or use `PostForm`) to serialize an
  object that way. Use `AxiosFile` for file parts.
- `T`: `string`, `byte[]` and `Stream` return the raw body; anything else is read as JSON (System.Text.Json, web defaults).
  Override with `ResponseType`.
- Query arrays follow axios: `a[]=1&a[]=2` by default; use `Indexes = null` for `a=1&a=2` or `true` for `a[0]=1`.
- Responses expose `Status`, `StatusText`, `Headers` (lowercase names), `Config`, `Request`, `Duration` and `Data`.

## Errors

Every failure throws an `AxiosException` (`Code`, `Config`, `Request`, `Response`, `Status`, `ToJSON()`).
Codes are in `AxiosErrorCodes`: `ERR_BAD_REQUEST` (4xx), `ERR_BAD_RESPONSE`, `ERR_NETWORK`, `ERR_CANCELED`
(`CanceledError`), `ERR_INVALID_URL`, `ERR_FORM_DATA_DEPTH_EXCEEDED`, `ECONNABORTED` / `ETIMEDOUT` (timeouts).
Use `ValidateStatus` to resolve selected status codes, and `Redact` to mask keys in `ToJSON()`.

```csharp
try { await api.Get<User>("/missing"); }
catch (AxiosException e) { Console.WriteLine($"{e.Code} {e.Status} {e.Response?.Data}"); }
```

## Interceptors

```csharp
int id = api.Interceptors.Request.Use(
    config => { config.Headers["X-Trace"] = Guid.NewGuid().ToString(); return config; },
    options: new() { RunWhen = c => c.Method == HttpMethod.Get, Synchronous = true });

api.Interceptors.Response.Use(
    response => response,
    error => error is AxiosException { Response: { } r } ? r : throw error); // recover or rethrow

api.Interceptors.Request.Eject(id);
api.Interceptors.Response.Clear();
```

Request interceptors run last registered first, response interceptors first registered first.

## Not applicable in .NET

These axios options are tied to browsers or Node.js and have no equivalent: `adapter` (pass an
`HttpMessageHandler` to `Axios.Create` instead), `xsrfCookieName`, `xsrfHeaderName`, `withXSRFToken`,
`withCredentials`, `httpAgent`, `httpsAgent`, `socketPath`, `allowedSocketPaths`, `transport`,
`insecureHTTPParser`, `formDataHeaderPolicy`, `env`, `maxRate`, `beforeRedirect`, `sensitiveHeaders`,
`parseReviver` (configure `AxiosClient.JsonOptions`), and the deprecated `CancelToken` (use `CancellationToken`).
`MaxRedirects`, `Decompress` and `Proxy` need the built-in handler (they are ignored with a custom one).
