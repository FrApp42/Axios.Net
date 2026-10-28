using System.Collections;

namespace AxiosNet;

/// <summary>Case-insensitive header collection, modeled after axios' <c>AxiosHeaders</c>.</summary>
public class AxiosHeaders : IEnumerable<KeyValuePair<string, string>>
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public AxiosHeaders() { }

    public AxiosHeaders(IEnumerable<KeyValuePair<string, string>> source)
    {
        foreach (var (name, value) in source) _headers[name] = value;
    }

    public int Count => _headers.Count;

    /// <summary>Gets a header (null when absent). Assigning null removes it.</summary>
    public string? this[string name]
    {
        get => Get(name);
        set
        {
            if (value is null) Delete(name);
            else Set(name, value);
        }
    }

    public AxiosHeaders Set(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public AxiosHeaders Set(IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var (name, value) in headers) _headers[name] = value;
        return this;
    }

    /// <summary>Enables collection initializers: <c>new AxiosHeaders { { "Accept", "text/plain" } }</c>.</summary>
    public void Add(string name, string value) => Set(name, value);

    public string? Get(string name) => _headers.GetValueOrDefault(name);

    public bool Has(string name) => _headers.ContainsKey(name);

    public bool Delete(string name) => _headers.Remove(name);

    public AxiosHeaders Clear()
    {
        _headers.Clear();
        return this;
    }

    /// <summary>Returns a new instance holding this collection followed by <paramref name="others"/>.</summary>
    public AxiosHeaders Concat(params AxiosHeaders[] others)
    {
        AxiosHeaders result = Clone();
        foreach (AxiosHeaders other in others) result.Set(other);
        return result;
    }

    public AxiosHeaders Clone() => new(_headers);

    /// <summary>Parses raw "Name: value" lines.</summary>
    public static AxiosHeaders Parse(string raw)
    {
        AxiosHeaders headers = new();

        foreach (string line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;

            headers.Set(line[..colon].Trim(), line[(colon + 1)..].Trim());
        }

        return headers;
    }

    /// <summary>Plain dictionary copy, convenient for serialization.</summary>
    public Dictionary<string, string> ToJson() => new(_headers, StringComparer.OrdinalIgnoreCase);

    public override string ToString() => string.Concat(_headers.Select(h => $"{h.Key}: {h.Value}\n"));

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
