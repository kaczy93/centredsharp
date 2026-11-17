using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using CentrED.IO;

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
        RegisterIntegerTuple2<ushort>();
        RegisterVector4();
        LandBrushManager.RegisterConfigConverters(); //This can be called directly in lb manager, once we get rid of legacy profile migration
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

    public static bool TryParseInteger<T>(string s, out T value) where T : IBinaryInteger<T>
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
        if (IsSet(type, out var setElement))
            return CreateGeneric<T>(nameof(CreateSet), type, setElement);
        throw new NotSupportedException($"No config converter registered for {type}");
    }

    private static bool TryParseEnum<T>(string s, out T value)
    {
        if (Enum.TryParse(typeof(T), s, true, out var result) && IsDefinedEnum(result))
        {
            value = (T)result;
            return true;
        }
        value = default!;
        return false;
    }

    // Enum.ToString falls back to a number when the value (or any flag bit) isn't defined
    private static bool IsDefinedEnum(object value)
    {
        var name = value.ToString()!;
        return name.Length > 0 && !char.IsAsciiDigit(name[0]) && name[0] != '-';
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
            v => Join(v.Select(element.Format), ',')
        );
    }

    private static ConfigConverter<List<TElement>> CreateList<TElement>()
    {
        var element = Get<TElement>();
        return new ConfigConverter<List<TElement>>
            ((s, out value) => TryParseList(element, s, out value), v => Join(v.Select(element.Format), ','));
    }

    private static bool IsSet(Type type, [NotNullWhen(true)] out Type? element)
    {
        element = null;
        if (!type.IsGenericType || type.GetConstructor(Type.EmptyTypes) == null)
            return false;
        var args = type.GetGenericArguments();
        if (args.Length != 1 || !typeof(ISet<>).MakeGenericType(args[0]).IsAssignableFrom(type))
            return false;
        element = args[0];
        return true;
    }

    private static ConfigConverter<TSet> CreateSet<TSet, TElement>() where TSet : ISet<TElement>, new()
    {
        var element = Get<TElement>();
        return new ConfigConverter<TSet>
        (
            (s, out value) =>
            {
                value = new TSet();
                if (!TryParseList(element, s, out var list))
                    return false;
                foreach (var item in list)
                    value.Add(item);
                return true;
            },
            v => Join(v.Select(element.Format), ',')
        );
    }

    private static bool TryParseList<TElement>(ConfigConverter<TElement> element, string text, out List<TElement> value)
    {
        value = [];
        foreach (var item in Split(text, ','))
        {
            if (item.Length == 0 || !element.TryParse(item, out var parsed))
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
            v => Join(v.Select(p => $"{Escape(key.Format(p.Key), ':')}:{value.Format(p.Value)}"), ';')
        );
    }

    private static bool TryParseDictionary<TKey, TValue>
        (ConfigConverter<TKey> key, ConfigConverter<TValue> value, string text, out Dictionary<TKey, TValue> result)
        where TKey : notnull
    {
        result = new Dictionary<TKey, TValue>();
        foreach (var pair in Split(text, ';'))
        {
            var separator = IndexOfUnescaped(pair, ':', 0);
            if (separator < 0 || !key.TryParse(Unescape(pair[..separator], ':'), out var parsedKey) ||
                !value.TryParse(pair[(separator + 1)..], out var parsedValue))
                return false;
            result[parsedKey] = parsedValue;
        }
        return true;
    }

    private static List<string> Split(string text, char delimiter)
    {
        var result = new List<string>();
        if (text.Length == 0)
            return result;
        var start = 0;
        int end;
        while ((end = IndexOfUnescaped(text, delimiter, start)) >= 0)
        {
            result.Add(Unescape(text[start..end], delimiter));
            start = end + 1;
        }
        result.Add(Unescape(text[start..], delimiter));
        return result;
    }

    private static string Join(IEnumerable<string> items, char delimiter)
    {
        return string.Join(delimiter, items.Select(i => Escape(i, delimiter)));
    }

    // Only '\' + delimiter and '\\' are escapes, any other '\x' passes through to the nested level
    private static string Escape(string text, char delimiter)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == delimiter || c == '\\' && (i + 1 == text.Length || text[i + 1] == '\\' || text[i + 1] == delimiter))
                sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static string Unescape(string text, char delimiter)
    {
        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length && (text[i + 1] == '\\' || text[i + 1] == delimiter))
                c = text[++i];
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static int IndexOfUnescaped(string text, char c, int start)
    {
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '\\')
                i++;
            else if (text[i] == c)
                return i;
        }
        return -1;
    }

    private static void RegisterIntegerTuple2<T>() where T : IBinaryInteger<T>
    {
        Register
        (
            (string s, out (T, T) value) =>
            {
                value = default;
                var separator = s.IndexOf(':');
                if (separator < 0 || !TryParseInteger(s[..separator], out T x) ||
                    !TryParseInteger(s[(separator + 1)..], out T y))
                    return false;
                value = (x, y);
                return true;
            },
            v => $"{v.Item1}:{v.Item2}"
        );
    }

    private static void RegisterVector4()
    {
        var component = Get<float>();
        Register
        (
            (string s, out Vector4 value) =>
            {
                value = default;
                var parts = s.Split(':');
                if (parts.Length != 4 || !component.TryParse(parts[0], out var x) || !component.TryParse(parts[1], out var y) ||
                    !component.TryParse(parts[2], out var z) || !component.TryParse(parts[3], out var w))
                    return false;
                value = new Vector4(x, y, z, w);
                return true;
            },
            v => $"{component.Format(v.X)}:{component.Format(v.Y)}:{component.Format(v.Z)}:{component.Format(v.W)}"
        );
    }
}
