using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace CentrED;

public sealed class ConfigConverter<T>(Func<string, T> parse, Func<T, string> format)
{
    public T Parse(string text) => parse(text);
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
        Register(s => s, s => s);
        Register(bool.Parse, v => v ? "true" : "false");
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

    public static void Register<T>(Func<string, T> parse, Func<T, string> format)
    {
        Cache<T>.Instance = new ConfigConverter<T>(parse, format);
    }

    public static ConfigConverter<T> Get<T>() => Cache<T>.Instance ??= Create<T>();

    private static void RegisterInteger<T>() where T : IBinaryInteger<T>
    {
        Register(ParseInteger<T>, v => v.ToString(null, CultureInfo.InvariantCulture));
    }

    private static T ParseInteger<T>(string s) where T : IBinaryInteger<T>
    {
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return T.Parse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return T.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static void RegisterFloat<T>() where T : IFloatingPoint<T>
    {
        Register
        (
            s => T.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
            v => v.ToString(null, CultureInfo.InvariantCulture)
        );
    }

    private static ConfigConverter<T> Create<T>()
    {
        var type = typeof(T);
        if (type.IsEnum)
            return new ConfigConverter<T>(s => (T)Enum.Parse(type, s, true), v => v!.ToString()!);
        if (type.IsSZArray)
            return CreateGeneric<T>(nameof(CreateArray), type.GetElementType()!);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return CreateGeneric<T>(nameof(CreateList), type.GetGenericArguments());
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            return CreateGeneric<T>(nameof(CreateDictionary), type.GetGenericArguments());
        throw new NotSupportedException($"No config converter registered for {type}");
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
            (s => SplitList(s).Select(element.Parse).ToArray(), v => JoinList(v.Select(element.Format)));
    }

    private static ConfigConverter<List<TElement>> CreateList<TElement>()
    {
        var element = Get<TElement>();
        return new ConfigConverter<List<TElement>>
            (s => SplitList(s).Select(element.Parse).ToList(), v => JoinList(v.Select(element.Format)));
    }

    private static ConfigConverter<Dictionary<TKey, TValue>> CreateDictionary<TKey, TValue>() where TKey : notnull
    {
        var key = Get<TKey>();
        var value = Get<TValue>();
        return new ConfigConverter<Dictionary<TKey, TValue>>
        (
            s => SplitDictionary(s).ToDictionary(p => key.Parse(p.Key), p => value.Parse(p.Value)),
            v => string.Join(';', v.Select(p => $"{EscapeItem(key.Format(p.Key))},{EscapeItem(value.Format(p.Value))}"))
        );
    }

    private static List<KeyValuePair<string, string>> SplitDictionary(string text)
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
                result.Add(new(key ?? throw new FormatException($"Missing ',' in dictionary entry '{sb}'"), sb.ToString()));
                key = null;
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(new(key ?? throw new FormatException($"Missing ',' in dictionary entry '{sb}'"), sb.ToString()));
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
