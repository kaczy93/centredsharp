using System.Globalization;

namespace CentrED.Utils;

public static class UshortParser
{
    public static ushort Apply(string s)
    {
        if (s.StartsWith("0x"))
        {
            return ushort.Parse(s[2..], NumberStyles.HexNumber);
        }
        return ushort.Parse(s, NumberStyles.Integer);
    }
}