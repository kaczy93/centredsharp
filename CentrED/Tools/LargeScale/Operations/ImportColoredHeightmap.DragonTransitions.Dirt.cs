namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Dirt biome transitions from DragonMod.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Add Dirt->Cobblestone transitions based on DragonMod dirt2cobble.txt
    /// </summary>
    private void AddDirtToCobblestoneTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Dirt, ToBiome = Biome.Cobblestone };

        // === EDGES ===
        // W edge: 0x400
        table.PatternToTiles["BAAAAABB"] = [0x0400];
        table.PatternToTiles["BAAAAAAB"] = [0x0400];
        table.PatternToTiles["AAAAAABB"] = [0x0400];
        table.PatternToTiles["AAAAAAAB"] = [0x0400];
        table.PatternToTiles["BAAAAABA"] = [0x0400];

        // E edge: 0x3FE
        table.PatternToTiles["AABBBAAA"] = [0x03FE];
        table.PatternToTiles["AAABBAAA"] = [0x03FE];
        table.PatternToTiles["AABBAAAA"] = [0x03FE];
        table.PatternToTiles["AAABAAAA"] = [0x03FE];
        table.PatternToTiles["AABABAAA"] = [0x03FE];

        // S edge: 0x401
        table.PatternToTiles["AAAABBBA"] = [0x0401];
        table.PatternToTiles["AAAAABBA"] = [0x0401];
        table.PatternToTiles["AAAABBAA"] = [0x0401];
        table.PatternToTiles["AAAAABAA"] = [0x0401];
        table.PatternToTiles["AAAABABA"] = [0x0401];

        // N edge: 0x3FF
        table.PatternToTiles["BBBAAAAA"] = [0x03FF];
        table.PatternToTiles["ABBAAAAA"] = [0x03FF];
        table.PatternToTiles["BBAAAAAA"] = [0x03FF];
        table.PatternToTiles["ABAAAAAA"] = [0x03FF];
        table.PatternToTiles["BABAAAAA"] = [0x03FF];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x0402]; // SW
        table.PatternToTiles["BAAAAAAA"] = [0x0403]; // NW
        table.PatternToTiles["AAAABAAA"] = [0x0405]; // SE
        table.PatternToTiles["AABAAAAA"] = [0x0404]; // NE

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x03F9]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x03F9];
        table.PatternToTiles["AABBBBAA"] = [0x03F9];
        table.PatternToTiles["AAABBBAA"] = [0x03F9];

        table.PatternToTiles["BBBBBAAA"] = [0x03F8]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x03F8];
        table.PatternToTiles["ABBBBAAA"] = [0x03F8];
        table.PatternToTiles["ABBBAAAA"] = [0x03F8];

        table.PatternToTiles["BBBAAABB"] = [0x03F6]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x03F6];
        table.PatternToTiles["BBBAAAAB"] = [0x03F6];
        table.PatternToTiles["BBAAAAAB"] = [0x03F6];

        table.PatternToTiles["BAAABBBB"] = [0x03F7]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x03F7];
        table.PatternToTiles["BAAAABBB"] = [0x03F7];
        table.PatternToTiles["AAAAABBB"] = [0x03F7];

        _dragonTransitions![(Biome.Dirt, Biome.Cobblestone)] = table;
    }

    /// <summary>
    /// Add Dirt->Rock transitions based on DragonMod dirt2mountain.txt
    /// </summary>
    private void AddDirtToRockTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Dirt, ToBiome = Biome.Rock };

        // === EDGES ===
        // W edge: 0x6ED, 0x6F1
        table.PatternToTiles["BAAAAABB"] = [0x06ED, 0x06F1];
        table.PatternToTiles["BAAAAAAB"] = [0x06ED, 0x06F1];
        table.PatternToTiles["AAAAAABB"] = [0x06ED, 0x06F1];
        table.PatternToTiles["AAAAAAAB"] = [0x06ED, 0x06F1];
        table.PatternToTiles["BAAAAABA"] = [0x06ED, 0x06F1];

        // E edge: 0x6EC, 0x6F0
        table.PatternToTiles["AABBBAAA"] = [0x06EC, 0x06F0];
        table.PatternToTiles["AAABBAAA"] = [0x06EC, 0x06F0];
        table.PatternToTiles["AABBAAAA"] = [0x06EC, 0x06F0];
        table.PatternToTiles["AAABAAAA"] = [0x06EC, 0x06F0];
        table.PatternToTiles["AABABAAA"] = [0x06EC, 0x06F0];

        // S edge: 0x6EB, 0x6EF
        table.PatternToTiles["AAAABBBA"] = [0x06EB, 0x06EF];
        table.PatternToTiles["AAAAABBA"] = [0x06EB, 0x06EF];
        table.PatternToTiles["AAAABBAA"] = [0x06EB, 0x06EF];
        table.PatternToTiles["AAAAABAA"] = [0x06EB, 0x06EF];
        table.PatternToTiles["AAAABABA"] = [0x06EB, 0x06EF];

        // N edge: 0x6EE, 0x6F2
        table.PatternToTiles["BBBAAAAA"] = [0x06EE, 0x06F2];
        table.PatternToTiles["ABBAAAAA"] = [0x06EE, 0x06F2];
        table.PatternToTiles["BBAAAAAA"] = [0x06EE, 0x06F2];
        table.PatternToTiles["ABAAAAAA"] = [0x06EE, 0x06F2];
        table.PatternToTiles["BABAAAAA"] = [0x06EE, 0x06F2];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x00E2]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x00E2];
        table.PatternToTiles["BAAAAAAA"] = [0x00E3]; // NW
        table.PatternToTiles["AAABABBA"] = [0x00E3];
        table.PatternToTiles["AAAABAAA"] = [0x00E1]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x00E1];
        table.PatternToTiles["AABAAAAA"] = [0x00E0]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x00E0];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x00E6]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x00E6];
        table.PatternToTiles["AABBBBAA"] = [0x00E6];
        table.PatternToTiles["AAABBBAA"] = [0x00E6];

        table.PatternToTiles["BBBBBAAA"] = [0x00E5]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x00E5];
        table.PatternToTiles["ABBBBAAA"] = [0x00E5];
        table.PatternToTiles["ABBBAAAA"] = [0x00E5];

        table.PatternToTiles["BBBAAABB"] = [0x00E4]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x00E4];
        table.PatternToTiles["BBBAAAAB"] = [0x00E4];
        table.PatternToTiles["BBAAAAAB"] = [0x00E4];

        table.PatternToTiles["BAAABBBB"] = [0x00E7]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x00E7];
        table.PatternToTiles["BAAAABBB"] = [0x00E7];
        table.PatternToTiles["AAAAABBB"] = [0x00E7];

        _dragonTransitions![(Biome.Dirt, Biome.Rock)] = table;
    }

    /// <summary>
    /// Add Dirt->Water transitions based on DragonMod dirt2water.txt
    /// </summary>
    private void AddDirtToWaterTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Dirt, ToBiome = Biome.Water };

        // === EDGES ===
        // W edge: 0x8D
        table.PatternToTiles["BAAAAABB"] = [0x008D];
        table.PatternToTiles["BAAAAAAB"] = [0x008D];
        table.PatternToTiles["AAAAAABB"] = [0x008D];
        table.PatternToTiles["AAAAAAAB"] = [0x008D];
        table.PatternToTiles["BAAAAABA"] = [0x008D];

        // E edge: 0x8D
        table.PatternToTiles["AABBBAAA"] = [0x008D];
        table.PatternToTiles["AAABBAAA"] = [0x008D];
        table.PatternToTiles["AABBAAAA"] = [0x008D];
        table.PatternToTiles["AAABAAAA"] = [0x008D];
        table.PatternToTiles["AABABAAA"] = [0x008D];

        // S edge: 0x91
        table.PatternToTiles["AAAABBBA"] = [0x0091];
        table.PatternToTiles["AAAAABBA"] = [0x0091];
        table.PatternToTiles["AAAABBAA"] = [0x0091];
        table.PatternToTiles["AAAAABAA"] = [0x0091];
        table.PatternToTiles["AAAABABA"] = [0x0091];

        // N edge: 0x91
        table.PatternToTiles["BBBAAAAA"] = [0x0091];
        table.PatternToTiles["ABBAAAAA"] = [0x0091];
        table.PatternToTiles["BBAAAAAA"] = [0x0091];
        table.PatternToTiles["ABAAAAAA"] = [0x0091];
        table.PatternToTiles["BABAAAAA"] = [0x0091];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x0095]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x0095];
        table.PatternToTiles["BAAAAAAA"] = [0x0095]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0095];
        table.PatternToTiles["AAAABAAA"] = [0x0095]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x0095];
        table.PatternToTiles["AABAAAAA"] = [0x0091]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x008D];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x02ED]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x02ED];
        table.PatternToTiles["AABBBBAA"] = [0x02ED];
        table.PatternToTiles["AAABBBAA"] = [0x02ED];

        table.PatternToTiles["BBBBBAAA"] = [0x008D]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x008D];
        table.PatternToTiles["ABBBBAAA"] = [0x008D];
        table.PatternToTiles["ABBBAAAA"] = [0x008D];

        table.PatternToTiles["BAAABBBB"] = [0x0091]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0091];
        table.PatternToTiles["BAAAABBB"] = [0x0091];
        table.PatternToTiles["AAAAABBB"] = [0x0091];

        _dragonTransitions![(Biome.Dirt, Biome.Water)] = table;
    }
}
