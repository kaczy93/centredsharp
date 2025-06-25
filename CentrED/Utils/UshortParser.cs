using System.Globalization;

namespace CentrED.Utils;

public static class UshortParser
{
    public static ushort Apply(this string s)
    {
        if (s.StartsWith("0x"))
        {
            return ushort.Parse(s.Substring(2), System.Globalization.NumberStyles.HexNumber);
        }
        return ushort.Parse(s, NumberStyles.Integer);
    }
}