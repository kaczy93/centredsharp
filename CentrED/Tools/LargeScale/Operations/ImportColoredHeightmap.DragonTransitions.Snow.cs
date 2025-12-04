namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Snow biome transitions from DragonMod.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Add Snow->Dirt transitions based on DragonMod snow2dirt.txt
    /// </summary>
    private void AddSnowToDirtTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Snow, ToBiome = Biome.Dirt };

        // === EDGES ===
        // W edge: 0x387
        table.PatternToTiles["BAAAAABB"] = [0x0387];
        table.PatternToTiles["BAAAAAAB"] = [0x0387];
        table.PatternToTiles["AAAAAABB"] = [0x0387];
        table.PatternToTiles["AAAAAAAB"] = [0x0387];
        table.PatternToTiles["BAAAAABA"] = [0x0387];

        // E edge: 0x386
        table.PatternToTiles["AABBBAAA"] = [0x0386];
        table.PatternToTiles["AAABBAAA"] = [0x0386];
        table.PatternToTiles["AABBAAAA"] = [0x0386];
        table.PatternToTiles["AAABAAAA"] = [0x0386];
        table.PatternToTiles["AABABAAA"] = [0x0386];

        // S edge: 0x385
        table.PatternToTiles["AAAABBBA"] = [0x0385];
        table.PatternToTiles["AAAAABBA"] = [0x0385];
        table.PatternToTiles["AAAABBAA"] = [0x0385];
        table.PatternToTiles["AAAAABAA"] = [0x0385];
        table.PatternToTiles["AAAABABA"] = [0x0385];

        // N edge: 0x388
        table.PatternToTiles["BBBAAAAA"] = [0x0388];
        table.PatternToTiles["ABBAAAAA"] = [0x0388];
        table.PatternToTiles["BBAAAAAA"] = [0x0388];
        table.PatternToTiles["ABAAAAAA"] = [0x0388];
        table.PatternToTiles["BABAAAAA"] = [0x0388];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x038C]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x038C];
        table.PatternToTiles["BAAAAAAA"] = [0x0389]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0389];
        table.PatternToTiles["AAAABAAA"] = [0x038A]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x038A];
        table.PatternToTiles["AABAAAAA"] = [0x038B]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x038B];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x038D]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x038D];
        table.PatternToTiles["AABBBBAA"] = [0x038D];
        table.PatternToTiles["AAABBBAA"] = [0x038D];

        table.PatternToTiles["BBBBBAAA"] = [0x0390]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0390];
        table.PatternToTiles["ABBBBAAA"] = [0x0390];
        table.PatternToTiles["ABBBAAAA"] = [0x0390];

        table.PatternToTiles["BBBAAABB"] = [0x038E]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x038E];
        table.PatternToTiles["BBBAAAAB"] = [0x038E];
        table.PatternToTiles["BBAAAAAB"] = [0x038E];

        table.PatternToTiles["BAAABBBB"] = [0x038F]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x038F];
        table.PatternToTiles["BAAAABBB"] = [0x038F];
        table.PatternToTiles["AAAAABBB"] = [0x038F];

        // Nacor fallback: 0x638
        table.PatternToTiles["ABBBBBBB"] = [0x0638];
        table.PatternToTiles["BABBBBBB"] = [0x0638];
        table.PatternToTiles["BBABBBBB"] = [0x0638];
        table.PatternToTiles["BBBABBBB"] = [0x0638];
        table.PatternToTiles["BBBBABBB"] = [0x0638];
        table.PatternToTiles["BBBBBABB"] = [0x0638];
        table.PatternToTiles["BBBBBBAB"] = [0x0638];
        table.PatternToTiles["BBBBBBBA"] = [0x0638];
        table.PatternToTiles["BBBBBBBB"] = [0x0638];

        _dragonTransitions![(Biome.Snow, Biome.Dirt)] = table;
    }

    /// <summary>
    /// Add Snow->Rock transitions based on DragonMod snow2mountain.txt
    /// </summary>
    private void AddSnowToRockTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Snow, ToBiome = Biome.Rock };

        // === EDGES ===
        // W edge: 0x115
        table.PatternToTiles["BAAAAABB"] = [0x0115];
        table.PatternToTiles["BAAAAAAB"] = [0x0115];
        table.PatternToTiles["AAAAAABB"] = [0x0115];
        table.PatternToTiles["AAAAAAAB"] = [0x0115];
        table.PatternToTiles["BAAAAABA"] = [0x0115];

        // E edge: 0x117
        table.PatternToTiles["AABBBAAA"] = [0x0117];
        table.PatternToTiles["AAABBAAA"] = [0x0117];
        table.PatternToTiles["AABBAAAA"] = [0x0117];
        table.PatternToTiles["AAABAAAA"] = [0x0117];
        table.PatternToTiles["AABABAAA"] = [0x0117];

        // S edge: 0x114
        table.PatternToTiles["AAAABBBA"] = [0x0114];
        table.PatternToTiles["AAAAABBA"] = [0x0114];
        table.PatternToTiles["AAAABBAA"] = [0x0114];
        table.PatternToTiles["AAAAABAA"] = [0x0114];
        table.PatternToTiles["AAAABABA"] = [0x0114];

        // N edge: 0x116
        table.PatternToTiles["BBBAAAAA"] = [0x0116];
        table.PatternToTiles["ABBAAAAA"] = [0x0116];
        table.PatternToTiles["BBAAAAAA"] = [0x0116];
        table.PatternToTiles["ABAAAAAA"] = [0x0116];
        table.PatternToTiles["BABAAAAA"] = [0x0116];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x010D]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x010D];
        table.PatternToTiles["BAAAAAAA"] = [0x010E]; // NW
        table.PatternToTiles["AAABABBA"] = [0x010E];
        table.PatternToTiles["AAAABAAA"] = [0x010C]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x010C];
        table.PatternToTiles["AABAAAAA"] = [0x010F]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x010F];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x0112]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x0112];
        table.PatternToTiles["AABBBBAA"] = [0x0112];
        table.PatternToTiles["AAABBBAA"] = [0x0112];

        table.PatternToTiles["BBBBBAAA"] = [0x0111]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0111];
        table.PatternToTiles["ABBBBAAA"] = [0x0111];
        table.PatternToTiles["ABBBAAAA"] = [0x0111];

        table.PatternToTiles["BBBAAABB"] = [0x0110]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0110];
        table.PatternToTiles["BBBAAAAB"] = [0x0110];
        table.PatternToTiles["BBAAAAAB"] = [0x0110];

        table.PatternToTiles["BAAABBBB"] = [0x0113]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0113];
        table.PatternToTiles["BAAAABBB"] = [0x0113];
        table.PatternToTiles["AAAAABBB"] = [0x0113];

        _dragonTransitions![(Biome.Snow, Biome.Rock)] = table;
    }

    /// <summary>
    /// Add Snow->Water transitions based on DragonMod snow2water.txt
    /// </summary>
    private void AddSnowToWaterTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Snow, ToBiome = Biome.Water };

        // === EDGES ===
        // W edge: 0x30F, 0x313
        table.PatternToTiles["BAAAAABB"] = [0x030F, 0x0313];
        table.PatternToTiles["BAAAAAAB"] = [0x030F, 0x0313];
        table.PatternToTiles["AAAAAABB"] = [0x030F, 0x0313];
        table.PatternToTiles["AAAAAAAB"] = [0x030F, 0x0313];
        table.PatternToTiles["BAAAAABA"] = [0x030F, 0x0313];

        // E edge: 0x310, 0x314
        table.PatternToTiles["AABBBAAA"] = [0x0310, 0x0314];
        table.PatternToTiles["AAABBAAA"] = [0x0310, 0x0314];
        table.PatternToTiles["AABBAAAA"] = [0x0310, 0x0314];
        table.PatternToTiles["AAABAAAA"] = [0x0310, 0x0314];
        table.PatternToTiles["AABABAAA"] = [0x0310, 0x0314];

        // S edge: 0x312
        table.PatternToTiles["AAAABBBA"] = [0x0312];
        table.PatternToTiles["AAAAABBA"] = [0x0312];
        table.PatternToTiles["AAAABBAA"] = [0x0312];
        table.PatternToTiles["AAAAABAA"] = [0x0312];
        table.PatternToTiles["AAAABABA"] = [0x0312];

        // N edge: 0x311, 0x315
        table.PatternToTiles["BBBAAAAA"] = [0x0311, 0x0315];
        table.PatternToTiles["ABBAAAAA"] = [0x0315, 0x0311];
        table.PatternToTiles["BBAAAAAA"] = [0x0311, 0x0315];
        table.PatternToTiles["ABAAAAAA"] = [0x0311, 0x0315];
        table.PatternToTiles["BABAAAAA"] = [0x0311, 0x0315];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x030A, 0x0306]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x030A, 0x0306];
        table.PatternToTiles["BAAAAAAA"] = [0x0308, 0x030C]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0308, 0x030C];
        table.PatternToTiles["AAAABAAA"] = [0x0307, 0x030B]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x0307, 0x030B];
        table.PatternToTiles["AABAAAAA"] = [0x0309, 0x030D]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0309, 0x030D];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x02ED]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x02ED];
        table.PatternToTiles["AABBBBAA"] = [0x02ED];
        table.PatternToTiles["AAABBBAA"] = [0x02ED];

        table.PatternToTiles["BBBBBAAA"] = [0x008D]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x008D];
        table.PatternToTiles["ABBBBAAA"] = [0x008D];
        table.PatternToTiles["ABBBAAAA"] = [0x008D];

        table.PatternToTiles["BBBAAABB"] = [0x0095]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0095];
        table.PatternToTiles["BBBAAAAB"] = [0x0095];
        table.PatternToTiles["BBAAAAAB"] = [0x0095];

        table.PatternToTiles["BAAABBBB"] = [0x0091]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0091];
        table.PatternToTiles["BAAAABBB"] = [0x0091];
        table.PatternToTiles["AAAAABBB"] = [0x0091];

        // Nacor fallback: 0xAA
        table.PatternToTiles["ABBBBBBB"] = [0x00AA];
        table.PatternToTiles["BABBBBBB"] = [0x00AA];
        table.PatternToTiles["BBABBBBB"] = [0x00AA];
        table.PatternToTiles["BBBABBBB"] = [0x00AA];
        table.PatternToTiles["BBBBABBB"] = [0x00AA];
        table.PatternToTiles["BBBBBABB"] = [0x00AA];
        table.PatternToTiles["BBBBBBAB"] = [0x00AA];
        table.PatternToTiles["BBBBBBBA"] = [0x00AA];
        table.PatternToTiles["BBBBBBBB"] = [0x00AA];

        _dragonTransitions![(Biome.Snow, Biome.Water)] = table;
    }
}
