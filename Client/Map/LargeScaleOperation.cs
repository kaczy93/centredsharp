using CentrED.Utility;
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
    
    // NEW: Alternate map source fields
    private readonly bool useAlternateSource;
    private readonly string alternateMapPath;
    private readonly string alternateStaIdxPath;
    private readonly string alternateStaticsPath;
    private readonly int alternateMapWidth;
    private readonly int alternateMapHeight;

    // Original constructor (backwards compatible)
    public LSOCopyMove(CopyMove type, bool erase, int offsetX, int offsetY)
    {
        this.type = type;
        this.erase = erase;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.useAlternateSource = false;
        this.alternateMapPath = string.Empty;
        this.alternateStaIdxPath = string.Empty;
        this.alternateStaticsPath = string.Empty;
        this.alternateMapWidth = 0;
        this.alternateMapHeight = 0;
    }

    // NEW: Extended constructor with alternate map support
    public LSOCopyMove(
        CopyMove type, 
        bool erase, 
        int offsetX, 
        int offsetY,
        string alternateMapPath,
        string alternateStaIdxPath,
        string alternateStaticsPath,
        int alternateMapWidth,
        int alternateMapHeight)
    {
        this.type = type;
        this.erase = erase;
        this.offsetX = offsetX;
        this.offsetY = offsetY;
        this.useAlternateSource = true;
        this.alternateMapPath = alternateMapPath ?? string.Empty;
        this.alternateStaIdxPath = alternateStaIdxPath ?? string.Empty;
        this.alternateStaticsPath = alternateStaticsPath ?? string.Empty;
        this.alternateMapWidth = alternateMapWidth;
        this.alternateMapHeight = alternateMapHeight;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)type);
        writer.Write(offsetX);
        writer.Write(offsetY);
        writer.Write(erase);
        
        // NEW: Write alternate source flag and data
        writer.Write(useAlternateSource);
        
        if (useAlternateSource)
        {
            writer.WriteStringNull(alternateMapPath);
            writer.WriteStringNull(alternateStaIdxPath);
            writer.WriteStringNull(alternateStaticsPath);
            writer.Write((ushort)alternateMapWidth); 
            writer.Write((ushort)alternateMapHeight);
        }
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

    public LSODeleteStatics(ushort[] tileIds, sbyte minZ, sbyte maxZ)
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