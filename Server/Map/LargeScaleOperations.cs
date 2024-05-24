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
    
    public LsCopyMove(BinaryReader reader)
    {
        Type = (CopyMove)reader.ReadByte();
        OffsetX = reader.ReadInt32();
        OffsetY = reader.ReadInt32();
        Erase = reader.ReadBoolean();
    }
}

public class LsSetAltitude : LargeScaleOperation
{
    public SetAltitude Type;
    public sbyte MinZ;
    public sbyte MaxZ;
    public sbyte RelativeZ;
    
    public LsSetAltitude(BinaryReader reader)
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

    public LsDrawTerrain(BinaryReader reader)
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
    
    public LsDeleteStatics(BinaryReader reader)
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
    
    public LsInsertStatics(BinaryReader reader)
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