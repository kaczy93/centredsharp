using static CentrED.Network.LSO;

namespace CentrED.Client.Map;

public interface ILargeScaleOperation
{
    public void Write(BinaryWriter writer);
}

public class LSOCopyMove : ILargeScaleOperation
{
    private readonly CopyMove type;
    private readonly int offsetX;
    private readonly int offsetY;
    private readonly bool erase;

    public LSOCopyMove(CopyMove type, bool erase, int offsetX, int offsetY)
    {
        this.type = type;
        this.erase = erase;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
    }


    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)type);
        writer.Write(offsetX);
        writer.Write(offsetY);
        writer.Write(erase);
    }
}

public class LSOSetAltitude : ILargeScaleOperation
{
    private SetAltitude type;
    private short minZ;
    private short maxZ;
    private short relativeZ;

    public LSOSetAltitude(short minZ, short maxZ)
    {
        type = SetAltitude.Terrain;
        this.minZ = minZ;
        this.maxZ = maxZ;
    }

    public LSOSetAltitude(short relativeZ)
    {
        type = SetAltitude.Relative;
        this.relativeZ = relativeZ;
    }


    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)type);
        switch (type)
        {
            case SetAltitude.Terrain:
                writer.WriteZ(minZ);
                writer.WriteZ(maxZ);
                break;
            case SetAltitude.Relative:
                writer.WriteZ(relativeZ);
                break;
        }
    }
}

public class LSODrawLand : ILargeScaleOperation
{
    private ushort[] tileIds;

    public LSODrawLand(ushort[] tileIds)
    {
        this.tileIds = tileIds;
    }
    
    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)tileIds.Length);
        foreach (var tileId in tileIds)
        {
            writer.Write(tileId);
        }
    }
}

public class LSODeleteStatics : ILargeScaleOperation
{
    private ushort[] tileIds;
    private short minZ;
    private short maxZ;

    public LSODeleteStatics(ushort[] tileIds, short minZ, short maxZ)
    {
        this.tileIds = tileIds;
        this.minZ = minZ;
        this.maxZ = maxZ;
    }
    
    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)tileIds.Length);
        foreach (var tileId in tileIds)
        {
            writer.Write(tileId);
        }
        writer.WriteZ(minZ);
        writer.WriteZ(maxZ);
    }
}

public class LSOAddStatics : ILargeScaleOperation
{
    private ushort[] tileIds;
    private byte chance;
    private StaticsPlacement placement;
    private short fixedZ;

    public LSOAddStatics(ushort[] tileIds, byte chance, StaticsPlacement placement, short fixedZ)
    {
        this.tileIds = tileIds;
        this.chance = chance;
        this.placement = placement;
        this.fixedZ = fixedZ;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((ushort)tileIds.Length);
        foreach (var tileId in tileIds)
        {
            writer.Write(tileId);
        }
        writer.Write(chance);
        writer.Write((byte)placement);
        if (placement == StaticsPlacement.Fix)
        {
            writer.WriteZ(fixedZ);
        }
    }
}