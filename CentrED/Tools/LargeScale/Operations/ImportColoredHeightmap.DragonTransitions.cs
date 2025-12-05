namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Partial class implementing DragonMod-style AAAABBBB transition system.
///
/// The AAAABBBB system works by examining the 8 neighbors around a center tile
/// in clockwise order starting from NW: [NW, N, NE, E, SE, S, SW, W]
///
/// Each position is marked as:
/// - A = same biome as center (the tile being processed)
/// - B = target biome (the neighboring biome)
///
/// IMPORTANT: This is UNIDIRECTIONAL. For each biome pair (e.g., Grass-Dirt),
/// we only define ONE transition direction based on which biome "owns" the border.
/// The Dragon system processes the CENTER tile and looks at its neighbors.
///
/// From grass2dirt.txt in DragonMod:
/// - TileA = Grass (center tile)
/// - TileB = Dirt (neighbor)
/// - Pattern describes WHERE the Dirt neighbors are
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Structure to hold Dragon-style transition definitions for a biome pair.
    /// </summary>
    private class DragonTransitionTable
    {
        public Biome FromBiome { get; init; }
        public Biome ToBiome { get; init; }
        public Dictionary<string, ushort[]> PatternToTiles { get; } = new();
    }

    // Dragon-style transition tables
    private Dictionary<(Biome from, Biome to), DragonTransitionTable>? _dragonTransitions;

    /// <summary>
    /// Initialize Dragon-style transition tables.
    /// Based on DragonMod transition file format.
    /// </summary>
    private void InitializeDragonTransitions()
    {
        _dragonTransitions = new Dictionary<(Biome from, Biome to), DragonTransitionTable>();

        // Pattern positions: [0]=NW, [1]=N, [2]=NE, [3]=E, [4]=SE, [5]=S, [6]=SW, [7]=W
        // A = center biome (TileA), B = neighbor biome (TileB)

        // === GRASS transitions (Grass is center, draws transitions to neighbors) ===
        AddGrassToDirtTransitions();
        AddGrassToSandTransitions();
        AddGrassToForestTransitions();
        AddGrassToSnowTransitions();
        AddGrassToRockTransitions();
        AddGrassToCobblestoneTransitions();
        AddGrassToSwampTransitions();
        AddGrassToJungleTransitions();
        AddGrassToWaterTransitions();

        // === SNOW transitions (Snow is center) ===
        AddSnowToDirtTransitions();
        AddSnowToRockTransitions();
        AddSnowToWaterTransitions();

        // === SAND transitions (Sand is center) ===
        AddSandToDirtTransitions();
        AddSandToRockTransitions();
        AddSandToWaterTransitions();

        // === DIRT transitions (Dirt is center) ===
        AddDirtToCobblestoneTransitions();
        AddDirtToRockTransitions();
        AddDirtToWaterTransitions();

        // === FOREST transitions (Forest is center) ===
        AddForestToDirtTransitions();
        AddForestToSandTransitions();
        AddForestToWaterTransitions();

        // === JUNGLE transitions (Jungle is center) ===
        AddJungleToDirtTransitions();
        AddJungleToWaterTransitions();

        Console.WriteLine($"Dragon transitions initialized: {_dragonTransitions.Count} biome pairs");
    }

    /// <summary>
    /// Calculate transition tile using Dragon AAAABBBB pattern system.
    /// </summary>
    private ushort CalculateDragonTransition(int px, int py, Biome centerBiome, int width, int height)
    {
        if (_biomeCache == null || _dragonTransitions == null)
            return 0;

        Biome GetBiome(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return centerBiome;
            return _biomeCache[x, y];
        }

        // Get all 8 neighbors in Dragon order: NW, N, NE, E, SE, S, SW, W
        Biome[] neighbors =
        [
            GetBiome(px - 1, py - 1), // NW [0]
            GetBiome(px, py - 1),     // N  [1]
            GetBiome(px + 1, py - 1), // NE [2]
            GetBiome(px + 1, py),     // E  [3]
            GetBiome(px + 1, py + 1), // SE [4]
            GetBiome(px, py + 1),     // S  [5]
            GetBiome(px - 1, py + 1), // SW [6]
            GetBiome(px - 1, py)      // W  [7]
        ];

        // Find unique neighbor biomes that are different from center
        var differentBiomes = new HashSet<Biome>();
        foreach (var b in neighbors)
        {
            if (b != centerBiome && b != Biome.Void)
                differentBiomes.Add(b);
        }

        if (differentBiomes.Count == 0)
            return 0;

        // Try each different biome as the target
        foreach (var targetBiome in differentBiomes)
        {
            if (!_dragonTransitions.TryGetValue((centerBiome, targetBiome), out var table))
                continue;

            // Build the AAAABBBB pattern
            var pattern = BuildPattern(neighbors, targetBiome);

            // Look up exact pattern
            if (table.PatternToTiles.TryGetValue(pattern, out var tiles) && tiles.Length > 0)
                return tiles[_random.Next(tiles.Length)];
        }

        return 0;
    }

    /// <summary>
    /// Build 8-character pattern string from neighbors.
    /// </summary>
    private static string BuildPattern(Biome[] neighbors, Biome targetBiome)
    {
        return string.Create(8, (neighbors, targetBiome), (span, state) =>
        {
            for (int i = 0; i < 8; i++)
                span[i] = state.neighbors[i] == state.targetBiome ? 'B' : 'A';
        });
    }
}
