using System.Collections;

namespace AxiosNet.Models;

/// <summary>Case-insensitive header collection, modeled after axios' <c>AxiosHeaders</c>.</summary>
public class AxiosHeaders : IEnumerable<KeyValuePair<string, string>>
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates an empty collection.</summary>
    public AxiosHeaders()
    {
    }

    /// <summary>Creates a collection holding a copy of <paramref name="source"/>.</summary>
    /// <param name="source">The headers to copy.</param>
    public AxiosHeaders(IEnumerable<KeyValuePair<string, string>> source)
    {
        foreach ((string name, string value) in source)
        {
            _headers[name] = value;
        }
    }

    /// <summary>Number of headers.</summary>
    public int Count => _headers.Count;

    /// <summary>Gets a header (null when absent). Assigning null removes it.</summary>
    public string? this[string name]
    {
        get => Get(name);
        set
        {
            if (value is null)
            {
                Delete(name);
            }
            else
            {
                Set(name, value);
            }
        }
    }

    /// <summary>Sets a header, replacing any previous value.</summary>
    /// <param name="name">Header name, case-insensitive.</param>
    /// <param name="value">Header value.</param>
    public AxiosHeaders Set(string name, string value)
    {
        _headers[name] = value;

        return this;
    }

    /// <summary>Sets several headers, replacing any previous values.</summary>
    /// <param name="headers">The headers to set.</param>
    public AxiosHeaders Set(IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach ((string name, string value) in headers)
        {
            _headers[name] = value;
        }

        return this;
    }

    /// <summary>Enables collection initializers: <c>new AxiosHeaders { { "Accept", "text/plain" } }</c>.</summary>
    public void Add(string name, string value) => Set(name, value);

    /// <summary>Gets a header value, or null when absent.</summary>
    public string? Get(string name) => _headers.GetValueOrDefault(name);

    /// <summary>True when the header is present.</summary>
    public bool Has(string name) => _headers.ContainsKey(name);

    /// <summary>Removes a header and returns whether it was present.</summary>
    public bool Delete(string name) => _headers.Remove(name);

    /// <summary>Removes every header.</summary>
    public AxiosHeaders Clear()
    {
        _headers.Clear();

        return this;
    }

    /// <summary>Returns a new instance holding this collection followed by <paramref name="others"/>.</summary>
    public AxiosHeaders Concat(params AxiosHeaders[] others)
    {
        AxiosHeaders result = Clone();

        foreach (AxiosHeaders other in others)
        {
            result.Set(other);
        }

        return result;
    }

    /// <summary>Returns a copy of this collection.</summary>
    public AxiosHeaders Clone() => new(_headers);

    /// <summary>Parses raw "Name: value" lines.</summary>
    public static AxiosHeaders Parse(string raw)
    {
        AxiosHeaders headers = new();

        foreach (string line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = line.IndexOf(':');

            if (colon <= 0)
            {
                continue;
            }

            headers.Set(line[..colon].Trim(), line[(colon + 1)..].Trim());
        }

        return headers;
    }

    /// <summary>Plain dictionary copy, convenient for serialization.</summary>
    public Dictionary<string, string> ToJson() => new(_headers, StringComparer.OrdinalIgnoreCase);

    /// <summary>Raw <c>Name: value</c> lines, one per header.</summary>
    public override string ToString()
        => string.Concat(_headers.Select(h => $"{h.Key}: {h.Value}\n"));

    /// <summary>Enumerates the headers.</summary>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
