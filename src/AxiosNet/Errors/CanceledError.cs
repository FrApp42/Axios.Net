namespace AxiosNet;

/// <summary>Thrown when a request is canceled through <see cref="AxiosRequestConfig.CancellationToken"/>.</summary>
public class CanceledError : AxiosException
{
    public CanceledError(
        AxiosRequestConfig config,
        string message = "canceled",
        HttpRequestMessage? request = null,
        Exception? inner = null
    ) : base(message, AxiosErrorCodes.Canceled, config, request, null, inner)
    {
    }
}
