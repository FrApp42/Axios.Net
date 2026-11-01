namespace AxiosNet.Errors;

/// <summary>Values of <see cref="AxiosException.Code"/>, same identifiers as axios.</summary>
public static class AxiosErrorCodes
{
    /// <summary>A config option is not valid.</summary>
    public const string BadOption = "ERR_BAD_OPTION";

    /// <summary>A config option has an invalid value.</summary>
    public const string BadOptionValue = "ERR_BAD_OPTION_VALUE";

    /// <summary>The server answered with a 4xx status that was rejected.</summary>
    public const string BadRequest = "ERR_BAD_REQUEST";

    /// <summary>The status was rejected, a size limit was exceeded or the body could not be read.</summary>
    public const string BadResponse = "ERR_BAD_RESPONSE";

    /// <summary>The request could not reach the server.</summary>
    public const string Network = "ERR_NETWORK";

    /// <summary>The request was canceled.</summary>
    public const string Canceled = "ERR_CANCELED";

    /// <summary>The URL is invalid or not absolute.</summary>
    public const string InvalidUrl = "ERR_INVALID_URL";

    /// <summary>Params or a form object are nested too deeply.</summary>
    public const string FormDataDepthExceeded = "ERR_FORM_DATA_DEPTH_EXCEEDED";

    /// <summary>Request timed out (default).</summary>
    public const string Timeout = "ECONNABORTED";

    /// <summary>Request timed out with <c>Transitional.ClarifyTimeoutError</c> enabled.</summary>
    public const string TimedOut = "ETIMEDOUT";
}
