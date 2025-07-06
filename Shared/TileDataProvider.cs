using System.Text;

namespace CentrED;

public class TileDataProvider
{
    public TileDataProvider(string tileDataPath)
    {
        using var file = File.Open(tileDataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(file, Encoding.UTF8);
        Version = file.Length >= 3188736 ? TileDataVersion.HighSeas : TileDataVersion.Legacy;
        file.Position = 0;
        for (var i = 0; i < 0x4000; i++)
        {
            //In High Seas, the first header comes AFTER the unknown tile (for whatever reason).
            //Therefore special handling is required.
            if ((Version == TileDataVersion.Legacy && i % 32 == 0) ||
                Version >= TileDataVersion.HighSeas && (i == 1 || (i > 1 && i % 32 == 0)))
            {
                reader.ReadUInt32(); //Group header
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
        StaticTiles = new TileDataStatic[staticCount];
        for (var i = 0; i < staticCount; i++)
        {
            if (i % 32 == 0)
            {
                file.Seek(4, SeekOrigin.Current); // skip header
            }
            StaticTiles[i] = ReadStaticTileData(reader);
        }
    }

    public TileDataVersion Version { get; }

    public TileDataLand[] LandTiles = new TileDataLand[0x4000];

    public TileDataStatic[] StaticTiles;

    private TileDataLand ReadLandTileData(BinaryReader reader)
    {
        var flags = Version switch
        {
            TileDataVersion.HighSeas => reader.ReadUInt64(),
            _ => reader.ReadUInt32()
        };
        var textureId = reader.ReadUInt16();
        var name = Encoding.ASCII.GetString(reader.ReadBytes(20)).Trim();
        return new TileDataLand(flags, textureId, name);
    }

    private TileDataStatic ReadStaticTileData(BinaryReader reader)
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
        return new TileDataStatic(flags, weight, layer, count, animId, hue, lightIndex, height, tileName);
    }
}