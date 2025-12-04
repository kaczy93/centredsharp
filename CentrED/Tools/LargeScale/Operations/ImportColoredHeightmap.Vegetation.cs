namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Partial class containing vegetation and structure classification logic.
/// </summary>
public partial class ImportColoredHeightmap
{
    #region Vegetation Classification

    private enum VegetationType
    {
        None, Tree, Bush, Grass, Flower, Crop, Coastline
    }

    /// <summary>
    /// Coastline direction for directional static placement.
    /// </summary>
    private enum CoastlineDirection
    {
        None = 0,
        W = 1,   // West coast
        S = 2,   // South coast
        N = 3,   // North coast
        E = 4,   // East coast
        SW = 5,  // Southwest corner
        SE = 6,  // Southeast corner
        NW = 7,  // Northwest corner
        NE = 8   // Northeast corner
    }

    /// <summary>
    /// Coastline statics by direction from CoastlineTool.
    /// </summary>
    private static readonly Dictionary<CoastlineDirection, ushort[]> CoastlineStaticsByDirection = new()
    {
        { CoastlineDirection.W,  [0x179D, 0x179E] },
        { CoastlineDirection.S,  [0x179F, 0x17A0] },
        { CoastlineDirection.N,  [0x17A1, 0x17A2] },
        { CoastlineDirection.E,  [0x17A3, 0x17A4] },
        { CoastlineDirection.SW, [0x17A5, 0x17A9] },
        { CoastlineDirection.SE, [0x17A6, 0x17AC] },
        { CoastlineDirection.NW, [0x17A7, 0x17AA] },
        { CoastlineDirection.NE, [0x17A8, 0x17AB] },
    };

    /// <summary>
    /// Coastline color pattern from UOMapMorph: R=200, G=100, B=direction.
    /// </summary>
    private const int CoastlineR = 200;
    private const int CoastlineG = 100;

    private static VegetationType ClassifyVegetationColor(byte r, byte g, byte b)
    {
        // Black = no vegetation
        if (r < 20 && g < 20 && b < 20)
            return VegetationType.None;

        // Check for coastline color pattern (R=200, G=100) first - takes priority
        if (Math.Abs(r - CoastlineR) <= 10 && Math.Abs(g - CoastlineG) <= 10)
            return VegetationType.Coastline;

        var vegColors = new (VegetationType type, int r, int g, int b)[]
        {
            (VegetationType.Tree, 34, 85, 34),
            (VegetationType.Bush, 50, 120, 50),
            (VegetationType.Grass, 80, 160, 80),
            (VegetationType.Flower, 200, 100, 150),
            (VegetationType.Crop, 180, 160, 60),
        };

        VegetationType bestMatch = VegetationType.None;
        double bestDistance = 50;

        foreach (var (type, vr, vg, vb) in vegColors)
        {
            var distance = Math.Sqrt(
                Math.Pow(r - vr, 2) +
                Math.Pow(g - vg, 2) +
                Math.Pow(b - vb, 2)
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = type;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Get coastline direction from Blue channel of coastline color.
    /// Color pattern: R=200, G=100, B=direction indicator.
    /// </summary>
    private static CoastlineDirection GetCoastlineDirection(byte b)
    {
        return b switch
        {
            >= 95 and <= 105 => CoastlineDirection.W,    // B=100
            >= 115 and <= 125 => CoastlineDirection.S,   // B=120
            >= 135 and <= 145 => CoastlineDirection.N,   // B=140
            >= 155 and <= 165 => CoastlineDirection.E,   // B=160
            >= 175 and <= 185 => CoastlineDirection.SW,  // B=180
            >= 186 and <= 195 => CoastlineDirection.SE,  // B=190
            >= 205 and <= 215 => CoastlineDirection.NW,  // B=210
            >= 216 and <= 225 => CoastlineDirection.NE,  // B=220
            _ => CoastlineDirection.W  // Default fallback
        };
    }

    /// <summary>
    /// Get coastline static for a specific direction.
    /// </summary>
    private ushort GetCoastlineStaticForDirection(CoastlineDirection direction)
    {
        if (direction == CoastlineDirection.None)
            return 0;

        if (CoastlineStaticsByDirection.TryGetValue(direction, out var statics))
            return statics[_random.Next(statics.Length)];

        // Fallback to W coast
        return CoastlineStaticsByDirection[CoastlineDirection.W][0];
    }

    private ushort GetStaticForVegetation(VegetationType type)
    {
        var statics = type switch
        {
            VegetationType.Tree => TreeStatics,
            VegetationType.Bush => BushStatics,
            VegetationType.Grass => GrassStatics,
            VegetationType.Flower => FlowerStatics,
            VegetationType.Crop => CropStatics,
            // Coastline is handled separately with direction
            _ => Array.Empty<ushort>()
        };

        if (statics.Length == 0)
            return 0;

        return statics[_random.Next(statics.Length)];
    }

    /// <summary>
    /// Check if vegetation type is coastline (requires special handling).
    /// </summary>
    private static bool IsCoastlineVegetation(VegetationType type)
    {
        return type == VegetationType.Coastline;
    }

    #endregion

    #region Structure Classification

    private enum StructureType
    {
        None, Wall, Pillar, Fence
    }

    private static StructureType ClassifyStructureColor(byte r, byte g, byte b)
    {
        // Black/near-black = no structure
        if (r < 20 && g < 20 && b < 20)
            return StructureType.None;

        // Grayscale check for walls/pillars
        int diff = Math.Max(Math.Max(Math.Abs(r - g), Math.Abs(g - b)), Math.Abs(r - b));
        if (diff < 30)
        {
            int gray = (r + g + b) / 3;
            if (gray >= 150)
                return StructureType.Pillar;
            else if (gray >= 100)
                return StructureType.Wall;
        }

        // Brown-ish = fence
        if (r > g && r > b && g > b)
            return StructureType.Fence;

        return StructureType.None;
    }

    private ushort GetStaticForStructure(StructureType type)
    {
        var statics = type switch
        {
            StructureType.Wall => WallStatics,
            StructureType.Pillar => PillarStatics,
            StructureType.Fence => FenceStatics,
            _ => Array.Empty<ushort>()
        };

        if (statics.Length == 0)
            return 0;

        return statics[_random.Next(statics.Length)];
    }

    #endregion
}
