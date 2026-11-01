using AxiosNet.Config;

namespace AxiosNet.Errors;

/// <summary>Thrown when a request is canceled through <see cref="AxiosRequestConfig.CancellationToken"/>.</summary>
public class CanceledError : AxiosException
{
    /// <summary>Creates the error of a canceled request.</summary>
    /// <param name="config">The config the request was sent with.</param>
    /// <param name="message">Description of the cancellation.</param>
    /// <param name="request">The request that was sent, when it got that far.</param>
    /// <param name="inner">The exception that caused this one.</param>
    public CanceledError(
        AxiosRequestConfig config,
        string message = "canceled",
        HttpRequestMessage? request = null,
        Exception? inner = null
    ) : base(message, AxiosErrorCodes.Canceled, config, request, null, inner)
    {
    }
}
