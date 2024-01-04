using System.Text;
using ClassicUO.Assets;

namespace CentrED;

public class TileDataProvider
{
    public TileDataProvider(String tileDataPath) 
    {
        var file = File.Open(tileDataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new BinaryReader(file, Encoding.UTF8);
        Version = file.Length >= 3188736 ? TileDataVersion.HighSeas : TileDataVersion.Legacy;
        file.Position = 0;
        for (var i = 0; i < 0x4000; i++)
        {
            //In High Seas, the first header comes AFTER the unknown tile (for whatever reason).
            //Therefore special handling is required.
            if ((Version == TileDataVersion.Legacy && i % 32 == 0) ||
                Version >= TileDataVersion.HighSeas && (i == 1 || (i > 1 && i % 32 == 0)))
            {
                file.Seek(4, SeekOrigin.Current);
            }

            LandTiles[i] = ReadLandTileData(reader);
        }

        var tsize = Version switch
        {
            TileDataVersion.HighSeas => 41,
            _ => 37
        };
        var xsize = 4 + 32 * tsize;

        var staticCount = (uint)((file.Length - file.Position) / xsize * 32);
        StaticTiles = new StaticTiles[staticCount];
        for (var i = 0; i < staticCount; i++)
        {
            if (i % 32 == 0)
            {
                file.Seek(4, SeekOrigin.Current); // skip header
            }
            StaticTiles[i] = ReadStaticTileData(reader);
        }
        reader.Dispose();
        file.Dispose();
    }

    public TileDataVersion Version { get; }

    public LandTiles[] LandTiles = new LandTiles[0x4000];

    public StaticTiles[] StaticTiles;

    private LandTiles ReadLandTileData(BinaryReader reader)
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

    private StaticTiles ReadStaticTileData(BinaryReader reader)
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