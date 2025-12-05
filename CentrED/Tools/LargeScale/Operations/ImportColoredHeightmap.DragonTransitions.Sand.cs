namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Sand biome transitions from DragonMod.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Add Sand->Dirt transitions based on DragonMod sand2dirt.txt
    /// </summary>
    private void AddSandToDirtTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Sand, ToBiome = Biome.Dirt };

        // === EDGES ===
        // W edge: 0x337
        table.PatternToTiles["BAAAAABB"] = [0x0337];
        table.PatternToTiles["BAAAAAAB"] = [0x0337];
        table.PatternToTiles["AAAAAABB"] = [0x0337];
        table.PatternToTiles["AAAAAAAB"] = [0x0337];
        table.PatternToTiles["BAAAAABA"] = [0x0337];

        // E edge: 0x336
        table.PatternToTiles["AABBBAAA"] = [0x0336];
        table.PatternToTiles["AAABBAAA"] = [0x0336];
        table.PatternToTiles["AABBAAAA"] = [0x0336];
        table.PatternToTiles["AAABAAAA"] = [0x0336];
        table.PatternToTiles["AABABAAA"] = [0x0336];

        // S edge: 0x335
        table.PatternToTiles["AAAABBBA"] = [0x0335];
        table.PatternToTiles["AAAAABBA"] = [0x0335];
        table.PatternToTiles["AAAABBAA"] = [0x0335];
        table.PatternToTiles["AAAAABAA"] = [0x0335];
        table.PatternToTiles["AAAABABA"] = [0x0335];

        // N edge: 0x338
        table.PatternToTiles["BBBAAAAA"] = [0x0338];
        table.PatternToTiles["ABBAAAAA"] = [0x0338];
        table.PatternToTiles["BBAAAAAA"] = [0x0338];
        table.PatternToTiles["ABAAAAAA"] = [0x0338];
        table.PatternToTiles["BABAAAAA"] = [0x0338];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x033C]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x033C];
        table.PatternToTiles["BAAAAAAA"] = [0x0339]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0339];
        table.PatternToTiles["AAAABAAA"] = [0x033A]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x033A];
        table.PatternToTiles["AABAAAAA"] = [0x033B]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x033B];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x033D]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x033D];
        table.PatternToTiles["AABBBBAA"] = [0x033D];
        table.PatternToTiles["AAABBBAA"] = [0x033D];

        table.PatternToTiles["BBBBBAAA"] = [0x0340]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0340];
        table.PatternToTiles["ABBBBAAA"] = [0x0340];
        table.PatternToTiles["ABBBAAAA"] = [0x0340];

        table.PatternToTiles["BBBAAABB"] = [0x033E]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x033E];
        table.PatternToTiles["BBBAAAAB"] = [0x033E];
        table.PatternToTiles["BBAAAAAB"] = [0x033E];

        table.PatternToTiles["BAAABBBB"] = [0x033F]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x033F];
        table.PatternToTiles["BAAAABBB"] = [0x033F];
        table.PatternToTiles["AAAAABBB"] = [0x033F];

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

        _dragonTransitions![(Biome.Sand, Biome.Dirt)] = table;
    }

    /// <summary>
    /// Add Sand->Rock transitions based on DragonMod sand2mountain.txt
    /// </summary>
    private void AddSandToRockTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Sand, ToBiome = Biome.Rock };

        // === EDGES ===
        // W edge: 0x128
        table.PatternToTiles["BAAAAABB"] = [0x0128];
        table.PatternToTiles["BAAAAAAB"] = [0x0128];
        table.PatternToTiles["AAAAAABB"] = [0x0128];
        table.PatternToTiles["AAAAAAAB"] = [0x0128];
        table.PatternToTiles["BAAAAABA"] = [0x0128];

        // E edge: 0x126
        table.PatternToTiles["AABBBAAA"] = [0x0126];
        table.PatternToTiles["AAABBAAA"] = [0x0126];
        table.PatternToTiles["AABBAAAA"] = [0x0126];
        table.PatternToTiles["AAABAAAA"] = [0x0126];
        table.PatternToTiles["AABABAAA"] = [0x0126];

        // S edge: 0x127
        table.PatternToTiles["AAAABBBA"] = [0x0127];
        table.PatternToTiles["AAAAABBA"] = [0x0127];
        table.PatternToTiles["AAAABBAA"] = [0x0127];
        table.PatternToTiles["AAAAABAA"] = [0x0127];
        table.PatternToTiles["AAAABABA"] = [0x0127];

        // N edge: 0x129
        table.PatternToTiles["BBBAAAAA"] = [0x0129];
        table.PatternToTiles["ABBAAAAA"] = [0x0129];
        table.PatternToTiles["BBAAAAAA"] = [0x0129];
        table.PatternToTiles["ABAAAAAA"] = [0x0129];
        table.PatternToTiles["BABAAAAA"] = [0x0129];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x07C0]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x07C0];
        table.PatternToTiles["BAAAAAAA"] = [0x07BD]; // NW
        table.PatternToTiles["AAABABBA"] = [0x07BD];
        table.PatternToTiles["AAAABAAA"] = [0x07BE]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x07BE];
        table.PatternToTiles["AABAAAAA"] = [0x012D]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x07BF];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x0124]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x0124];
        table.PatternToTiles["AABBBBAA"] = [0x0124];
        table.PatternToTiles["AAABBBAA"] = [0x0124];

        table.PatternToTiles["BBBBBAAA"] = [0x0123]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0123];
        table.PatternToTiles["ABBBBAAA"] = [0x0123];
        table.PatternToTiles["ABBBAAAA"] = [0x0123];

        table.PatternToTiles["BBBAAABB"] = [0x0122]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0122];
        table.PatternToTiles["BBBAAAAB"] = [0x0122];
        table.PatternToTiles["BBAAAAAB"] = [0x0122];

        table.PatternToTiles["BAAABBBB"] = [0x0125]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0125];
        table.PatternToTiles["BAAAABBB"] = [0x0125];
        table.PatternToTiles["AAAAABBB"] = [0x0125];

        _dragonTransitions![(Biome.Sand, Biome.Rock)] = table;
    }

    /// <summary>
    /// Add Sand->Water transitions based on DragonMod sand2water.txt
    /// </summary>
    private void AddSandToWaterTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Sand, ToBiome = Biome.Water };

        // === EDGES ===
        // W edge: 0x1C1, 0x1C5
        table.PatternToTiles["BAAAAABB"] = [0x01C1, 0x01C5];
        table.PatternToTiles["BAAAAAAB"] = [0x01C1, 0x01C5];
        table.PatternToTiles["AAAAAABB"] = [0x01C1, 0x01C5];
        table.PatternToTiles["AAAAAAAB"] = [0x01C1, 0x01C5];
        table.PatternToTiles["BAAAAABA"] = [0x01C1, 0x01C5];

        // E edge: 0x1C2, 0x1C6
        table.PatternToTiles["AABBBAAA"] = [0x01C2, 0x01C6];
        table.PatternToTiles["AAABBAAA"] = [0x01C2, 0x01C6];
        table.PatternToTiles["AABBAAAA"] = [0x01C2, 0x01C6];
        table.PatternToTiles["AAABAAAA"] = [0x01C2, 0x01C6];
        table.PatternToTiles["AABABAAA"] = [0x01C2, 0x01C6];

        // S edge: 0x1C4, 0x1C8
        table.PatternToTiles["AAAABBBA"] = [0x01C4, 0x01C8];
        table.PatternToTiles["AAAAABBA"] = [0x01C4, 0x01C8];
        table.PatternToTiles["AAAABBAA"] = [0x01C4, 0x01C8];
        table.PatternToTiles["AAAAABAA"] = [0x01C4, 0x01C8];
        table.PatternToTiles["AAAABABA"] = [0x01C4, 0x01C8];

        // N edge: 0x1C3, 0x1C7
        table.PatternToTiles["BBBAAAAA"] = [0x01C3, 0x01C7];
        table.PatternToTiles["ABBAAAAA"] = [0x01C3, 0x01C7];
        table.PatternToTiles["BBAAAAAA"] = [0x01C3, 0x01C7];
        table.PatternToTiles["ABAAAAAA"] = [0x01C3, 0x01C7];
        table.PatternToTiles["BABAAAAA"] = [0x01C3, 0x01C7];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x01BD]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x01BD];
        table.PatternToTiles["BAAAAAAA"] = [0x01BF]; // NW
        table.PatternToTiles["AAABABBA"] = [0x01BF];
        table.PatternToTiles["AAAABAAA"] = [0x01BC]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x01BC];
        table.PatternToTiles["AABAAAAA"] = [0x01BE]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x01BE];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x01BB]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x01BB];
        table.PatternToTiles["AABBBBAA"] = [0x01BB];
        table.PatternToTiles["AAABBBAA"] = [0x01BB];

        table.PatternToTiles["BBBBBAAA"] = [0x01B9]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x01B9];
        table.PatternToTiles["ABBBBAAA"] = [0x01B9];
        table.PatternToTiles["ABBBAAAA"] = [0x01B9];

        table.PatternToTiles["BBBAAABB"] = [0x01BB]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x01BB];
        table.PatternToTiles["BBBAAAAB"] = [0x01BB];
        table.PatternToTiles["BBAAAAAB"] = [0x01BB];

        table.PatternToTiles["BAAABBBB"] = [0x01BA]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x01BA];
        table.PatternToTiles["BAAAABBB"] = [0x01BA];
        table.PatternToTiles["AAAAABBB"] = [0x01BA];

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

        _dragonTransitions![(Biome.Sand, Biome.Water)] = table;
    }
}
