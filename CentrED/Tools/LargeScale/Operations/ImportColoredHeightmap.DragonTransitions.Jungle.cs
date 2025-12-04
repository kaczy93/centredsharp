namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Jungle biome transitions from DragonMod.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Add Jungle->Dirt transitions based on DragonMod jungle2dirt.txt
    /// </summary>
    private void AddJungleToDirtTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Jungle, ToBiome = Biome.Dirt };

        // === EDGES ===
        // W edge: 0x599, 0x59D
        table.PatternToTiles["BAAAAABB"] = [0x0599, 0x059D];
        table.PatternToTiles["BAAAAAAB"] = [0x0599, 0x059D];
        table.PatternToTiles["AAAAAABB"] = [0x0599, 0x059D];
        table.PatternToTiles["AAAAAAAB"] = [0x0599, 0x059D];
        table.PatternToTiles["BAAAAABA"] = [0x0599, 0x059D];

        // E edge: 0x598, 0x59C
        table.PatternToTiles["AABBBAAA"] = [0x0598, 0x059C];
        table.PatternToTiles["AAABBAAA"] = [0x0598, 0x059C];
        table.PatternToTiles["AABBAAAA"] = [0x0598, 0x059C];
        table.PatternToTiles["AAABAAAA"] = [0x0598, 0x059C];
        table.PatternToTiles["AABABAAA"] = [0x0598, 0x059C];

        // S edge: 0x597, 0x59B
        table.PatternToTiles["AAAABBBA"] = [0x0597, 0x059B];
        table.PatternToTiles["AAAAABBA"] = [0x0597, 0x059C];
        table.PatternToTiles["AAAABBAA"] = [0x0597, 0x059C];
        table.PatternToTiles["AAAAABAA"] = [0x0597, 0x059C];
        table.PatternToTiles["AAAABABA"] = [0x0597, 0x059C];

        // N edge: 0x59A, 0x59E
        table.PatternToTiles["BBBAAAAA"] = [0x059A, 0x059E];
        table.PatternToTiles["ABBAAAAA"] = [0x059A, 0x059E];
        table.PatternToTiles["BBAAAAAA"] = [0x059A, 0x059E];
        table.PatternToTiles["ABAAAAAA"] = [0x059A, 0x059E];
        table.PatternToTiles["BABAAAAA"] = [0x059A, 0x059E];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x05A0]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x05A0];
        table.PatternToTiles["BAAAAAAA"] = [0x05A3, 0x05A4]; // NW
        table.PatternToTiles["AAABABBA"] = [0x05A3, 0x05A4];
        table.PatternToTiles["AAAABAAA"] = [0x05A5, 0x05A6]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x05A5, 0x05A6];
        table.PatternToTiles["AABAAAAA"] = [0x05A1, 0x059F]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x05A1, 0x059F];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x059B]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x059B];
        table.PatternToTiles["AABBBBAA"] = [0x059B];
        table.PatternToTiles["AAABBBAA"] = [0x059B];

        table.PatternToTiles["BBBBBAAA"] = [0x059A]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x059A];
        table.PatternToTiles["ABBBBAAA"] = [0x059A];
        table.PatternToTiles["ABBBAAAA"] = [0x059A];

        table.PatternToTiles["BBBAAABB"] = [0x059E]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x059E];
        table.PatternToTiles["BBBAAAAB"] = [0x059E];
        table.PatternToTiles["BBAAAAAB"] = [0x059E];

        table.PatternToTiles["BAAABBBB"] = [0x0599]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0599];
        table.PatternToTiles["BAAAABBB"] = [0x0599];
        table.PatternToTiles["AAAAABBB"] = [0x0599];

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

        _dragonTransitions![(Biome.Jungle, Biome.Dirt)] = table;
    }

    /// <summary>
    /// Add Jungle->Water transitions based on DragonMod jungle2water.txt
    /// </summary>
    private void AddJungleToWaterTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Jungle, ToBiome = Biome.Water };

        // === EDGES ===
        // W edge: 0x779, 0x780
        table.PatternToTiles["BAAAAABB"] = [0x0779, 0x0780];
        table.PatternToTiles["BAAAAAAB"] = [0x0779, 0x0780];
        table.PatternToTiles["AAAAAABB"] = [0x0779, 0x0780];
        table.PatternToTiles["AAAAAAAB"] = [0x0779, 0x0780];
        table.PatternToTiles["BAAAAABA"] = [0x0779, 0x0780];

        // E edge: 0x778, 0x785
        table.PatternToTiles["AABBBAAA"] = [0x0778, 0x0785];
        table.PatternToTiles["AAABBAAA"] = [0x0778, 0x0785];
        table.PatternToTiles["AABBAAAA"] = [0x0778, 0x0785];
        table.PatternToTiles["AAABAAAA"] = [0x0778, 0x0785];
        table.PatternToTiles["AABABAAA"] = [0x0778, 0x0785];

        // S edge: 0x77D, 0x784
        table.PatternToTiles["AAAABBBA"] = [0x077D, 0x0784];
        table.PatternToTiles["AAAAABBA"] = [0x077D, 0x0784];
        table.PatternToTiles["AAAABBAA"] = [0x077D, 0x0784];
        table.PatternToTiles["AAAAABAA"] = [0x077D, 0x0784];
        table.PatternToTiles["AAAABABA"] = [0x077D, 0x0784];

        // N edge: 0x77C, 0x783
        table.PatternToTiles["BBBAAAAA"] = [0x077C, 0x0783];
        table.PatternToTiles["ABBAAAAA"] = [0x077C, 0x0783];
        table.PatternToTiles["BBAAAAAA"] = [0x077C, 0x0783];
        table.PatternToTiles["ABAAAAAA"] = [0x077C, 0x0783];
        table.PatternToTiles["BABAAAAA"] = [0x077C, 0x0783];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x078C]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x078C];
        table.PatternToTiles["BAAAAAAA"] = [0x078A]; // NW
        table.PatternToTiles["AAABABBA"] = [0x078A];
        table.PatternToTiles["AAAABAAA"] = [0x078D]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x078D];
        table.PatternToTiles["AABAAAAA"] = [0x078B]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x078B];

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

        _dragonTransitions![(Biome.Jungle, Biome.Water)] = table;
    }
}
