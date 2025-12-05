namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Partial class containing biome classification logic.
/// </summary>
public partial class ImportColoredHeightmap
{
    private enum Biome
    {
        // Natural terrain
        Water, Sand, Grass, Dirt, Jungle, Forest, Swamp, Rock, Snow, Lava, Cave, Void,
        // Stone/dungeon floors (new)
        SandstoneFloor, MarbleBrown, MarbleBlue, Cobblestone, StoneGray, Brick, MarbleWhite, MarbleGreen, Acid,
        // Border tiles for grass/forest coastlines - ALL are land tiles on WATER PIXELS
        // All directions place tiles on water pixels adjacent to land:
        GrassBorderN, GrassBorderS, GrassBorderE, GrassBorderW, GrassBorderNW, GrassBorderSE,
        ForestBorderN, ForestBorderS, ForestBorderE, ForestBorderW, ForestBorderNW, ForestBorderSE,
        // Corner tiles (0x0095) - SW, NE, ES directions:
        CornerTileSW, CornerTileNE, CornerTileES,
        Unknown
    }

    #region Border Tile Detection

    /// <summary>
    /// Border tile color patterns from UOMapMorph:
    /// ALL border tiles use R=150, B=95, G varies by direction and biome
    /// All tiles are placed on WATER PIXELS adjacent to land.
    /// </summary>
    private const int BorderTileR = 150;
    private const int BorderTileB = 95;

    /// <summary>
    /// Check if color is a border tile (placed on water pixel).
    /// ALL border tiles use color scheme: R=150, B=95, G varies.
    /// - Grass tiles: G=140(N), 142(S), 144(E), 130(W), 150(NW), 160(SE)
    /// - Forest tiles: G=141(N), 143(S), 145(E), 131(W), 151(NW), 161(SE)
    /// - Corner tiles (0x0095): G=100(SW), 110(NE), 120(ES)
    /// </summary>
    private static Biome? CheckBorderTileColor(byte r, byte g, byte b)
    {
        if (Math.Abs(r - BorderTileR) > 5 || Math.Abs(b - BorderTileB) > 5)
            return null;

        return g switch
        {
            // Corner tiles (0x0095) - same for grass and forest
            100 => Biome.CornerTileSW,    // G=100
            110 => Biome.CornerTileNE,    // G=110
            120 => Biome.CornerTileES,    // G=120
            // Forest tiles (exact G values) - must come before grass ranges
            131 => Biome.ForestBorderW,   // G=131
            151 => Biome.ForestBorderNW,  // G=151
            141 => Biome.ForestBorderN,   // G=141
            143 => Biome.ForestBorderS,   // G=143
            145 => Biome.ForestBorderE,   // G=145
            161 => Biome.ForestBorderSE,  // G=161
            // Grass tiles (exact G values)
            130 => Biome.GrassBorderW,    // G=130
            150 => Biome.GrassBorderNW,   // G=150
            140 => Biome.GrassBorderN,    // G=140
            142 => Biome.GrassBorderS,    // G=142
            144 => Biome.GrassBorderE,    // G=144
            160 => Biome.GrassBorderSE,   // G=160
            _ => null
        };
    }

    #endregion

    private (Biome biome, sbyte altitude) ClassifyBiomeColor(byte r, byte g, byte b)
    {
        // Check for void/black first (r,g,b all very low)
        if (r < 15 && g < 15 && b < 15)
        {
            return (Biome.Void, 0);
        }

        // Check for border tile colors FIRST (all placed on water pixels)
        // Border tiles use water Z level (-5) since they replace water tiles
        var borderTile = CheckBorderTileColor(r, g, b);
        if (borderTile.HasValue)
        {
            return (borderTile.Value, -5);
        }

        // Check for water - blue dominant
        if (b > 100 && b > r + 50 && b > g + 50)
        {
            return (Biome.Water, -5);
        }

        // Check for EXACT arena floor tile colors BEFORE grayscale check
        if (r == 105 && g == 105 && b == 105)
            return (Biome.Cobblestone, 0);
        if (r == 128 && g == 128 && b == 128)
            return (Biome.StoneGray, 0);
        if (r == 245 && g == 245 && b == 245)
            return (Biome.MarbleWhite, 0);
        if (r == 160 && g == 160 && b == 160)
            return (Biome.StoneGray, 0);

        // Pure black (0,0,0) is void
        if (r == 0 && g == 0 && b == 0)
            return (Biome.Void, 0);

        // Check for grayscale - snow/rock for world maps
        if (IsGrayscale(r, g, b))
        {
            var gray = (r + g + b) / 3;

            if (gray >= 200)
            {
                return (Biome.Snow, 0);
            }

            if (gray >= 64 && gray <= 127)
            {
                return (Biome.Rock, 0);
            }
        }

        // Match against biome colors
        var biome = MatchBiomeColor(r, g, b);
        sbyte landAltitude = (biome == Biome.Water) ? (sbyte)-5 : (sbyte)0;

        return (biome, landAltitude);
    }

    /// <summary>
    /// Classify biome from flat color only (for use with separate heightmap).
    /// </summary>
    private static Biome ClassifyBiomeColorOnly(byte r, byte g, byte b)
    {
        // Check for void/black first
        if (r < 15 && g < 15 && b < 15)
        {
            return Biome.Void;
        }

        // Check for border tile colors FIRST (all placed on water pixels)
        var borderTile = CheckBorderTileColor(r, g, b);
        if (borderTile.HasValue)
        {
            return borderTile.Value;
        }

        // Check for water FIRST - blue dominant
        if (b > 100 && b > r + 50 && b > g + 50)
        {
            return Biome.Water;
        }

        // Check for EXACT arena floor colors FIRST
        if (r == 105 && g == 105 && b == 105)
            return Biome.Cobblestone;
        if (r == 128 && g == 128 && b == 128)
            return Biome.StoneGray;
        if (r == 160 && g == 160 && b == 160)
            return Biome.StoneGray;
        if (r == 245 && g == 245 && b == 245)
            return Biome.MarbleWhite;

        // Check for grayscale colors (rock/snow for world maps)
        if (IsGrayscale(r, g, b))
        {
            var gray = (r + g + b) / 3;

            if (gray >= 200)
                return Biome.Snow;

            if (gray >= 64 && gray <= 127)
                return Biome.Rock;
        }

        // Use color distance matching against exact colors
        var biomeColors = new (Biome biome, int cr, int cg, int cb)[]
        {
            (Biome.Water, 0, 50, 180),
            (Biome.Forest, 80, 120, 50),
            (Biome.Grass, 60, 140, 60),
            (Biome.Swamp, 70, 90, 50),
            (Biome.Jungle, 120, 180, 40),
            (Biome.Dirt, 140, 100, 60),
            (Biome.Sand, 210, 180, 120),
            (Biome.Snow, 220, 220, 230),
            (Biome.Lava, 200, 60, 20),
            (Biome.Cave, 60, 50, 45),
            (Biome.Rock, 100, 100, 100),
            (Biome.SandstoneFloor, 194, 178, 128),
            (Biome.MarbleBrown, 139, 90, 43),
            (Biome.MarbleBlue, 70, 130, 180),
            (Biome.Cobblestone, 105, 105, 105),
            (Biome.StoneGray, 128, 128, 128),
            (Biome.StoneGray, 160, 160, 160),
            (Biome.Brick, 178, 34, 34),
            (Biome.MarbleWhite, 245, 245, 245),
            (Biome.MarbleGreen, 60, 179, 113),
            (Biome.Acid, 127, 255, 0),
        };

        Biome bestMatch = Biome.Grass;
        double bestDistance = double.MaxValue;

        foreach (var (biome, cr, cg, cb) in biomeColors)
        {
            var distance = Math.Sqrt(
                Math.Pow(r - cr, 2) +
                Math.Pow(g - cg, 2) +
                Math.Pow(b - cb, 2)
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = biome;
            }
        }

        return bestMatch;
    }

    private static bool IsGrayscale(byte r, byte g, byte b)
    {
        const int tolerance = 20;
        var maxDiff = Math.Max(Math.Max(Math.Abs(r - g), Math.Abs(g - b)), Math.Abs(r - b));
        var isGreenDominant = g > r + 20 && g > b + 20;
        var max = Math.Max(Math.Max(r, g), b);
        var min = Math.Min(Math.Min(r, g), b);
        var saturation = max > 0 ? (max - min) / (float)max : 0;

        return maxDiff <= tolerance && !isGreenDominant && saturation < 0.3f;
    }

    private static Biome MatchBiomeColor(byte r, byte g, byte b)
    {
        if (IsGrayscale(r, g, b))
        {
            var gray = (r + g + b) / 3;
            if (gray < 128)
                return Biome.Rock;
            else
                return Biome.Snow;
        }

        var biomeColors = new (Biome biome, int r, int g, int b)[]
        {
            (Biome.Water, 0, 50, 180),
            (Biome.Sand, 210, 180, 120),
            (Biome.Grass, 60, 140, 60),
            (Biome.Dirt, 140, 100, 60),
            (Biome.Jungle, 120, 180, 40),
            (Biome.Forest, 80, 120, 50),
            (Biome.Swamp, 70, 90, 50),
            (Biome.Lava, 200, 60, 20),
            (Biome.Cave, 60, 50, 45),
            (Biome.Rock, 100, 100, 100),
            (Biome.Snow, 220, 220, 230),
            (Biome.SandstoneFloor, 194, 178, 128),
            (Biome.MarbleBrown, 139, 90, 43),
            (Biome.MarbleBlue, 70, 130, 180),
            (Biome.Cobblestone, 105, 105, 105),
            (Biome.StoneGray, 128, 128, 128),
            (Biome.Brick, 178, 34, 34),
            (Biome.MarbleWhite, 245, 245, 245),
            (Biome.MarbleGreen, 60, 179, 113),
            (Biome.Acid, 127, 255, 0),
        };

        Biome bestMatch = Biome.Grass;
        double bestDistance = double.MaxValue;

        foreach (var (biome, br, bg, bb) in biomeColors)
        {
            var distance = Math.Sqrt(
                Math.Pow(r - br, 2) +
                Math.Pow(g - bg, 2) +
                Math.Pow(b - bb, 2)
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = biome;
            }
        }

        if (bestDistance > 100)
            return Biome.Unknown;

        return bestMatch;
    }

    private ushort GetLandTileForBiome(Biome biome)
    {
        var tiles = biome switch
        {
            Biome.Water => WaterTiles,
            Biome.Sand => SandTiles,
            Biome.Grass => GrassTiles,
            Biome.Dirt => DirtTiles,
            Biome.Jungle => JungleTiles,
            Biome.Forest => ForestTiles,
            Biome.Swamp => SwampTiles,
            Biome.Rock => RockTiles,
            Biome.Snow => SnowTiles,
            Biome.Lava => LavaTiles,
            Biome.Cave => CaveTiles,
            Biome.Void => VoidTiles,
            Biome.SandstoneFloor => SandstoneFloorTiles,
            Biome.MarbleBrown => MarbleBrownFloorTiles,
            Biome.MarbleBlue => MarbleBlueFloorTiles,
            Biome.Cobblestone => CobblestoneFloorTiles,
            Biome.StoneGray => StoneGrayFloorTiles,
            Biome.Brick => BrickFloorTiles,
            Biome.MarbleWhite => MarbleWhiteFloorTiles,
            Biome.MarbleGreen => MarbleGreenFloorTiles,
            Biome.Acid => AcidFloorTiles,
            // Grass border tiles - ALL placed on water pixels
            Biome.GrassBorderN => GrassBorderNTiles,
            Biome.GrassBorderS => GrassBorderSTiles,
            Biome.GrassBorderE => GrassBorderETiles,
            Biome.GrassBorderW => GrassBorderWTiles,
            Biome.GrassBorderNW => GrassBorderNWTiles,
            Biome.GrassBorderSE => GrassBorderSETiles,
            // Forest border tiles - ALL placed on water pixels
            Biome.ForestBorderN => ForestBorderNTiles,
            Biome.ForestBorderS => ForestBorderSTiles,
            Biome.ForestBorderE => ForestBorderETiles,
            Biome.ForestBorderW => ForestBorderWTiles,
            Biome.ForestBorderNW => ForestBorderNWTiles,
            Biome.ForestBorderSE => ForestBorderSETiles,
            // Corner tiles (0x0095) - placed on water pixels
            Biome.CornerTileSW => CornerTiles,
            Biome.CornerTileNE => CornerTiles,
            Biome.CornerTileES => CornerTiles,
            _ => GrassTiles
        };

        return tiles[_random.Next(tiles.Length)];
    }
}
