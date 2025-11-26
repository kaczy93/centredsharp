namespace CentrED.UI;

public enum NumberDisplayFormat
{
    HEX,    // 0x0A3F
    DEC,        // 2623
    HEX_DEC,     // 0x0A3F (2623)
    DEC_HEX      // 2623 (0x0A3F)
}

public static class NumberFormatter
{
    public static string FormatId(this int value)
    {
        return FormatId(value, Config.Instance.NumberFormat);
    }
    
    public static string FormatId(this uint value)
    {
        return FormatId((int)value, Config.Instance.NumberFormat);
    }
        
    public static string FormatId(this ushort value)
    {
        return FormatId(value, Config.Instance.NumberFormat);
    }

    public static string FormatId(this ushort value, NumberDisplayFormat format)
    {
        return FormatId((int)value, format);
    }
    
    public static string FormatId(this int value, NumberDisplayFormat format)
    {
        return format switch
        {
            NumberDisplayFormat.HEX => $"0x{value:X4}",
            NumberDisplayFormat.DEC => $"{value}",
            NumberDisplayFormat.HEX_DEC => $"0x{value:X4} ({value})",
            NumberDisplayFormat.DEC_HEX => $"{value} (0x{value:X4})",
            _ => $"0x{value:X4}"
        };
    }
}
