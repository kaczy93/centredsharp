namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Partial class containing biome transition tile definitions and logic.
/// </summary>
public partial class ImportColoredHeightmap
{
    // Transition tiles between biomes
    // Format for each: [outer corners (4), edges (4), inner corners (4)]
    // Outer corners: NW, NE, SW, SE - where center tile has target biome in 2 adjacent cardinal directions
    // Edges: N, E, S, W - where center tile has target biome in 1 cardinal direction
    // Inner corners: NW, NE, SW, SE - where center tile has target biome only diagonally

    // Sand -> Grass transitions (from MapCreator: tiles 51-62 decimal = 0x0033-0x003E)
    private static readonly ushort[] SandToGrassTransitions = [
        0x0033, 0x0034, 0x0035, 0x0036,  // Outer corners: UL, UR, DL, DR
        0x0037, 0x0038, 0x0039, 0x003A,  // Edges: N, S, E, W
        0x003B, 0x003C, 0x003D, 0x003E   // Inner corners: UL, UR, DL, DR
    ];

    // Grass -> Sand transitions (from MapCreator: tiles 51-62 decimal = 0x0033-0x003E)
    private static readonly ushort[] GrassToSandTransitions = [
        0x0033, 0x0034, 0x0035, 0x0036,  // Outer corners: UL, UR, DL, DR
        0x0037, 0x0038, 0x0039, 0x003A,  // Edges: N, S, E, W
        0x003B, 0x003C, 0x003D, 0x003E   // Inner corners: UL, UR, DL, DR
    ];

    // Grass -> Dirt transitions (TilesBrush.xml: Grass Edge To Dirt)
    // Order matches Dragon/AAAABBBB convention: corners NW, NE, SW, SE; edges N, S, W, E.
    // UL/NW=0x007D, UR/NE=0x0082, DL/SW=0x0083, DR/SE=0x007E, LL/W=0x0089/0x008A, UU/N=0x008B/0x008C
    private static readonly ushort[] GrassToDirtTransitions = [
        0x007D, 0x0082, 0x0083, 0x007E,  // Corners: NW, NE, SW, SE
        0x008B, 0x008C, 0x0089, 0x008A,  // Edges: N, S, W, E
        0x007D, 0x0082, 0x0083, 0x007E   // Inner corners: NW, NE, SW, SE
    ];

    // Dirt -> Grass transitions (TilesBrush.xml: Dirt Edge To Grass)
    // Order matches Dragon/AAAABBBB convention: corners NW, NE, SW, SE; edges N, S, W, E.
    // UL/NW=0x0079, UR/NE=0x007B, DL/SW=0x007C, DR/SE=0x007A, LL/W=0x0087/0x0088, UU/N=0x0085/0x0086
    private static readonly ushort[] DirtToGrassTransitions = [
        0x0079, 0x007B, 0x007C, 0x007A,  // Corners: NW, NE, SW, SE
        0x0085, 0x0086, 0x0087, 0x0088,  // Edges: N, S, W, E
        0x0079, 0x007B, 0x007C, 0x007A   // Inner corners: NW, NE, SW, SE
    ];

    // Snow -> Dirt transitions (from TilesBrush.xml)
    // TilesBrush: UL=0x038A, UR=0x038C, DL=0x038B, DR=0x0389, UU=0x0385, LL=0x0386
    private static readonly ushort[] SnowToDirtTransitions = [
        0x038A, 0x038C, 0x038B, 0x0389,  // UL, UR, DL, DR corners
        0x0385, 0x0385, 0x0386, 0x0386,  // UU, UU, LL, LL edges
        0x038A, 0x038C, 0x038B, 0x0389   // UL, UR, DL, DR inner corners
    ];

    // Dirt -> Snow transitions (from TilesBrush.xml)
    // TilesBrush: UL=0x038A, UR=0x038C, DL=0x038B, DR=0x0389, UU=0x0385, LL=0x0386
    private static readonly ushort[] DirtToSnowTransitions = [
        0x038A, 0x038C, 0x038B, 0x0389,  // UL, UR, DL, DR corners
        0x0385, 0x0385, 0x0386, 0x0386,  // UU, UU, LL, LL edges
        0x038A, 0x038C, 0x038B, 0x0389   // UL, UR, DL, DR inner corners
    ];

    // Snow -> Grass transitions (from TilesBrush.xml)
    // TilesBrush: UL=0x05C8, UR=0x05CA, DL=0x05C9, DR=0x05C7, UU=0x05C2, LL=0x05C1
    private static readonly ushort[] SnowToGrassTransitions = [
        0x05C8, 0x05CA, 0x05C9, 0x05C7,  // UL, UR, DL, DR corners
        0x05C2, 0x05C2, 0x05C1, 0x05C1,  // UU, UU, LL, LL edges
        0x05C8, 0x05CA, 0x05C9, 0x05C7   // UL, UR, DL, DR inner corners
    ];

    // Grass -> Snow transitions (from TilesBrush.xml)
    // TilesBrush: UL=0x05C8, UR=0x05CA, DL=0x05C9, DR=0x05C7, UU=0x05C2, LL=0x05C1
    private static readonly ushort[] GrassToSnowTransitions = [
        0x05C8, 0x05CA, 0x05C9, 0x05C7,  // UL, UR, DL, DR corners
        0x05C2, 0x05C2, 0x05C1, 0x05C1,  // UU, UU, LL, LL edges
        0x05C8, 0x05CA, 0x05C9, 0x05C7   // UL, UR, DL, DR inner corners
    ];


    // Mountain/Rock transitions (from TilesBrush.xml)
    // Rock → Grass: UL=0x0235, UR=0x0236, DL=0x0238, DR=0x0237, UU=0x023B, LL=0x023A
    private static readonly ushort[] RockToGrassTransitions = [
        0x0235, 0x0236, 0x0238, 0x0237,  // UL, UR, DL, DR corners
        0x023B, 0x023B, 0x023A, 0x023A,  // UU, UU, LL, LL edges
        0x0235, 0x0236, 0x0238, 0x0237   // UL, UR, DL, DR inner corners
    ];

    // Grass -> Rock: UL=0x0239, UR=0x023B, DL=0x023A, DR=0x0238, UU=0x0241, LL=0x0240
    private static readonly ushort[] GrassToRockTransitions = [
        0x0239, 0x023B, 0x023A, 0x0238,  // UL, UR, DL, DR corners
        0x0241, 0x0241, 0x0240, 0x0240,  // UU, UU, LL, LL edges
        0x0239, 0x023B, 0x023A, 0x0238   // UL, UR, DL, DR inner corners
    ];

    // Rock → Dirt: UL=0x00E4, UR=0x00E5, DL=0x00E7, DR=0x00E6, UU=0x00DF, LL=0x00DE
    private static readonly ushort[] RockToDirtTransitions = [
        0x00E4, 0x00E5, 0x00E7, 0x00E6,  // UL, UR, DL, DR corners
        0x00DF, 0x00DF, 0x00DE, 0x00DE,  // UU, UU, LL, LL edges
        0x00E4, 0x00E5, 0x00E7, 0x00E6   // UL, UR, DL, DR inner corners
    ];

    // Dirt → Rock: UL=0x00E8, UR=0x00E9, DL=0x00EB, DR=0x00EA, UU=0x00F0, LL=0x00EF
    private static readonly ushort[] DirtToRockTransitions = [
        0x00E8, 0x00E9, 0x00EB, 0x00EA,  // UL, UR, DL, DR corners
        0x00F0, 0x00F0, 0x00EF, 0x00EF,  // UU, UU, LL, LL edges
        0x00E8, 0x00E9, 0x00EB, 0x00EA   // UL, UR, DL, DR inner corners
    ];

    // Rock → Snow: UL=0x0110, UR=0x0111, DL=0x0113, DR=0x0112, UU=0x0116, LL=0x0115
    private static readonly ushort[] RockToSnowTransitions = [
        0x0110, 0x0111, 0x0113, 0x0112,  // UL, UR, DL, DR corners
        0x0116, 0x0116, 0x0115, 0x0115,  // UU, UU, LL, LL edges
        0x0110, 0x0111, 0x0113, 0x0112   // UL, UR, DL, DR inner corners
    ];

    // Snow → Rock: UL=0x0114, UR=0x0115, DL=0x0117, DR=0x0116, UU=0x011B, LL=0x011A
    private static readonly ushort[] SnowToRockTransitions = [
        0x0114, 0x0115, 0x0117, 0x0116,  // UL, UR, DL, DR corners
        0x011B, 0x011B, 0x011A, 0x011A,  // UU, UU, LL, LL edges
        0x0114, 0x0115, 0x0117, 0x0116   // UL, UR, DL, DR inner corners
    ];

    // Rock → Sand: UL=0x0122, UR=0x0123, DL=0x0125, DR=0x0124, UU=0x0129, LL=0x0128
    private static readonly ushort[] RockToSandTransitions = [
        0x0122, 0x0123, 0x0125, 0x0124,  // UL, UR, DL, DR corners
        0x0129, 0x0129, 0x0128, 0x0128,  // UU, UU, LL, LL edges
        0x0122, 0x0123, 0x0125, 0x0124   // UL, UR, DL, DR inner corners
    ];

    // Sand → Rock: UL=0x0126, UR=0x0127, DL=0x0129, DR=0x0128, UU=0x012D, LL=0x012C
    private static readonly ushort[] SandToRockTransitions = [
        0x0126, 0x0127, 0x0129, 0x0128,  // UL, UR, DL, DR corners
        0x012D, 0x012D, 0x012C, 0x012C,  // UU, UU, LL, LL edges
        0x0126, 0x0127, 0x0129, 0x0128   // UL, UR, DL, DR inner corners
    ];

    // Rock → Forest: UL=0x00F4, UR=0x00F5, DL=0x00F7, DR=0x00F6, UU=0x00EC, LL=0x00EF
    private static readonly ushort[] RockToForestTransitions = [
        0x00F4, 0x00F5, 0x00F7, 0x00F6,  // UL, UR, DL, DR corners
        0x00EC, 0x00EC, 0x00EF, 0x00EF,  // UU, UU, LL, LL edges
        0x00F4, 0x00F5, 0x00F7, 0x00F6   // UL, UR, DL, DR inner corners
    ];

    // Forest → Rock: UL=0x00F8, UR=0x00F9, DL=0x00FB, DR=0x00FA, UU=0x0100, LL=0x00FF
    private static readonly ushort[] ForestToRockTransitions = [
        0x00F8, 0x00F9, 0x00FB, 0x00FA,  // UL, UR, DL, DR corners
        0x0100, 0x0100, 0x00FF, 0x00FF,  // UU, UU, LL, LL edges
        0x00F8, 0x00F9, 0x00FB, 0x00FA   // UL, UR, DL, DR inner corners
    ];

    // Grass → Cobblestone (from TilesBrush.xml: Grass Edge To Cobblestone)
    // DR=0x0681, DL=0x0683, UL=0x0682, UR=0x0684, LL=0x067E, UU=0x067D
    private static readonly ushort[] GrassToCobblestoneTransitions = [
        0x0682, 0x0684, 0x0683, 0x0681,  // UL, UR, DL, DR outer corners
        0x067D, 0x067D, 0x067E, 0x067E,  // UU, UU, LL, LL edges
        0x0682, 0x0684, 0x0683, 0x0681   // UL, UR, DL, DR inner corners
    ];



    // Cobblestone → Grass (from TilesBrush.xml: Cobblestone Edge To Grass)
    // DR=0x0685, DL=0x0687, UL=0x0686, UR=0x0688, LL=0x067F, UU=0x0680
    private static readonly ushort[] CobblestoneToGrassTransitions = [
        0x0686, 0x0688, 0x0687, 0x0685,  // UL, UR, DL, DR outer corners
        0x0680, 0x0680, 0x067F, 0x067F,  // UU, UU, LL, LL edges
        0x0686, 0x0688, 0x0687, 0x0685   // UL, UR, DL, DR inner corners
    ];

    // Dirt → Cobblestone (from TilesBrush.xml: Dirt Edge To Large Cobblestones 0198)
    // DR=0x0403, DL=0x0404, UL=0x0405, UR=0x0402, LL=0x0400, UU=0x0401
    private static readonly ushort[] DirtToCobblestoneTransitions = [
        0x0405, 0x0402, 0x0404, 0x0403,  // UL, UR, DL, DR corners
        0x0401, 0x0401, 0x0400, 0x0400,  // UU, UU, LL, LL edges
        0x0405, 0x0402, 0x0404, 0x0403   // UL, UR, DL, DR inner corners
    ];

    // Cobblestone → Dirt (from TilesBrush.xml: Large Cobblestones 0198 Edge To Dirt)
    // DR=0x0406, DL=0x0407, UL=0x0408, UR=0x0409, LL=0x040B, UU=0x040A
    private static readonly ushort[] CobblestoneToDirtTransitions = [
        0x0408, 0x0409, 0x0407, 0x0406,  // UL, UR, DL, DR corners
        0x040A, 0x040A, 0x040B, 0x040B,  // UU, UU, LL, LL edges
        0x0408, 0x0409, 0x0407, 0x0406   // UL, UR, DL, DR inner corners
    ];

    /// <summary>
    /// Calculate transition tile for a border tile (called during pre-processing).
    /// </summary>
    private ushort CalculateTransitionTile(int px, int py, Biome centerBiome, int width, int height)
    {
        if (_biomeCache == null)
            return 0;

        Biome GetBiomeAt(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return centerBiome;
            return _biomeCache[x, y];
        }

        // Get neighbors - using standard UO coordinate system (matches LandBrush Direction.Offset)
        // North = (0, -1), South = (0, +1), East = (+1, 0), West = (-1, 0)
        var n = GetBiomeAt(px, py - 1);      // North
        var s = GetBiomeAt(px, py + 1);      // South
        var e = GetBiomeAt(px + 1, py);      // East
        var w = GetBiomeAt(px - 1, py);      // West
        var ne = GetBiomeAt(px + 1, py - 1); // NorthEast
        var se = GetBiomeAt(px + 1, py + 1); // SouthEast
        var sw = GetBiomeAt(px - 1, py + 1); // SouthWest
        var nw = GetBiomeAt(px - 1, py - 1); // NorthWest

        // Try each transition type
        var result = TryGetTransition(centerBiome, Biome.Sand, Biome.Grass, SandToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Sand, GrassToSandTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Dirt, GrassToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Grass, DirtToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Cobblestone, GrassToCobblestoneTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Cobblestone, Biome.Grass, CobblestoneToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Cobblestone, DirtToCobblestoneTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Cobblestone, Biome.Dirt, CobblestoneToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Dirt, SnowToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Snow, DirtToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Grass, SnowToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Snow, GrassToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Grass, RockToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Rock, GrassToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Dirt, RockToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Rock, DirtToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Snow, RockToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Rock, SnowToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Sand, RockToSandTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Sand, Biome.Rock, SandToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Forest, RockToForestTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Forest, Biome.Rock, ForestToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        // No transition: if TilesBrush land tiles are available and enabled, use one of them.
        if (_useTilesBrush && _tilesBrushes != null)
        {
            var brushId = GetBrushIdForBiome(centerBiome);
            if (_tilesBrushes.TryGetValue(brushId, out var brush) && brush.LandTiles.Count > 0)
            {
                var totalChance = brush.LandTiles.Sum(t => t.Chance);
                var roll = (float)(_random.NextDouble() * totalChance);
                float cumulative = 0;
                foreach (var (tileId, chance) in brush.LandTiles)
                {
                    cumulative += chance;
                    if (roll <= cumulative)
                        return tileId;
                }
                return brush.LandTiles[0].TileId;
            }
        }

        return GetLandTileForBiome(centerBiome);
    }

    /// <summary>
    /// Calculate transition tile only for owned neighbor biomes.
    /// This ensures unidirectional transitions - only the tile that "owns" the border gets transition tiles.
    /// </summary>
    private ushort CalculateTransitionTileForOwned(int px, int py, Biome centerBiome, HashSet<Biome> ownedNeighbors, int width, int height)
    {
        if (_biomeCache == null)
            return 0;

        Biome GetBiomeAt(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return centerBiome;
            return _biomeCache[x, y];
        }

        // Get neighbors - using standard UO coordinate system (matches LandBrush Direction.Offset)
        // North = (0, -1), South = (0, +1), East = (+1, 0), West = (-1, 0)
        var n = GetBiomeAt(px, py - 1);      // North
        var s = GetBiomeAt(px, py + 1);      // South
        var e = GetBiomeAt(px + 1, py);      // East
        var w = GetBiomeAt(px - 1, py);      // West
        var ne = GetBiomeAt(px + 1, py - 1); // NorthEast
        var se = GetBiomeAt(px + 1, py + 1); // SouthEast
        var sw = GetBiomeAt(px - 1, py + 1); // SouthWest
        var nw = GetBiomeAt(px - 1, py - 1); // NorthWest

        // Mask neighbors - only consider biomes we "own"
        n = ownedNeighbors.Contains(n) ? n : centerBiome;
        s = ownedNeighbors.Contains(s) ? s : centerBiome;
        e = ownedNeighbors.Contains(e) ? e : centerBiome;
        w = ownedNeighbors.Contains(w) ? w : centerBiome;
        ne = ownedNeighbors.Contains(ne) ? ne : centerBiome;
        se = ownedNeighbors.Contains(se) ? se : centerBiome;
        sw = ownedNeighbors.Contains(sw) ? sw : centerBiome;
        nw = ownedNeighbors.Contains(nw) ? nw : centerBiome;

        // Try each transition type
        var result = TryGetTransition(centerBiome, Biome.Sand, Biome.Grass, SandToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Sand, GrassToSandTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Dirt, GrassToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Grass, DirtToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Cobblestone, GrassToCobblestoneTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Cobblestone, Biome.Grass, CobblestoneToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Cobblestone, DirtToCobblestoneTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Cobblestone, Biome.Dirt, CobblestoneToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Dirt, SnowToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Snow, DirtToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Grass, SnowToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Snow, GrassToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Grass, RockToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Rock, GrassToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Dirt, RockToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Rock, DirtToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Snow, RockToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Rock, SnowToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Sand, RockToSandTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Sand, Biome.Rock, SandToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Forest, RockToForestTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Forest, Biome.Rock, ForestToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        return 0;  // No transition
    }

    /// <summary>
    /// Gets transition tile based on neighboring biomes.
    /// Analyzes the 3x3 neighborhood to determine if this is an edge tile.
    /// </summary>
    private ushort GetTransitionTile(int px, int py, Biome centerBiome)
    {
        if (_biomeCache == null)
            return GetLandTileForBiome(centerBiome);

        // Water, Lava, Void don't use TilesBrush - return directly
        if (centerBiome == Biome.Water || centerBiome == Biome.Lava || centerBiome == Biome.Void)
        {
            return GetLandTileForBiome(centerBiome);
        }

        // Try TilesBrush transitions first if enabled and loaded
        if (_useTilesBrush && _tilesBrushes != null)
        {
            var brushResult = GetTilesBrushTransition(px, py, centerBiome);
            if (brushResult.HasValue)
                return brushResult.Value;
            // If no TilesBrush transition matched, fall through to hardcoded transitions below.
        }

        int width = _biomeCache.GetLength(0);
        int height = _biomeCache.GetLength(1);

        // Get neighboring biomes (N, E, S, W, NE, SE, SW, NW)
        Biome GetBiomeAt(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return centerBiome;
            return _biomeCache[x, y];
        }

        // Standard UO coordinate system (matches LandBrush Direction.Offset)
        var n = GetBiomeAt(px, py - 1);      // North
        var s = GetBiomeAt(px, py + 1);      // South
        var e = GetBiomeAt(px + 1, py);      // East
        var w = GetBiomeAt(px - 1, py);      // West
        var ne = GetBiomeAt(px + 1, py - 1); // NorthEast
        var se = GetBiomeAt(px + 1, py + 1); // SouthEast
        var sw = GetBiomeAt(px - 1, py + 1); // SouthWest
        var nw = GetBiomeAt(px - 1, py - 1); // NorthWest

        // Try each transition (fallback hardcoded transitions)
        var result = TryGetTransition(centerBiome, Biome.Sand, Biome.Grass, SandToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Sand, GrassToSandTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Dirt, GrassToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Grass, DirtToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Dirt, SnowToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Snow, DirtToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Grass, SnowToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Snow, GrassToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        // Rock/Mountain transitions
        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Grass, RockToGrassTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Grass, Biome.Rock, GrassToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Dirt, RockToDirtTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Dirt, Biome.Rock, DirtToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Snow, RockToSnowTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Snow, Biome.Rock, SnowToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Sand, RockToSandTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Sand, Biome.Rock, SandToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Rock, Biome.Forest, RockToForestTransitions, null,
            n, s, e, w, ne, se, sw, nw);
        if (result.HasValue) return result.Value;

        result = TryGetTransition(centerBiome, Biome.Forest, Biome.Rock, ForestToRockTransitions, null,
            n, s, e, w, ne, se, sw, nw); ;
        if (result.HasValue) return result.Value;

        // No transition needed, return normal tile
        return GetLandTileForBiome(centerBiome);
    }

    /// <summary>
    /// Tries to find a transition tile for a biome pair.
    /// IMPORTANT: Only biomeA (the "top" biome) gets transition tiles!
    /// biomeB (the "bottom" biome) stays as normal tile.
    /// </summary>
    private ushort? TryGetTransition(
        Biome centerBiome, Biome biomeA, Biome biomeB,
        ushort[] transitions, ushort[]? unused,
        Biome n, Biome s, Biome e, Biome w,
        Biome ne, Biome se, Biome sw, Biome nw)
    {
        // ONLY biomeA gets transition tiles when adjacent to biomeB
        if (centerBiome != biomeA)
        {
            return null;
        }

        Biome targetBiome = biomeB;

        // Note: In the code, n/s/e/w are named based on inverted isometric coords:
        // n = py+1 (actually South in standard coords)
        // s = py-1 (actually North in standard coords)
        // e = px-1 (actually West in standard coords)
        // w = px+1 (actually East in standard coords)
        bool hasTargetN = n == targetBiome;
        bool hasTargetS = s == targetBiome;
        bool hasTargetE = e == targetBiome;
        bool hasTargetW = w == targetBiome;

        // Using same logic as GetTilesBrushTransition (which matches TilesBrush.xml)
        // Array: [0]=UL, [1]=UR, [2]=DL, [3]=DR, [4-5]=UU, [6-7]=LL, [8-11]=inner corners

        // OUTER corners - two adjacent cardinal neighbors (same as TilesBrush.xml)
        if (hasTargetN && hasTargetW && !hasTargetS && !hasTargetE)
            return transitions[0]; // N+W → UL
        if (hasTargetN && hasTargetE && !hasTargetS && !hasTargetW)
            return transitions[1]; // N+E → UR
        if (hasTargetS && hasTargetW && !hasTargetN && !hasTargetE)
            return transitions[2]; // S+W → DL
        if (hasTargetS && hasTargetE && !hasTargetN && !hasTargetW)
            return transitions[3]; // S+E → DR

        // Edge transitions - single cardinal neighbor (same as TilesBrush.xml)
        if (hasTargetN && !hasTargetS && !hasTargetE && !hasTargetW)
            return transitions[4]; // N → UU
        if (hasTargetS && !hasTargetN && !hasTargetE && !hasTargetW)
            return transitions[5]; // S → UU
        if (hasTargetW && !hasTargetN && !hasTargetS && !hasTargetE)
            return transitions[6]; // W → LL
        if (hasTargetE && !hasTargetN && !hasTargetS && !hasTargetW)
            return transitions[7]; // E → LL

        // INNER corners - diagonal neighbor only (no adjacent cardinals)
        if (nw == targetBiome && !hasTargetN && !hasTargetW)
            return transitions[8];  // NW → UL inner
        if (ne == targetBiome && !hasTargetN && !hasTargetE)
            return transitions[9];  // NE → UR inner
        if (sw == targetBiome && !hasTargetS && !hasTargetW)
            return transitions[10]; // SW → DL inner
        if (se == targetBiome && !hasTargetS && !hasTargetE)
            return transitions[11]; // SE → DR inner

        return null;
    }
}
