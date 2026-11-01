namespace AxiosNet.Models;

/// <summary>A file part for multipart bodies.</summary>
public sealed class AxiosFile
{
    /// <summary>The file content, a <c>byte[]</c> or a <see cref="Stream"/>.</summary>
    public object Content { get; }

    /// <summary>File name sent with the multipart part.</summary>
    public string FileName { get; }

    /// <summary>Media type of the part, when set.</summary>
    public string? ContentType { get; }

    /// <summary>Creates a file part from bytes.</summary>
    /// <param name="content">The file content.</param>
    /// <param name="fileName">File name sent with the part.</param>
    /// <param name="contentType">Media type of the part.</param>
    public AxiosFile(byte[] content, string fileName, string? contentType = null)
        => (Content, FileName, ContentType) = (content, fileName, contentType);

    /// <summary>Creates a file part from a stream.</summary>
    /// <param name="content">The file content, read when the request is sent.</param>
    /// <param name="fileName">File name sent with the part.</param>
    /// <param name="contentType">Media type of the part.</param>
    public AxiosFile(Stream content, string fileName, string? contentType = null)
        => (Content, FileName, ContentType) = (content, fileName, contentType);
}
