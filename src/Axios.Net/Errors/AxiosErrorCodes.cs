namespace AxiosNet.Errors;

/// <summary>Values of <see cref="AxiosException.Code"/>, same identifiers as axios.</summary>
public static class AxiosErrorCodes
{
    public const string BadOption = "ERR_BAD_OPTION";
    public const string BadOptionValue = "ERR_BAD_OPTION_VALUE";
    public const string BadRequest = "ERR_BAD_REQUEST";
    public const string BadResponse = "ERR_BAD_RESPONSE";
    public const string Network = "ERR_NETWORK";
    public const string Canceled = "ERR_CANCELED";
    public const string InvalidUrl = "ERR_INVALID_URL";
    public const string FormDataDepthExceeded = "ERR_FORM_DATA_DEPTH_EXCEEDED";

    /// <summary>Request timed out (default).</summary>
    public const string Timeout = "ECONNABORTED";

    /// <summary>Request timed out with <c>Transitional.ClarifyTimeoutError</c> enabled.</summary>
    public const string TimedOut = "ETIMEDOUT";
}
