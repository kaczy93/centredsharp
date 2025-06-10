using System.Collections.Generic;

namespace CentrED;

public class SparseGrid<T>
{
    private readonly Dictionary<(int X, int Y), T?> _data = new();

    public T? this[int x, int y]
    {
        get => _data.TryGetValue((x, y), out var value) ? value : default;
        set
        {
            var key = (x, y);
            if (value == null)
            {
                _data.Remove(key);
            }
            else
            {
                _data[key] = value;
            }
        }
    }

    public IEnumerable<T?> Values => _data.Values;

    public void Clear() => _data.Clear();
}
