namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Forest biome transitions from DragonMod.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Add Forest->Dirt transitions based on DragonMod forest2dirt.txt
    /// </summary>
    private void AddForestToDirtTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Forest, ToBiome = Biome.Dirt };

        // === EDGES ===
        // W edge: 0x166
        table.PatternToTiles["BAAAAABB"] = [0x0166];
        table.PatternToTiles["BAAAAAAB"] = [0x0166];
        table.PatternToTiles["AAAAAABB"] = [0x0166];
        table.PatternToTiles["AAAAAAAB"] = [0x0166];
        table.PatternToTiles["BAAAAABA"] = [0x0166];

        // E edge: 0x167
        table.PatternToTiles["AABBBAAA"] = [0x0167];
        table.PatternToTiles["AAABBAAA"] = [0x0167];
        table.PatternToTiles["AABBAAAA"] = [0x0167];
        table.PatternToTiles["AAABAAAA"] = [0x0167];
        table.PatternToTiles["AABABAAA"] = [0x0167];

        // S edge: 0x168
        table.PatternToTiles["AAAABBBA"] = [0x0168];
        table.PatternToTiles["AAAAABBA"] = [0x0168];
        table.PatternToTiles["AAAABBAA"] = [0x0168];
        table.PatternToTiles["AAAAABAA"] = [0x0168];
        table.PatternToTiles["AAAABABA"] = [0x0168];

        // N edge: 0x165
        table.PatternToTiles["BBBAAAAA"] = [0x0165];
        table.PatternToTiles["ABBAAAAA"] = [0x0165];
        table.PatternToTiles["BBAAAAAA"] = [0x0165];
        table.PatternToTiles["ABAAAAAA"] = [0x0165];
        table.PatternToTiles["BABAAAAA"] = [0x0165];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x0161]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x0161];
        table.PatternToTiles["BAAAAAAA"] = [0x0163]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0163];
        table.PatternToTiles["AAAABAAA"] = [0x0164]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x0164];
        table.PatternToTiles["AABAAAAA"] = [0x0162]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0162];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x016B]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x016B];
        table.PatternToTiles["AABBBBAA"] = [0x016B];
        table.PatternToTiles["AAABBBAA"] = [0x016B];

        table.PatternToTiles["BBBBBAAA"] = [0x0169]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0169];
        table.PatternToTiles["ABBBBAAA"] = [0x0169];
        table.PatternToTiles["ABBBAAAA"] = [0x0169];

        table.PatternToTiles["BBBAAABB"] = [0x016C]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x016C];
        table.PatternToTiles["BBBAAAAB"] = [0x016C];
        table.PatternToTiles["BBAAAAAB"] = [0x016C];

        table.PatternToTiles["BAAABBBB"] = [0x016A]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x016A];
        table.PatternToTiles["BAAAABBB"] = [0x016A];
        table.PatternToTiles["AAAAABBB"] = [0x016A];

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

        _dragonTransitions![(Biome.Forest, Biome.Dirt)] = table;
    }

    /// <summary>
    /// Add Forest->Sand transitions based on DragonMod forest2sand.txt
    /// </summary>
    private void AddForestToSandTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Forest, ToBiome = Biome.Sand };

        // === EDGES ===
        // W edge: 0x64C
        table.PatternToTiles["BAAAAABB"] = [0x064C];
        table.PatternToTiles["BAAAAAAB"] = [0x064C];
        table.PatternToTiles["AAAAAABB"] = [0x064C];
        table.PatternToTiles["AAAAAAAB"] = [0x064C];
        table.PatternToTiles["BAAAAABA"] = [0x064C];

        // E edge: 0x64D
        table.PatternToTiles["AABBBAAA"] = [0x064D];
        table.PatternToTiles["AAABBAAA"] = [0x064D];
        table.PatternToTiles["AABBAAAA"] = [0x064D];
        table.PatternToTiles["AAABAAAA"] = [0x064D];
        table.PatternToTiles["AABABAAA"] = [0x064D];

        // S edge: 0x64E
        table.PatternToTiles["AAAABBBA"] = [0x064E];
        table.PatternToTiles["AAAAABBA"] = [0x064E];
        table.PatternToTiles["AAAABBAA"] = [0x064E];
        table.PatternToTiles["AAAAABAA"] = [0x064E];
        table.PatternToTiles["AAAABABA"] = [0x064E];

        // N edge: 0x64B
        table.PatternToTiles["BBBAAAAA"] = [0x064B];
        table.PatternToTiles["ABBAAAAA"] = [0x064B];
        table.PatternToTiles["BBAAAAAA"] = [0x064B];
        table.PatternToTiles["ABAAAAAA"] = [0x064B];
        table.PatternToTiles["BABAAAAA"] = [0x064B];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x0656]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x0656];
        table.PatternToTiles["BAAAAAAA"] = [0x0653]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0653];
        table.PatternToTiles["AAAABAAA"] = [0x0654]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x0654];
        table.PatternToTiles["AABAAAAA"] = [0x0655]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0655];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x064F]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x064F];
        table.PatternToTiles["AABBBBAA"] = [0x064F];
        table.PatternToTiles["AAABBBAA"] = [0x064F];

        table.PatternToTiles["BBBBBAAA"] = [0x0652]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0652];
        table.PatternToTiles["ABBBBAAA"] = [0x0652];
        table.PatternToTiles["ABBBAAAA"] = [0x0652];

        table.PatternToTiles["BBBAAABB"] = [0x0650]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0650];
        table.PatternToTiles["BBBAAAAB"] = [0x0650];
        table.PatternToTiles["BBAAAAAB"] = [0x0650];

        table.PatternToTiles["BAAABBBB"] = [0x0651]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0651];
        table.PatternToTiles["BAAAABBB"] = [0x0651];
        table.PatternToTiles["AAAAABBB"] = [0x0651];

        // Nacor fallback: 0x671
        table.PatternToTiles["ABBBBBBB"] = [0x0671];
        table.PatternToTiles["BABBBBBB"] = [0x0671];
        table.PatternToTiles["BBABBBBB"] = [0x0671];
        table.PatternToTiles["BBBABBBB"] = [0x0671];
        table.PatternToTiles["BBBBABBB"] = [0x0671];
        table.PatternToTiles["BBBBBABB"] = [0x0671];
        table.PatternToTiles["BBBBBBAB"] = [0x0671];
        table.PatternToTiles["BBBBBBBA"] = [0x0671];
        table.PatternToTiles["BBBBBBBB"] = [0x0671];

        _dragonTransitions![(Biome.Forest, Biome.Sand)] = table;
    }

    /// <summary>
    /// Add Forest->Water transitions based on DragonMod forest2water.txt
    /// </summary>
    private void AddForestToWaterTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Forest, ToBiome = Biome.Water };

        // === EDGES ===
        // W edge: 0x2EE, 0x2E7
        table.PatternToTiles["BAAAAABB"] = [0x02EE, 0x02E7];
        table.PatternToTiles["BAAAAAAB"] = [0x02EE, 0x02E7];
        table.PatternToTiles["AAAAAABB"] = [0x02EE, 0x02E7];
        table.PatternToTiles["AAAAAAAB"] = [0x02EE, 0x02E7];
        table.PatternToTiles["BAAAAABA"] = [0x02EE, 0x02E7];

        // E edge: 0x2E6, 0x2F3
        table.PatternToTiles["AABBBAAA"] = [0x02E6, 0x02F3];
        table.PatternToTiles["AAABBAAA"] = [0x02E6, 0x02F3];
        table.PatternToTiles["AABBAAAA"] = [0x02E6, 0x02F3];
        table.PatternToTiles["AAABAAAA"] = [0x02E6, 0x02F3];
        table.PatternToTiles["AABABAAA"] = [0x02E6, 0x02F3];

        // S edge: 0x2F2, 0x2EB
        table.PatternToTiles["AAAABBBA"] = [0x02F2, 0x02EB];
        table.PatternToTiles["AAAAABBA"] = [0x02F2, 0x02EB];
        table.PatternToTiles["AAAABBAA"] = [0x02F2, 0x02EB];
        table.PatternToTiles["AAAAABAA"] = [0x02F2, 0x02EB];
        table.PatternToTiles["AAAABABA"] = [0x02F2, 0x02EB];

        // N edge: 0x2EA, 0x2F1
        table.PatternToTiles["BBBAAAAA"] = [0x02EA, 0x02F1];
        table.PatternToTiles["ABBAAAAA"] = [0x02EA, 0x02F1];
        table.PatternToTiles["BBAAAAAA"] = [0x02EA, 0x02F1];
        table.PatternToTiles["ABAAAAAA"] = [0x02EA, 0x02F1];
        table.PatternToTiles["BABAAAAA"] = [0x02EA, 0x02F1];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x02FA]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x02FA];
        table.PatternToTiles["BAAAAAAA"] = [0x00C6]; // NW
        table.PatternToTiles["AAABABBA"] = [0x02F8, 0x02EF];
        table.PatternToTiles["AAAABAAA"] = [0x02F0, 0x02FB]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x02F0, 0x02FB];
        table.PatternToTiles["AABAAAAA"] = [0x02F9]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x02F9];

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

        _dragonTransitions![(Biome.Forest, Biome.Water)] = table;
    }
}
