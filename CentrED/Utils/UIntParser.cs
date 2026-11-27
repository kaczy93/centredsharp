using System.Globalization;

namespace CentrED.Utils;

public static class UIntParser
{
    public static uint Apply(string s)
    {
        if (s.StartsWith("0x"))
        {
            return uint.Parse(s[2..], NumberStyles.HexNumber);
        }
        return uint.Parse(s, NumberStyles.Integer);
    }
}