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
    private sbyte minZ;
    private sbyte maxZ;
    private sbyte relativeZ;

    public LSOSetAltitude(sbyte minZ, sbyte maxZ)
    {
        type = SetAltitude.Terrain;
        this.minZ = minZ;
        this.maxZ = maxZ;
    }

    public LSOSetAltitude(sbyte relativeZ)
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
                writer.Write(minZ);
                writer.Write(maxZ);
                break;
            case SetAltitude.Relative:
                writer.Write(relativeZ);
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
    private sbyte minZ;
    private sbyte maxZ;

    public LSODeleteStatics(string tileIds, sbyte minZ, sbyte maxZ)
    {
        if (tileIds.Trim().Length == 0)
        {
            this.tileIds = [];
        }
        else
        {
            this.tileIds = tileIds.Split(',').Select(s => (ushort)(int.Parse(s) + 0x4000)).ToArray();
        }
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
        writer.Write(minZ);
        writer.Write(maxZ);
    }
}

public class LSOAddStatics : ILargeScaleOperation
{
    private ushort[] tileIds;
    private byte chance;
    private StaticsPlacement placement;
    private sbyte fixedZ;

    public LSOAddStatics(ushort[] tileIds, byte chance, StaticsPlacement placement, sbyte fixedZ)
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
            writer.Write(fixedZ);
        }
    }
}