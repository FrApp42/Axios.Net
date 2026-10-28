namespace AxiosNet;

/// <summary>
/// Thrown when a request fails: non-validated status, timeout, cancellation or network error.
/// </summary>
public class AxiosException : Exception
{
    /// <summary>ERR_BAD_RESPONSE, ECONNABORTED (timeout), ERR_CANCELED or ERR_NETWORK.</summary>
    public string Code { get; }

    public AxiosRequestConfig Config { get; }

    /// <summary>Set when the server answered; <see cref="AxiosResponse.Data"/> holds the raw body text.</summary>
    public AxiosResponse? Response { get; }

    public int? Status => Response?.Status;

    public AxiosException(string message, string code, AxiosRequestConfig config, AxiosResponse? response = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Config = config;
        Response = response;
    }
}
