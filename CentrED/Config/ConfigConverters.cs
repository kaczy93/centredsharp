using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace CentrED;

public delegate bool ConfigTryParse<T>(string text, out T value);

public sealed class ConfigConverter<T>(ConfigTryParse<T> tryParse, Func<T, string> format)
{
    public bool TryParse(string text, out T value) => tryParse(text, out value);
    public string Format(T value) => format(value);
}

public static class ConfigConverters
{
    private static class Cache<T>
    {
        public static ConfigConverter<T>? Instance;
    }

    static ConfigConverters()
    {
        Register
        (
            (string text, out string value) =>
            {
                value = text;
                return true;
            },
            v => v
        );
        Register<bool>(bool.TryParse, v => v ? "true" : "false");
        RegisterInteger<byte>();
        RegisterInteger<sbyte>();
        RegisterInteger<short>();
        RegisterInteger<ushort>();
        RegisterInteger<int>();
        RegisterInteger<uint>();
        RegisterInteger<long>();
        RegisterInteger<ulong>();
        RegisterFloat<float>();
        RegisterFloat<double>();
    }

    public static void Register<T>(ConfigTryParse<T> tryParse, Func<T, string> format)
    {
        Cache<T>.Instance = new ConfigConverter<T>(tryParse, format);
    }

    public static void Register<T>(Func<string, T> parse, Func<T, string> format)
    {
        Register
        (
            (text, out value) =>
            {
                try
                {
                    value = parse(text);
                    return true;
                }
                catch
                {
                    value = default!;
                    return false;
                }
            },
            format
        );
    }

    public static ConfigConverter<T> Get<T>() => Cache<T>.Instance ??= Create<T>();

    private static void RegisterInteger<T>() where T : IBinaryInteger<T>
    {
        Register<T>(TryParseInteger, v => v.ToString(null, CultureInfo.InvariantCulture));
    }

    private static bool TryParseInteger<T>(string s, out T value) where T : IBinaryInteger<T>
    {
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return T.TryParse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value!);
        return T.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value!);
    }

    private static void RegisterFloat<T>() where T : IFloatingPoint<T>
    {
        Register
        (
            (string s, out T value) => T.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value!),
            v => v.ToString(null, CultureInfo.InvariantCulture)
        );
    }

    private static ConfigConverter<T> Create<T>()
    {
        var type = typeof(T);
        if (type.IsEnum)
            return new ConfigConverter<T>(TryParseEnum, v => v!.ToString()!);
        if (type.IsSZArray)
            return CreateGeneric<T>(nameof(CreateArray), type.GetElementType()!);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return CreateGeneric<T>(nameof(CreateList), type.GetGenericArguments());
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return CreateGeneric<T>(nameof(CreateDictionary), type.GetGenericArguments());
        throw new NotSupportedException($"No config converter registered for {type}");
    }

    private static bool TryParseEnum<T>(string s, out T value)
    {
        if (Enum.TryParse(typeof(T), s, true, out var result))
        {
            value = (T)result;
            return true;
        }
        value = default!;
        return false;
    }

    private static ConfigConverter<T> CreateGeneric<T>(string methodName, params Type[] typeArguments)
    {
        var method = typeof(ConfigConverters).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
        return (ConfigConverter<T>)method.MakeGenericMethod(typeArguments).Invoke(null, null)!;
    }

    private static ConfigConverter<TElement[]> CreateArray<TElement>()
    {
        var element = Get<TElement>();
        return new ConfigConverter<TElement[]>
        (
            (s, out value) =>
            {
                var success = TryParseList(element, s, out var list);
                value = success ? list.ToArray() : [];
                return success;
            },
            v => JoinList(v.Select(element.Format))
        );
    }

    private static ConfigConverter<List<TElement>> CreateList<TElement>()
    {
        var element = Get<TElement>();
        return new ConfigConverter<List<TElement>>
            ((s, out value) => TryParseList(element, s, out value), v => JoinList(v.Select(element.Format)));
    }

    private static bool TryParseList<TElement>(ConfigConverter<TElement> element, string text, out List<TElement> value)
    {
        value = [];
        foreach (var item in SplitList(text))
        {
            if (!element.TryParse(item, out var parsed))
                return false;
            value.Add(parsed);
        }
        return true;
    }

    private static ConfigConverter<Dictionary<TKey, TValue>> CreateDictionary<TKey, TValue>() where TKey : notnull
    {
        var key = Get<TKey>();
        var value = Get<TValue>();
        return new ConfigConverter<Dictionary<TKey, TValue>>
        (
            (s, out result) => TryParseDictionary(key, value, s, out result),
            v => string.Join(';', v.Select(p => $"{EscapeItem(key.Format(p.Key))},{EscapeItem(value.Format(p.Value))}"))
        );
    }

    private static bool TryParseDictionary<TKey, TValue>
        (ConfigConverter<TKey> key, ConfigConverter<TValue> value, string text, out Dictionary<TKey, TValue> result)
        where TKey : notnull
    {
        result = new Dictionary<TKey, TValue>();
        var pairs = SplitDictionary(text);
        if (pairs == null)
            return false;
        foreach (var (rawKey, rawValue) in pairs)
        {
            if (!key.TryParse(rawKey, out var parsedKey) || !value.TryParse(rawValue, out var parsedValue))
                return false;
            result[parsedKey] = parsedValue;
        }
        return true;
    }

    private static List<KeyValuePair<string, string>>? SplitDictionary(string text)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (text.Length == 0)
            return result;
        var sb = new StringBuilder();
        string? key = null;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                sb.Append(text[++i]);
            }
            else if (c == ',' && key == null)
            {
                key = sb.ToString();
                sb.Clear();
            }
            else if (c == ';')
            {
                if (key == null)
                    return null;
                result.Add(new(key, sb.ToString()));
                key = null;
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        if (key == null)
            return null;
        result.Add(new(key, sb.ToString()));
        return result;
    }

    private static List<string> SplitList(string text)
    {
        var result = new List<string>();
        if (text.Length == 0)
            return result;
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                sb.Append(text[++i]);
            }
            else if (c == ',')
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private static string JoinList(IEnumerable<string> items)
    {
        return string.Join(',', items.Select(EscapeItem));
    }

    private static string EscapeItem(string item)
    {
        return item.Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;");
    }
}
