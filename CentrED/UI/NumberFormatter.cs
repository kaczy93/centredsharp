namespace CentrED.UI;

public enum NumberDisplayFormat
{
    Hexadecimal,    // 0x0A3F
    Decimal,        // 2623
    HexWithDec,     // 0x0A3F (2623)
    DecWithHex      // 2623 (0x0A3F)
}

public static class NumberFormatter
{
    public static string FormatId(this int value)
    {
        return Format(value, Config.Instance.NumberFormat);
    }
    
    public static string FormatId(this uint value)
    {
        return Format((int)value, Config.Instance.NumberFormat);
    }
        
    public static string FormatId(this ushort value)
    {
        return Format(value, Config.Instance.NumberFormat);
    }
    
    public static string Format(int value, NumberDisplayFormat format)
    {
        return format switch
        {
            NumberDisplayFormat.Hexadecimal => $"0x{value:X4}",
            NumberDisplayFormat.Decimal => $"{value}",
            NumberDisplayFormat.HexWithDec => $"0x{value:X4} ({value})",
            NumberDisplayFormat.DecWithHex => $"{value} (0x{value:X4})",
            _ => $"0x{value:X4}"
        };
    }
}
