using Shared;

namespace ClientMock;

public static class Packets {
    public static void readPacket(BinaryReader reader, string packetName) {
        Console.WriteLine($"Reading {packetName}");
        Console.WriteLine($"packetId: {reader.ReadByte()}");
        var length = reader.ReadUInt32();
        Console.WriteLine($"length: {length}");
        var data = reader.ReadBytes((int)length - 5); // 1(packetid) +4(length)
        foreach (var b in data) {
            Console.Write($"{b} ");
        }

        Console.WriteLine();
    }

    public static byte[] LoginPacket() {
        Console.WriteLine("Writing LoginPacket");
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)0x02);
        writer.Write((uint)0);
        writer.Write((byte)0x03);
        writer.WriteStringNull("admin");
        writer.WriteStringNull("admin");
        stream.Seek(1, SeekOrigin.Begin);
        writer.Write((uint)stream.Length);
        stream.Seek(0, SeekOrigin.Begin);
        byte[] result = new byte[stream.Length];
        stream.Read(result);
        foreach (var b in result) {
            Console.Write($"{b} ");
        }

        Console.WriteLine();
        return result;
    }

    public static byte[] RadarHandlingPacket() {
        Console.WriteLine("Writing RadarHandlingPacket");
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)0x0D);
        writer.Write((byte)0x01);
        stream.Seek(0, SeekOrigin.Begin);
        byte[] result = new byte[stream.Length];
        stream.Read(result);
        foreach (var b in result) {
            Console.Write($"{b} ");
        }

        Console.WriteLine();
        return result;
    }

    public static byte[] RegionListPacket() {
        Console.WriteLine("Writing RegionListPacket");
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)0x03);
        writer.Write((uint)0);
        writer.Write((byte)0x0A);
        stream.Seek(1, SeekOrigin.Begin);
        writer.Write((uint)stream.Length);
        stream.Seek(0, SeekOrigin.Begin);
        byte[] result = new byte[stream.Length];
        stream.Read(result);
        foreach (var b in result) {
            Console.Write($"{b} ");
        }

        Console.WriteLine();
        return result;
    }

    public static byte[] RequestBlockPacket() {
        Console.WriteLine("Writing RequestBlockPacket");
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)0x04);
        writer.Write((uint)0);
        for (int x = 0; x < 7; x++) {
            for (int y = 0; y < 7; y++) {
                writer.Write((ushort)x);
                writer.Write((ushort)y);
            }
        }
        stream.Seek(1, SeekOrigin.Begin);
        writer.Write((uint)stream.Length);
        stream.Seek(0, SeekOrigin.Begin);
        byte[] result = new byte[stream.Length];
        stream.Read(result);
        foreach (var b in result) {
            Console.Write($"{b} ");
        }

        Console.WriteLine();
        return result;
    }
}