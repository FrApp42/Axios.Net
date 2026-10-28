namespace AxiosNet;

/// <summary>A file part for multipart bodies.</summary>
public sealed class AxiosFile
{
    public object Content { get; }

    public string FileName { get; }

    public string? ContentType { get; }

    public AxiosFile(byte[] content, string fileName, string? contentType = null)
        => (Content, FileName, ContentType) = (content, fileName, contentType);

    public AxiosFile(Stream content, string fileName, string? contentType = null)
        => (Content, FileName, ContentType) = (content, fileName, contentType);
}
