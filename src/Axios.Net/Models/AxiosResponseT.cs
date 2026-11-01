namespace AxiosNet.Models;

/// <summary>A response whose body is converted to <typeparamref name="T"/>.</summary>
/// <typeparam name="T">Type of the body.</typeparam>
public class AxiosResponse<T> : AxiosResponse
{
    /// <summary>Body converted to <typeparamref name="T"/> (default when empty).</summary>
    public new T? Data
    {
        get => base.Data is T value ? value : default;
        init => base.Data = value;
    }
}
