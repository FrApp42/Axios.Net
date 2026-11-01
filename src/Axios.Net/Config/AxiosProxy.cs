using System.Net;

namespace AxiosNet.Config;

/// <summary>Proxy settings of a request.</summary>
public class AxiosProxy
{
    /// <summary>Pass as <see cref="AxiosRequestConfig.Proxy"/> to ignore system and environment proxies.</summary>
    public static readonly AxiosProxy Disabled = new();

    /// <summary>Proxy scheme, such as <c>http</c>.</summary>
    public string Protocol { get; set; } = "http";

    /// <summary>Proxy host name or address.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Proxy port.</summary>
    public int Port { get; set; }

    /// <summary>User name and password sent to the proxy, when it requires them.</summary>
    public (string Username, string Password)? Auth { get; set; }

    internal WebProxy ToWebProxy() => new($"{Protocol}://{Host}:{Port}")
    {
        Credentials = Auth is (string user, string password) ? new NetworkCredential(user, password) : null
    };
}
