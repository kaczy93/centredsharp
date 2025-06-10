using System.Text;

namespace CentrED.Utility;

public static class StreamExt
{
    public static string ReadStringNull(this BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        while (true)
        {
            var currentVal = reader.ReadByte();
            if (currentVal == '\0')
            {
                break;
            }
            bytes.Add(currentVal);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    public static void WriteStringNull(this BinaryWriter writer, string value)
    {
        writer.Write(Encoding.ASCII.GetBytes(value + char.MinValue));
    }
    
    ///<Summary>
    ///Dequeues <c>count</c> bytes from <c>source.Position</c>. Drops data before current <c>source.Position</c>
    ///</Summary>
    public static byte[] Dequeue(this MemoryStream source, int offset, int count)
    {
        source.Position = 0;

        // Clamp parameters to the available data range
        if (offset < 0)
            offset = 0;
        if (offset > source.Length)
            offset = (int)source.Length;
        if (count < 0)
            count = 0;
        if (offset + count > source.Length)
            count = (int)source.Length - offset;

        byte[] result = new byte[count];
        Buffer.BlockCopy(source.GetBuffer(), offset, result, 0, count);

        int remainderLength = (int)source.Length - (offset + count);
        remainderLength = Math.Max(0, remainderLength);
        byte[] remainder = new byte[remainderLength];
        if (remainderLength > 0)
            Buffer.BlockCopy(source.GetBuffer(), offset + count, remainder, 0, remainderLength);

        source.SetLength(0);
        if (remainderLength > 0)
            source.Write(remainder, 0, remainderLength);
        source.Position = 0;
        return result;
    }
}