using System.Text;
using ClassicUO.Assets;

namespace CentrED;

public class TileDataProvider : MulProvider
{
    public TileDataProvider(String tileDataPath, bool initOnly) : base(tileDataPath)
    {
        Version = Stream.Length >= 3188736 ? TileDataVersion.HighSeas : TileDataVersion.Legacy;
        Stream.Position = 0;
        for (var i = 0; i < 0x4000; i++)
        {
            //In High Seas, the first header comes AFTER the unknown tile (for whatever reason).
            //Therefore special handling is required.
            if ((Version == TileDataVersion.Legacy && i % 32 == 0) ||
                Version >= TileDataVersion.HighSeas && (i == 1 || (i > 1 && i % 32 == 0)))
            {
                Stream.Seek(4, SeekOrigin.Current);
            }

            LandTiles[i] = ReadLandTileData(Version, Reader);
        }

        var tsize = Version switch
        {
            TileDataVersion.HighSeas => 41,
            _ => 37
        };
        var xsize = 4 + 32 * tsize;

        var staticCount = (uint)((Stream.Length - Stream.Position) / xsize * 32);
        StaticTiles = new StaticTiles[staticCount];
        for (var i = 0; i < staticCount; i++)
        {
            if (i % 32 == 0)
            {
                Stream.Seek(4, SeekOrigin.Current); // skip header
            }
            StaticTiles[i] = ReadStaticTileData(Version, Reader);
        }

        if (initOnly)
        {
            Reader.Dispose();
            Stream.Dispose();
        }
    }

    public TileDataVersion Version { get; }

    public LandTiles[] LandTiles = new LandTiles[0x4000];

    public StaticTiles[] StaticTiles;

    private LandTiles ReadLandTileData(TileDataVersion version, BinaryReader reader)
    {
        var flags = Version switch
        {
            TileDataVersion.HighSeas => reader.ReadUInt64(),
            _ => reader.ReadUInt32()
        };
        var textureId = reader.ReadUInt16();
        var name = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
        return new LandTiles(flags, textureId, name);
    }

    private StaticTiles ReadStaticTileData(TileDataVersion version, BinaryReader reader)
    {
        var flags = Version switch
        {
            TileDataVersion.HighSeas => reader.ReadUInt64(),
            _ => reader.ReadUInt32()
        };
        var weight = reader.ReadByte();
        var layer = reader.ReadByte();
        var count = reader.ReadInt32();
        var animId = reader.ReadUInt16();
        var hue = reader.ReadUInt16();
        var lightIndex = reader.ReadUInt16();
        var height = reader.ReadByte();
        var tileName = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
        return new StaticTiles(flags, weight, layer, count, animId, hue, lightIndex, height, tileName);
    }
}