using System.Buffers;
using static CentrED.Network.LSO;

namespace CentrED.Server.Map;

public abstract class LargeScaleOperation
{
    public virtual void Validate(ServerLandscape landscape){}
}

public class LsCopyMove : LargeScaleOperation
{
    public CopyMove Type;
    public int OffsetX;
    public int OffsetY;
    public bool Erase;
    
    // NEW: Alternate map source fields
    public bool UseAlternateSource;
    public string AlternateMapPath;
    public string AlternateStaIdxPath;
    public string AlternateStaticsPath;
    public ushort AlternateMapWidth;
    public ushort AlternateMapHeight;
    
    public LsCopyMove(ref SpanReader reader)
    {
        try
        {
            Type = (CopyMove)reader.ReadByte();
            OffsetX = reader.ReadInt32();
            OffsetY = reader.ReadInt32();
            Erase = reader.ReadBoolean();
            
            // NEW: Read alternate source flag and data
            UseAlternateSource = reader.ReadBoolean();
            
            // DEBUG LOG
            Console.WriteLine($"[LsCopyMove] UseAlternateSource: {UseAlternateSource}");
            
            if (UseAlternateSource)
            {
                AlternateMapPath = reader.ReadString();
                Console.WriteLine($"[LsCopyMove] Read AlternateMapPath: '{AlternateMapPath}'");
                
                AlternateStaIdxPath = reader.ReadString();
                Console.WriteLine($"[LsCopyMove] Read AlternateStaIdxPath: '{AlternateStaIdxPath}'");
                
                AlternateStaticsPath = reader.ReadString();
                Console.WriteLine($"[LsCopyMove] Read AlternateStaticsPath: '{AlternateStaticsPath}'");
                
                AlternateMapWidth = reader.ReadUInt16();
                Console.WriteLine($"[LsCopyMove] Read AlternateMapWidth: {AlternateMapWidth}");
                
                AlternateMapHeight = reader.ReadUInt16();
                Console.WriteLine($"[LsCopyMove] Read AlternateMapHeight: {AlternateMapHeight}");
                
                Console.WriteLine($"[LsCopyMove] All alternate source fields read successfully");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LsCopyMove] ERROR reading from SpanReader: {ex.Message}");
            Console.WriteLine($"[LsCopyMove] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}

public class LsSetAltitude : LargeScaleOperation
{
    public SetAltitude Type;
    public sbyte MinZ;
    public sbyte MaxZ;
    public sbyte RelativeZ;
    
    public LsSetAltitude(ref SpanReader reader)
    {
        Type = (SetAltitude)reader.ReadByte();
        switch (Type)
        {
            case SetAltitude.Terrain:
            {
                MinZ = reader.ReadSByte();
                MaxZ = reader.ReadSByte();
                break;
            }
            case SetAltitude.Relative:
            {
                RelativeZ = reader.ReadSByte();
                break;
            }
        }
    }
}

public class LsDrawTerrain : LargeScaleOperation
{
    public ushort[] TileIds;

    public LsDrawTerrain(ref SpanReader reader)
    {
        var count = reader.ReadUInt16();
        TileIds = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            TileIds[i] = reader.ReadUInt16();
        }
    }

    public override void Validate(ServerLandscape landscape)
    {
        foreach (var tileId in TileIds)
        {
            landscape.AssertLandTileId(tileId);
        }
    }
}

public class LsDeleteStatics : LargeScaleOperation
{
    
    public ushort[] TileIds;
    public sbyte MinZ;
    public sbyte MaxZ;
    
    public LsDeleteStatics(ref SpanReader reader)
    {
        var count = reader.ReadUInt16();
        TileIds = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            TileIds[i] = (ushort)(reader.ReadUInt16() - 0x4000);
        }
        MinZ = reader.ReadSByte();
        MaxZ = reader.ReadSByte();
    }
}

public class LsInsertStatics : LargeScaleOperation
{
    
    public ushort[] TileIds;
    public byte Probability;
    public StaticsPlacement PlacementType;
    public sbyte FixedZ;
    
    public LsInsertStatics(ref SpanReader reader)
    {
        var count = reader.ReadUInt16();
        TileIds = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            TileIds[i] = (ushort)(reader.ReadUInt16() - 0x4000);
        }
        Probability = reader.ReadByte();
        PlacementType = (StaticsPlacement)reader.ReadByte();
        if (PlacementType == StaticsPlacement.Fix)
        {
            FixedZ = reader.ReadSByte();
        }
    }

    public override void Validate(ServerLandscape landscape)
    {
        foreach (var tileId in TileIds)
        {
            landscape.AssertStaticTileId(tileId);
        }
    }
}