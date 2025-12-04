namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Grass biome transitions from DragonMod.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Add Grass->Dirt transitions based on DragonMod grass2dirt.txt
    /// </summary>
    private void AddGrassToDirtTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Dirt };

        // === EDGES ===
        // W edge (Dirt to West): 0x87, 0x88
        table.PatternToTiles["BAAAAABB"] = [0x0087, 0x0088];
        table.PatternToTiles["BAAAAAAB"] = [0x0087, 0x0088];
        table.PatternToTiles["AAAAAABB"] = [0x0087, 0x0088];
        table.PatternToTiles["AAAAAAAB"] = [0x0087, 0x0088];
        table.PatternToTiles["BAAAAABA"] = [0x0087, 0x0088];

        // E edge (Dirt to East): 0x89, 0x8A
        table.PatternToTiles["AABBBAAA"] = [0x0089, 0x008A];
        table.PatternToTiles["AAABBAAA"] = [0x0089, 0x008A];
        table.PatternToTiles["AABBAAAA"] = [0x0089, 0x008A];
        table.PatternToTiles["AAABAAAA"] = [0x0089, 0x008A];
        table.PatternToTiles["AABABAAA"] = [0x0089, 0x008A];

        // S edge (Dirt to South): 0x8B, 0x8C
        table.PatternToTiles["AAAABBBA"] = [0x008B, 0x008C];
        table.PatternToTiles["AAAAABBA"] = [0x008B, 0x008C];
        table.PatternToTiles["AAAABBAA"] = [0x008B, 0x008C];
        table.PatternToTiles["AAAAABAA"] = [0x008B, 0x008C];
        table.PatternToTiles["AAAABABA"] = [0x008B, 0x008C];

        // N edge (Dirt to North): 0x85, 0x86
        table.PatternToTiles["BBBAAAAA"] = [0x0085, 0x0086];
        table.PatternToTiles["ABBAAAAA"] = [0x0085, 0x0086];
        table.PatternToTiles["BBAAAAAA"] = [0x0085, 0x0086];
        table.PatternToTiles["ABAAAAAA"] = [0x0085, 0x0086];
        table.PatternToTiles["BABAAAAA"] = [0x0085, 0x0086];

        // === INNER CORNERS (diagonal only) ===
        table.PatternToTiles["AAAAAABA"] = [0x0082]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x0082]; // SW alt
        table.PatternToTiles["BAAAAAAA"] = [0x007E]; // NW
        table.PatternToTiles["AAABABBA"] = [0x007E]; // NW alt
        table.PatternToTiles["AAAABAAA"] = [0x007D]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x007D]; // SE alt
        table.PatternToTiles["AABAAAAA"] = [0x0083]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0083]; // NE alt

        // === OUTER CORNERS ===
        // NW corner is TileA: 0x7A
        table.PatternToTiles["AABBBBBA"] = [0x007A];
        table.PatternToTiles["AAABBBBA"] = [0x007A];
        table.PatternToTiles["AABBBBAA"] = [0x007A];
        table.PatternToTiles["AAABBBAA"] = [0x007A];

        // SW corner is TileA: 0x7B
        table.PatternToTiles["BBBBBAAA"] = [0x007B];
        table.PatternToTiles["BBBBAAAA"] = [0x007B];
        table.PatternToTiles["ABBBBAAA"] = [0x007B];
        table.PatternToTiles["ABBBAAAA"] = [0x007B];

        // SE corner is TileA: 0x79
        table.PatternToTiles["BBBAAABB"] = [0x0079];
        table.PatternToTiles["BBAAAABB"] = [0x0079];
        table.PatternToTiles["BBBAAAAB"] = [0x0079];
        table.PatternToTiles["BBAAAAAB"] = [0x0079];

        // NE corner is TileA: 0x7C
        table.PatternToTiles["BAAABBBB"] = [0x007C];
        table.PatternToTiles["AAAABBBB"] = [0x007C];
        table.PatternToTiles["BAAAABBB"] = [0x007C];
        table.PatternToTiles["AAAAABBB"] = [0x007C];

        // Nacor fallback (mostly Dirt): 0x638
        table.PatternToTiles["ABBBBBBB"] = [0x0638];
        table.PatternToTiles["BABBBBBB"] = [0x0638];
        table.PatternToTiles["BBABBBBB"] = [0x0638];
        table.PatternToTiles["BBBABBBB"] = [0x0638];
        table.PatternToTiles["BBBBABBB"] = [0x0638];
        table.PatternToTiles["BBBBBABB"] = [0x0638];
        table.PatternToTiles["BBBBBBAB"] = [0x0638];
        table.PatternToTiles["BBBBBBBA"] = [0x0638];
        table.PatternToTiles["BBBBBBBB"] = [0x0638];

        _dragonTransitions![(Biome.Grass, Biome.Dirt)] = table;
    }

    /// <summary>
    /// Add Grass->Sand transitions based on DragonMod grass2sand.txt
    /// </summary>
    private void AddGrassToSandTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Sand };

        // === EDGES ===
        // W edge: 0x37
        table.PatternToTiles["BAAAAABB"] = [0x0037];
        table.PatternToTiles["BAAAAAAB"] = [0x0037];
        table.PatternToTiles["AAAAAABB"] = [0x0037];
        table.PatternToTiles["AAAAAAAB"] = [0x0037];
        table.PatternToTiles["BAAAAABA"] = [0x0037];

        // E edge: 0x38
        table.PatternToTiles["AABBBAAA"] = [0x0038];
        table.PatternToTiles["AAABBAAA"] = [0x0038];
        table.PatternToTiles["AABBAAAA"] = [0x0038];
        table.PatternToTiles["AAABAAAA"] = [0x0038];
        table.PatternToTiles["AABABAAA"] = [0x0038];

        // S edge: 0x3A
        table.PatternToTiles["AAAABBBA"] = [0x003A];
        table.PatternToTiles["AAAAABBA"] = [0x003A];
        table.PatternToTiles["AAAABBAA"] = [0x003A];
        table.PatternToTiles["AAAAABAA"] = [0x003A];
        table.PatternToTiles["AAAABABA"] = [0x003A];

        // N edge: 0x39
        table.PatternToTiles["BBBAAAAA"] = [0x0039];
        table.PatternToTiles["ABBAAAAA"] = [0x0039];
        table.PatternToTiles["BBAAAAAA"] = [0x0039];
        table.PatternToTiles["ABAAAAAA"] = [0x0039];
        table.PatternToTiles["BABAAAAA"] = [0x0039];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x003C]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x003C];
        table.PatternToTiles["BAAAAAAA"] = [0x003E]; // NW
        table.PatternToTiles["AAABABBA"] = [0x003E];
        table.PatternToTiles["AAAABAAA"] = [0x003D]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x003D];
        table.PatternToTiles["AABAAAAA"] = [0x003B]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x003B];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x0035]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x0035];
        table.PatternToTiles["AABBBBAA"] = [0x0035];
        table.PatternToTiles["AAABBBAA"] = [0x0035];

        table.PatternToTiles["BBBBBAAA"] = [0x0034]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0034];
        table.PatternToTiles["ABBBBAAA"] = [0x0034];
        table.PatternToTiles["ABBBAAAA"] = [0x0034];

        table.PatternToTiles["BBBAAABB"] = [0x0033]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0033];
        table.PatternToTiles["BBBAAAAB"] = [0x0033];
        table.PatternToTiles["BBAAAAAB"] = [0x0033];

        table.PatternToTiles["BAAABBBB"] = [0x0036]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0036];
        table.PatternToTiles["BAAAABBB"] = [0x0036];
        table.PatternToTiles["AAAAABBB"] = [0x0036];

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

        _dragonTransitions![(Biome.Grass, Biome.Sand)] = table;
    }

    /// <summary>
    /// Add Grass->Forest transitions based on DragonMod grass2forest.txt
    /// </summary>
    private void AddGrassToForestTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Forest };

        // === EDGES ===
        // W edge: 0xCB, 0xCC
        table.PatternToTiles["BAAAAABB"] = [0x00CB, 0x00CC];
        table.PatternToTiles["BAAAAAAB"] = [0x00CB, 0x00CC];
        table.PatternToTiles["AAAAAABB"] = [0x00CB, 0x00CC];
        table.PatternToTiles["AAAAAAAB"] = [0x00CB, 0x00CC];
        table.PatternToTiles["BAAAAABA"] = [0x00CB, 0x00CC];

        // E edge: 0xCE
        table.PatternToTiles["AABBBAAA"] = [0x00CE];
        table.PatternToTiles["AAABBAAA"] = [0x00CE];
        table.PatternToTiles["AABBAAAA"] = [0x00CE];
        table.PatternToTiles["AAABAAAA"] = [0x00CE];
        table.PatternToTiles["AABABAAA"] = [0x00CE];

        // S edge: 0xD1, 0xD2
        table.PatternToTiles["AAAABBBA"] = [0x00D1, 0x00D2];
        table.PatternToTiles["AAAAABBA"] = [0x00D1, 0x00D2];
        table.PatternToTiles["AAAABBAA"] = [0x00D1, 0x00D2];
        table.PatternToTiles["AAAAABAA"] = [0x00D1, 0x00D2];
        table.PatternToTiles["AAAABABA"] = [0x00D1, 0x00D2];

        // N edge: 0xC8, 0xC9
        table.PatternToTiles["BBBAAAAA"] = [0x00C8, 0x00C9];
        table.PatternToTiles["ABBAAAAA"] = [0x00C8, 0x00C9];
        table.PatternToTiles["BBAAAAAA"] = [0x00C8, 0x00C9];
        table.PatternToTiles["ABAAAAAA"] = [0x00C8, 0x00C9];
        table.PatternToTiles["BABAAAAA"] = [0x00C8, 0x00C9];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x00CF]; // SW (same as NE in this file)
        table.PatternToTiles["ABABBAAA"] = [0x00CF];
        table.PatternToTiles["BAAAAAAA"] = [0x00CD]; // NW
        table.PatternToTiles["AAABABBA"] = [0x00CD];
        table.PatternToTiles["AAAABAAA"] = [0x00D0]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x00D0];
        table.PatternToTiles["AABAAAAA"] = [0x00CF]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x00CF];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x00D5]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x00D5];
        table.PatternToTiles["AABBBBAA"] = [0x00D5];
        table.PatternToTiles["AAABBBAA"] = [0x00D5];

        table.PatternToTiles["BBBBBAAA"] = [0x00D7]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x00D7];
        table.PatternToTiles["ABBBBAAA"] = [0x00D7];
        table.PatternToTiles["ABBBAAAA"] = [0x00D7];

        table.PatternToTiles["BBBAAABB"] = [0x00D4]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x00D4];
        table.PatternToTiles["BBBAAAAB"] = [0x00D4];
        table.PatternToTiles["BBAAAAAB"] = [0x00D4];

        table.PatternToTiles["BAAABBBB"] = [0x00D6]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x00D6];
        table.PatternToTiles["BAAAABBB"] = [0x00D6];
        table.PatternToTiles["AAAAABBB"] = [0x00D6];

        // Nacor fallback: 0xC4
        table.PatternToTiles["ABBBBBBB"] = [0x00C4];
        table.PatternToTiles["BABBBBBB"] = [0x00C4];
        table.PatternToTiles["BBABBBBB"] = [0x00C4];
        table.PatternToTiles["BBBABBBB"] = [0x00C4];
        table.PatternToTiles["BBBBABBB"] = [0x00C4];
        table.PatternToTiles["BBBBBABB"] = [0x00C4];
        table.PatternToTiles["BBBBBBAB"] = [0x00C4];
        table.PatternToTiles["BBBBBBBA"] = [0x00C4];
        table.PatternToTiles["BBBBBBBB"] = [0x00C4];

        _dragonTransitions![(Biome.Grass, Biome.Forest)] = table;
    }

    /// <summary>
    /// Add Grass->Snow transitions based on DragonMod grass2snow.txt
    /// </summary>
    private void AddGrassToSnowTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Snow };

        // === EDGES ===
        // W edge: 0x5C1
        table.PatternToTiles["BAAAAABB"] = [0x05C1];
        table.PatternToTiles["BAAAAAAB"] = [0x05C1];
        table.PatternToTiles["AAAAAABB"] = [0x05C1];
        table.PatternToTiles["AAAAAAAB"] = [0x05C1];
        table.PatternToTiles["BAAAAABA"] = [0x05C1];

        // E edge: 0x5C0
        table.PatternToTiles["AABBBAAA"] = [0x05C0];
        table.PatternToTiles["AAABBAAA"] = [0x05C0];
        table.PatternToTiles["AABBAAAA"] = [0x05C0];
        table.PatternToTiles["AAABAAAA"] = [0x05C0];
        table.PatternToTiles["AABABAAA"] = [0x05C0];

        // S edge: 0x5BF
        table.PatternToTiles["AAAABBBA"] = [0x05BF];
        table.PatternToTiles["AAAAABBA"] = [0x05BF];
        table.PatternToTiles["AAAABBAA"] = [0x05BF];
        table.PatternToTiles["AAAAABAA"] = [0x05BF];
        table.PatternToTiles["AAAABABA"] = [0x05BF];

        // N edge: 0x5C2
        table.PatternToTiles["BBBAAAAA"] = [0x05C2];
        table.PatternToTiles["ABBAAAAA"] = [0x05C2];
        table.PatternToTiles["BBAAAAAA"] = [0x05C2];
        table.PatternToTiles["ABAAAAAA"] = [0x05C2];
        table.PatternToTiles["BABAAAAA"] = [0x05C2];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x05C6]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x05C6];
        table.PatternToTiles["BAAAAAAA"] = [0x05C3]; // NW
        table.PatternToTiles["AAABABBA"] = [0x05C3];
        table.PatternToTiles["AAAABAAA"] = [0x05C4]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x05C4];
        table.PatternToTiles["AABAAAAA"] = [0x05C5]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x05C5];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x05C7]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x05C7];
        table.PatternToTiles["AABBBBAA"] = [0x05C7];
        table.PatternToTiles["AAABBBAA"] = [0x05C7];

        table.PatternToTiles["BBBBBAAA"] = [0x05CA]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x05CA];
        table.PatternToTiles["ABBBBAAA"] = [0x05CA];
        table.PatternToTiles["ABBBAAAA"] = [0x05CA];

        table.PatternToTiles["BBBAAABB"] = [0x05C8]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x05C8];
        table.PatternToTiles["BBBAAAAB"] = [0x05C8];
        table.PatternToTiles["BBAAAAAB"] = [0x05C8];

        table.PatternToTiles["BAAABBBB"] = [0x05C9]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x05C9];
        table.PatternToTiles["BAAAABBB"] = [0x05C9];
        table.PatternToTiles["AAAAABBB"] = [0x05C9];

        // Nacor fallback: 0x770
        table.PatternToTiles["ABBBBBBB"] = [0x0770];
        table.PatternToTiles["BABBBBBB"] = [0x0770];
        table.PatternToTiles["BBABBBBB"] = [0x0770];
        table.PatternToTiles["BBBABBBB"] = [0x0770];
        table.PatternToTiles["BBBBABBB"] = [0x0770];
        table.PatternToTiles["BBBBBABB"] = [0x0770];
        table.PatternToTiles["BBBBBBAB"] = [0x0770];
        table.PatternToTiles["BBBBBBBA"] = [0x0770];
        table.PatternToTiles["BBBBBBBB"] = [0x0770];

        _dragonTransitions![(Biome.Grass, Biome.Snow)] = table;
    }

    /// <summary>
    /// Add Grass->Rock transitions based on DragonMod grass2mountain.txt
    /// </summary>
    private void AddGrassToRockTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Rock };

        // === EDGES ===
        // W edge: 0x23A
        table.PatternToTiles["BAAAAABB"] = [0x023A];
        table.PatternToTiles["BAAAAAAB"] = [0x023A];
        table.PatternToTiles["AAAAAABB"] = [0x023A];
        table.PatternToTiles["AAAAAAAB"] = [0x023A];
        table.PatternToTiles["BAAAAABA"] = [0x023A];

        // E edge: 0x23C
        table.PatternToTiles["AABBBAAA"] = [0x023C];
        table.PatternToTiles["AAABBAAA"] = [0x023C];
        table.PatternToTiles["AABBAAAA"] = [0x023C];
        table.PatternToTiles["AAABAAAA"] = [0x023C];
        table.PatternToTiles["AABABAAA"] = [0x023C];

        // S edge: 0x239
        table.PatternToTiles["AAAABBBA"] = [0x0239];
        table.PatternToTiles["AAAAABBA"] = [0x0239];
        table.PatternToTiles["AAAABBAA"] = [0x0239];
        table.PatternToTiles["AAAAABAA"] = [0x0239];
        table.PatternToTiles["AAAABABA"] = [0x0239];

        // N edge: 0x23B
        table.PatternToTiles["BBBAAAAA"] = [0x023B];
        table.PatternToTiles["ABBAAAAA"] = [0x023B];
        table.PatternToTiles["BBAAAAAA"] = [0x023B];
        table.PatternToTiles["ABAAAAAA"] = [0x023B];
        table.PatternToTiles["BABAAAAA"] = [0x023B];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x0232]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x0232];
        table.PatternToTiles["BAAAAAAA"] = [0x0233]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0233];
        table.PatternToTiles["AAAABAAA"] = [0x0231]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x0231];
        table.PatternToTiles["AABAAAAA"] = [0x0234]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0234];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x0237]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x0237];
        table.PatternToTiles["AABBBBAA"] = [0x0237];
        table.PatternToTiles["AAABBBAA"] = [0x0237];

        table.PatternToTiles["BBBBBAAA"] = [0x0236]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0236];
        table.PatternToTiles["ABBBBAAA"] = [0x0236];
        table.PatternToTiles["ABBBAAAA"] = [0x0236];

        table.PatternToTiles["BBBAAABB"] = [0x0235]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0235];
        table.PatternToTiles["BBBAAAAB"] = [0x0235];
        table.PatternToTiles["BBAAAAAB"] = [0x0235];

        table.PatternToTiles["BAAABBBB"] = [0x0238]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0238];
        table.PatternToTiles["BAAAABBB"] = [0x0238];
        table.PatternToTiles["AAAAABBB"] = [0x0238];

        // Nacor fallback: 0x6DC
        table.PatternToTiles["ABBBBBBB"] = [0x06DC];
        table.PatternToTiles["BABBBBBB"] = [0x06DC];
        table.PatternToTiles["BBABBBBB"] = [0x06DC];
        table.PatternToTiles["BBBABBBB"] = [0x06DC];
        table.PatternToTiles["BBBBABBB"] = [0x06DC];
        table.PatternToTiles["BBBBBABB"] = [0x06DC];
        table.PatternToTiles["BBBBBBAB"] = [0x06DC];
        table.PatternToTiles["BBBBBBBA"] = [0x06DC];
        table.PatternToTiles["BBBBBBBB"] = [0x06DC];

        _dragonTransitions![(Biome.Grass, Biome.Rock)] = table;
    }

    /// <summary>
    /// Add Grass->Cobblestone transitions based on DragonMod grass2cobble.txt
    /// </summary>
    private void AddGrassToCobblestoneTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Cobblestone };

        // === EDGES ===
        // W edge: 0x67F
        table.PatternToTiles["BAAAAABB"] = [0x067F];
        table.PatternToTiles["BAAAAAAB"] = [0x067F];
        table.PatternToTiles["AAAAAABB"] = [0x067F];
        table.PatternToTiles["AAAAAAAB"] = [0x067F];
        table.PatternToTiles["BAAAAABA"] = [0x067F];

        // E edge: 0x67E
        table.PatternToTiles["AABBBAAA"] = [0x067E];
        table.PatternToTiles["AAABBAAA"] = [0x067E];
        table.PatternToTiles["AABBAAAA"] = [0x067E];
        table.PatternToTiles["AAABAAAA"] = [0x067E];
        table.PatternToTiles["AABABAAA"] = [0x067E];

        // S edge: 0x67D
        table.PatternToTiles["AAAABBBA"] = [0x067D];
        table.PatternToTiles["AAAAABBA"] = [0x067D];
        table.PatternToTiles["AAAABBAA"] = [0x067D];
        table.PatternToTiles["AAAAABAA"] = [0x067D];
        table.PatternToTiles["AAAABABA"] = [0x067D];

        // N edge: 0x680
        table.PatternToTiles["BBBAAAAA"] = [0x0680];
        table.PatternToTiles["ABBAAAAA"] = [0x0680];
        table.PatternToTiles["BBAAAAAA"] = [0x0680];
        table.PatternToTiles["ABAAAAAA"] = [0x0680];
        table.PatternToTiles["BABAAAAA"] = [0x0680];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x0684]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x0684];
        table.PatternToTiles["BAAAAAAA"] = [0x0681]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0681];
        table.PatternToTiles["AAAABAAA"] = [0x0682]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x0682];
        table.PatternToTiles["AABAAAAA"] = [0x0683]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0683];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x0685]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x0685];
        table.PatternToTiles["AABBBBAA"] = [0x0685];
        table.PatternToTiles["AAABBBAA"] = [0x0685];

        table.PatternToTiles["BBBBBAAA"] = [0x0688]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0688];
        table.PatternToTiles["ABBBBAAA"] = [0x0688];
        table.PatternToTiles["ABBBAAAA"] = [0x0688];

        table.PatternToTiles["BBBAAABB"] = [0x0686]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0686];
        table.PatternToTiles["BBBAAAAB"] = [0x0686];
        table.PatternToTiles["BBAAAAAB"] = [0x0686];

        table.PatternToTiles["BAAABBBB"] = [0x0687]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0687];
        table.PatternToTiles["BAAAABBB"] = [0x0687];
        table.PatternToTiles["AAAAABBB"] = [0x0687];

        // Nacor fallback: 0x69D
        table.PatternToTiles["ABBBBBBB"] = [0x069D];
        table.PatternToTiles["BABBBBBB"] = [0x069D];
        table.PatternToTiles["BBABBBBB"] = [0x069D];
        table.PatternToTiles["BBBABBBB"] = [0x069D];
        table.PatternToTiles["BBBBABBB"] = [0x069D];
        table.PatternToTiles["BBBBBABB"] = [0x069D];
        table.PatternToTiles["BBBBBBAB"] = [0x069D];
        table.PatternToTiles["BBBBBBBA"] = [0x069D];
        table.PatternToTiles["BBBBBBBB"] = [0x069D];

        _dragonTransitions![(Biome.Grass, Biome.Cobblestone)] = table;
    }

    /// <summary>
    /// Add Grass->Swamp transitions based on DragonMod grass2swamp.txt
    /// </summary>
    private void AddGrassToSwampTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Swamp };

        // === EDGES ===
        // W edge: 0x3DDB
        table.PatternToTiles["BAAAAABB"] = [0x3DDB];
        table.PatternToTiles["BAAAAAAB"] = [0x3DDB];
        table.PatternToTiles["AAAAAABB"] = [0x3DDB];
        table.PatternToTiles["AAAAAAAB"] = [0x3DDB];
        table.PatternToTiles["BAAAAABA"] = [0x3DDB];

        // E edge: 0x3DDE
        table.PatternToTiles["AABBBAAA"] = [0x3DDE];
        table.PatternToTiles["AAABBAAA"] = [0x3DDE];
        table.PatternToTiles["AABBAAAA"] = [0x3DDE];
        table.PatternToTiles["AAABAAAA"] = [0x3DDE];
        table.PatternToTiles["AABABAAA"] = [0x3DDE];

        // S edge: 0x3DDF
        table.PatternToTiles["AAAABBBA"] = [0x3DDF];
        table.PatternToTiles["AAAAABBA"] = [0x3DDF];
        table.PatternToTiles["AAAABBAA"] = [0x3DDF];
        table.PatternToTiles["AAAAABAA"] = [0x3DDF];
        table.PatternToTiles["AAAABABA"] = [0x3DDF];

        // N edge: 0x3DE1
        table.PatternToTiles["BBBAAAAA"] = [0x3DE1];
        table.PatternToTiles["ABBAAAAA"] = [0x3DE1];
        table.PatternToTiles["BBAAAAAA"] = [0x3DE1];
        table.PatternToTiles["ABAAAAAA"] = [0x3DE1];
        table.PatternToTiles["BABAAAAA"] = [0x3DE1];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x3DD5]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x3DD5];
        table.PatternToTiles["BAAAAAAA"] = [0x3DD8]; // NW
        table.PatternToTiles["AAABABBA"] = [0x3DD8];
        table.PatternToTiles["AAAABAAA"] = [0x3DD6]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x3DD6];
        table.PatternToTiles["AABAAAAA"] = [0x3DD7]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x3DD7];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x3DE5]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x3DE5];
        table.PatternToTiles["AABBBBAA"] = [0x3DE5];
        table.PatternToTiles["AAABBBAA"] = [0x3DE5];

        table.PatternToTiles["BBBBBAAA"] = [0x3DE4]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x3DE4];
        table.PatternToTiles["ABBBBAAA"] = [0x3DE4];
        table.PatternToTiles["ABBBAAAA"] = [0x3DE4];

        table.PatternToTiles["BBBAAABB"] = [0x3DE6]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x3DE6];
        table.PatternToTiles["BBBAAAAB"] = [0x3DE6];
        table.PatternToTiles["BBAAAAAB"] = [0x3DE6];

        table.PatternToTiles["BAAABBBB"] = [0x3DE3]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x3DE3];
        table.PatternToTiles["BAAAABBB"] = [0x3DE3];
        table.PatternToTiles["AAAAABBB"] = [0x3DE3];

        _dragonTransitions![(Biome.Grass, Biome.Swamp)] = table;
    }

    /// <summary>
    /// Add Grass->Jungle transitions based on DragonMod grass2jungle.txt
    /// </summary>
    private void AddGrassToJungleTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Jungle };

        // === EDGES ===
        // W edge: 0x583
        table.PatternToTiles["BAAAAABB"] = [0x0583];
        table.PatternToTiles["BAAAAAAB"] = [0x0583];
        table.PatternToTiles["AAAAAABB"] = [0x0583];
        table.PatternToTiles["AAAAAAAB"] = [0x0583];
        table.PatternToTiles["BAAAAABA"] = [0x0583];

        // E edge: 0x582
        table.PatternToTiles["AABBBAAA"] = [0x0582];
        table.PatternToTiles["AAABBAAA"] = [0x0582];
        table.PatternToTiles["AABBAAAA"] = [0x0582];
        table.PatternToTiles["AAABAAAA"] = [0x0582];
        table.PatternToTiles["AABABAAA"] = [0x0582];

        // S edge: 0x581
        table.PatternToTiles["AAAABBBA"] = [0x0581];
        table.PatternToTiles["AAAAABBA"] = [0x0581];
        table.PatternToTiles["AAAABBAA"] = [0x0581];
        table.PatternToTiles["AAAAABAA"] = [0x0581];
        table.PatternToTiles["AAAABABA"] = [0x0581];

        // N edge: 0x584
        table.PatternToTiles["BBBAAAAA"] = [0x0584];
        table.PatternToTiles["ABBAAAAA"] = [0x0584];
        table.PatternToTiles["BBAAAAAA"] = [0x0584];
        table.PatternToTiles["ABAAAAAA"] = [0x0584];
        table.PatternToTiles["BABAAAAA"] = [0x0584];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x057A]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x057A];
        table.PatternToTiles["BAAAAAAA"] = [0x057D]; // NW
        table.PatternToTiles["AAABABBA"] = [0x057D];
        table.PatternToTiles["AAAABAAA"] = [0x057F]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x057F];
        table.PatternToTiles["AABAAAAA"] = [0x0579]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x0579];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x0580]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x0580];
        table.PatternToTiles["AABBBBAA"] = [0x0580];
        table.PatternToTiles["AAABBBAA"] = [0x0580];

        table.PatternToTiles["BBBBBAAA"] = [0x0582]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x0582];
        table.PatternToTiles["ABBBBAAA"] = [0x0582];
        table.PatternToTiles["ABBBAAAA"] = [0x0582];

        table.PatternToTiles["BBBAAABB"] = [0x0587]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x0587];
        table.PatternToTiles["BBBAAAAB"] = [0x0587];
        table.PatternToTiles["BBAAAAAB"] = [0x0587];

        table.PatternToTiles["BAAABBBB"] = [0x0583]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x0583];
        table.PatternToTiles["BAAAABBB"] = [0x0583];
        table.PatternToTiles["AAAAABBB"] = [0x0583];

        // Nacor fallback: 0x616
        table.PatternToTiles["ABBBBBBB"] = [0x0616];
        table.PatternToTiles["BABBBBBB"] = [0x0616];
        table.PatternToTiles["BBABBBBB"] = [0x0616];
        table.PatternToTiles["BBBABBBB"] = [0x0616];
        table.PatternToTiles["BBBBABBB"] = [0x0616];
        table.PatternToTiles["BBBBBABB"] = [0x0616];
        table.PatternToTiles["BBBBBBAB"] = [0x0616];
        table.PatternToTiles["BBBBBBBA"] = [0x0616];
        table.PatternToTiles["BBBBBBBB"] = [0x0616];

        _dragonTransitions![(Biome.Grass, Biome.Jungle)] = table;
    }

    /// <summary>
    /// Add Grass->Water transitions based on DragonMod grass2water-light.txt
    /// </summary>
    private void AddGrassToWaterTransitions()
    {
        var table = new DragonTransitionTable { FromBiome = Biome.Grass, ToBiome = Biome.Water };

        // === EDGES ===
        // W edge: 0x22, 0x26
        table.PatternToTiles["BAAAAABB"] = [0x0022, 0x0026];
        table.PatternToTiles["BAAAAAAB"] = [0x0022, 0x0026];
        table.PatternToTiles["AAAAAABB"] = [0x0022, 0x0026];
        table.PatternToTiles["AAAAAAAB"] = [0x0022, 0x0026];
        table.PatternToTiles["BAAAAABA"] = [0x0022, 0x0026];

        // E edge: 0x23, 0x27
        table.PatternToTiles["AABBBAAA"] = [0x0023, 0x0027];
        table.PatternToTiles["AAABBAAA"] = [0x0023, 0x0027];
        table.PatternToTiles["AABBAAAA"] = [0x0023, 0x0027];
        table.PatternToTiles["AAABAAAA"] = [0x0023, 0x0027];
        table.PatternToTiles["AABABAAA"] = [0x0023, 0x0027];

        // S edge: 0x21, 0x25
        table.PatternToTiles["AAAABBBA"] = [0x0021, 0x0025];
        table.PatternToTiles["AAAAABBA"] = [0x0021, 0x0025];
        table.PatternToTiles["AAAABBAA"] = [0x0021, 0x0025];
        table.PatternToTiles["AAAAABAA"] = [0x0021, 0x0025];
        table.PatternToTiles["AAAABABA"] = [0x0021, 0x0025];

        // N edge: 0x24, 0x28
        table.PatternToTiles["BBBAAAAA"] = [0x0024, 0x0028];
        table.PatternToTiles["ABBAAAAA"] = [0x0024, 0x0028];
        table.PatternToTiles["BBAAAAAA"] = [0x0024, 0x0028];
        table.PatternToTiles["ABAAAAAA"] = [0x0024, 0x0028];
        table.PatternToTiles["BABAAAAA"] = [0x0024, 0x0028];

        // === INNER CORNERS ===
        table.PatternToTiles["AAAAAABA"] = [0x001E]; // SW
        table.PatternToTiles["ABABBAAA"] = [0x001E];
        table.PatternToTiles["BAAAAAAA"] = [0x0003]; // NW
        table.PatternToTiles["AAABABBA"] = [0x0020];
        table.PatternToTiles["AAAABAAA"] = [0x001D]; // SE
        table.PatternToTiles["ABBAAAAB"] = [0x001D];
        table.PatternToTiles["AABAAAAA"] = [0x001F]; // NE
        table.PatternToTiles["BAAAABAB"] = [0x001F];

        // === OUTER CORNERS ===
        table.PatternToTiles["AABBBBBA"] = [0x001C]; // NW
        table.PatternToTiles["AAABBBBA"] = [0x001C];
        table.PatternToTiles["AABBBBAA"] = [0x001C];
        table.PatternToTiles["AAABBBAA"] = [0x001C];

        table.PatternToTiles["BBBBBAAA"] = [0x001A]; // SW
        table.PatternToTiles["BBBBAAAA"] = [0x001A];
        table.PatternToTiles["ABBBBAAA"] = [0x001A];
        table.PatternToTiles["ABBBAAAA"] = [0x001A];

        table.PatternToTiles["BBBAAABB"] = [0x001C]; // SE
        table.PatternToTiles["BBAAAABB"] = [0x001C];
        table.PatternToTiles["BBBAAAAB"] = [0x001C];
        table.PatternToTiles["BBAAAAAB"] = [0x001C];

        table.PatternToTiles["BAAABBBB"] = [0x001B]; // NE
        table.PatternToTiles["AAAABBBB"] = [0x001B];
        table.PatternToTiles["BAAAABBB"] = [0x001B];
        table.PatternToTiles["AAAAABBB"] = [0x001B];

        _dragonTransitions![(Biome.Grass, Biome.Water)] = table;
    }
}
