using System.Buffers;
using System.Runtime.CompilerServices;

namespace CentrED;

public class LandBlock
{
    public const int SIZE = 4 + 64 * LandTile.Size;

    public static LandBlock Empty(BaseLandscape landscape) => new(landscape)
    {
        _header = 0,
        Tiles = Enumerable.Repeat(LandTile.Empty, 64).ToArray()
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetTileIndex(ushort x, ushort y) => (byte)((y & 0x7) * 8 + (x & 0x7));

    public BaseLandscape Landscape { get; }
    public bool Changed { get; set; }
    public ushort X { get; }
    public ushort Y { get; }

    public LandTile[] Tiles { get; private init; } = new LandTile[64];

    public LandBlock(BaseLandscape landscape, ushort x = 0, ushort y = 0)
    {
        Landscape = landscape;
        X = x;
        Y = y;
        Changed = false;
        
    }
    
    public LandBlock(BaseLandscape landscape, ushort blockX, ushort blockY, BinaryReader reader) : this(landscape, blockX, blockY)
    {
        _header = reader.ReadInt32();
        for (ushort y = 0; y < 8; y++)
            for (ushort x = 0; x < 8; x++)
                Tiles[y * 8 + x] = new LandTile(reader,(ushort)(blockX * 8 + x), (ushort)(blockY * 8 + y), this);
    }

    public LandBlock(BaseLandscape landscape, ushort blockX, ushort blockY, SpanReader reader) : this(landscape, blockX, blockY)
    {
        _header = reader.ReadInt32();
        for (ushort y = 0; y < 8; y++){
            for (ushort x = 0; x < 8; x++)
            {
                Tiles[y * 8 + x] = new LandTile(this, reader.ReadUInt16(), (ushort)(blockX * 8 + x), (ushort)(blockY * 8 + y), reader.ReadSByte());
            }
        }
    }

    private int _header;

    public void OnChanged()
    {
        Changed = true;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(_header);
        foreach (var tile in Tiles)
            tile.Write(writer);
    }
}