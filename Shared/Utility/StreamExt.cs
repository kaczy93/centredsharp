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

    public static int CopyBytesTo(this Stream source, Stream target, int count)
    {
        var buffer = new byte[count];
        var result = source.Read(buffer);
        target.Write(buffer);
        return result;
    }

    ///<Summary>
    ///Dequeues <c>count</c> bytes from <c>source.Position</c>. Drops data before current <c>source.Position</c>
    ///</Summary>
    public static byte[] Dequeue(this MemoryStream source, int offset, int count)
    {
        source.Position = 0;
        byte[] result = new byte[count];
        byte[] remainder = new byte[source.Length - (offset + count)];

        Buffer.BlockCopy(source.GetBuffer(), offset, result, 0, count);
        Buffer.BlockCopy(source.GetBuffer(), offset + count, remainder, 0, remainder.Length);

        source.SetLength(0);
        source.Write(remainder);
        source.Position = 0;
        return result;
    }
}