using System.Net;

namespace AxiosNet.Config;

public class AxiosProxy
{
    /// <summary>Pass as <see cref="AxiosRequestConfig.Proxy"/> to ignore system and environment proxies.</summary>
    public static readonly AxiosProxy Disabled = new();

    public string Protocol { get; set; } = "http";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public (string Username, string Password)? Auth { get; set; }

    internal WebProxy ToWebProxy() => new($"{Protocol}://{Host}:{Port}")
    {
        Credentials = Auth is (string user, string password) ? new NetworkCredential(user, password) : null
    };
}
