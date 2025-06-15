using System.Text;

namespace CentrED.Utility;

public static class StreamExt
{
    public static void WriteStringNull(this BinaryWriter writer, string value)
    {
        writer.Write(Encoding.ASCII.GetBytes(value + '\0'));
    }
}