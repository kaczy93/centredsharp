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
            return CreateGeneric<T>(nameof(CreateList), type.GetGenericArguments()[0]);
        throw new NotSupportedException($"No config converter registered for {type}");
    }

    private static ConfigConverter<T> CreateGeneric<T>(string methodName, Type elementType)
    {
        var method = typeof(ConfigConverters).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;
        return (ConfigConverter<T>)method.MakeGenericMethod(elementType).Invoke(null, null)!;
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
        return string.Join(',', items.Select(s => s.Replace("\\", "\\\\").Replace(",", "\\,")));
    }
}
