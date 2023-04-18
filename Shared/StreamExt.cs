using System.Text;

namespace Shared; 

public static class StreamExt {
    public static string ReadStringNull(this BinaryReader reader) {
        List<byte> bytes = new List<byte>();
        while (true) {
            var currentVal = reader.ReadByte();
            if (currentVal == '\0') {
                break;
            }
            bytes.Add(currentVal);
        }

        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    public static void WriteStringNull(this BinaryWriter writer, string value) {
        writer.Write(Encoding.ASCII.GetBytes(value + char.MinValue));
    }

    public static int CopyBytesTo(this Stream source, Stream target, int count) {
        var buffer = new byte[count];
        var result = source.Read(buffer);
        target.Write(buffer);
        return result;
    }

    public static Stream Dequeue(this Stream source, int count) {
        var result = new MemoryStream();
        var sourcePosition = source.Position;
        source.Position = 0;
        source.CopyBytesTo(result, count);
        result.Position = sourcePosition; //Since we dequeue, we also want to keep the position as it was

        var tempStream = new MemoryStream();
        source.CopyTo(tempStream);
        source.Position = 0;
        tempStream.Position = 0;
        source.SetLength(0); //Reset stream
        tempStream.CopyTo(source);
        source.Position = 0;
        return result;
    }
}