using System.Text;

namespace Cedserver; 

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
}