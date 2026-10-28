namespace AxiosNet;

public class AxiosTransitional
{
    /// <summary
    /// >With an explicit <see cref="AxiosResponseType.Json"/>, a JSON parse failure yields default instead of throwing.
    /// </summary>
    public bool SilentJsonParsing { get; set; } = true;

    /// <summary>When false, <c>T = object</c> returns the raw text instead of parsing it as JSON.</summary>
    public bool ForcedJsonParsing { get; set; } = true;

    /// <summary>Timeouts fail with ETIMEDOUT instead of ECONNABORTED.</summary>
    public bool ClarifyTimeoutError { get; set; }
}
