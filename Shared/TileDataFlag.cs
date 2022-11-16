namespace Shared;

[Flags]
public enum TiledataFlag : ulong {
    /// <summary>
        /// Nothing is flagged.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        Background = 0x00000001,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        Weapon = 0x00000002,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        Transparent = 0x00000004,
        /// <summary>
        /// The tile is rendered with partial alpha-transparency.
        /// </summary>
        Translucent = 0x00000008,
        /// <summary>
        /// The tile is a wall.
        /// </summary>
        Wall = 0x00000010,
        /// <summary>
        /// The tile can cause damage when moved over.
        /// </summary>
        Damaging = 0x00000020,
        /// <summary>
        /// The tile may not be moved over or through.
        /// </summary>
        Impassable = 0x00000040,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        Wet = 0x00000080,
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown1 = 0x00000100,
        /// <summary>
        /// The tile is a surface. It may be moved over, but not through.
        /// </summary>
        Surface = 0x00000200,
        /// <summary>
        /// The tile is a stair, ramp, or ladder.
        /// </summary>
        Bridge = 0x00000400,
        /// <summary>
        /// The tile is stackable
        /// </summary>
        Generic = 0x00000800,
        /// <summary>
        /// The tile is a window. Like <see cref="TileFlag.NoShoot" />, tiles with this flag block line of sight.
        /// </summary>
        Window = 0x00001000,
        /// <summary>
        /// The tile blocks line of sight.
        /// </summary>
        NoShoot = 0x00002000,
        /// <summary>
        /// For single-amount tiles, the string "a " should be prepended to the tile name.
        /// </summary>
        ArticleA = 0x00004000,
        /// <summary>
        /// For single-amount tiles, the string "an " should be prepended to the tile name.
        /// </summary>
        ArticleAn = 0x00008000,
        /// <summary>
        /// Probably article The prepended to the tile name.
        /// </summary>
        ArticleThe = 0x00010000,
        /// <summary>
        /// The tile becomes translucent when walked behind. Boat masts also have this flag.
        /// </summary>
        Foliage = 0x00020000,
        /// <summary>
        /// Only gray pixels will be hued
        /// </summary>
        PartialHue = 0x00040000,
        /// <summary>
        /// NoHouse or Unknown. Needs further research.
        /// </summary>
        NoHouse = 0x00080000,
        /// <summary>
        /// The tile is a map--in the cartography sense. Unknown usage.
        /// </summary>
        Map = 0x00100000,
        /// <summary>
        /// The tile is a container.
        /// </summary>
        Container = 0x00200000,
        /// <summary>
        /// The tile may be equipped.
        /// </summary>
        Wearable = 0x00400000,
        /// <summary>
        /// The tile gives off light.
        /// </summary>
        LightSource = 0x00800000,
        /// <summary>
        /// The tile is animated.
        /// </summary>
        Animation = 0x01000000,
        /// <summary>
        /// Gargoyles can fly over or NoDiagonal
        /// </summary>
        HoverOver = 0x02000000,
        /// <summary>
        /// NoDiagonal (Unknown3).
        /// </summary>
        NoDiagonal = 0x04000000,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        Armor = 0x08000000,
        /// <summary>
        /// The tile is a slanted roof.
        /// </summary>
        Roof = 0x10000000,
        /// <summary>
        /// The tile is a door. Tiles with this flag can be moved through by ghosts and GMs.
        /// </summary>
        Door = 0x20000000,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        StairBack = 0x40000000,
        /// <summary>
        /// Not yet documented.
        /// </summary>
        StairRight = 0x80000000,
        /// <summary>
        /// Blend Alphas, tile blending.
        /// </summary>
        AlphaBlend = 0x0100000000,
        /// <summary>
        /// Uses new art style? Something related to the nodraw tile?
        /// </summary>
        UseNewArt = 0x0200000000,
        /// <summary>
        /// Has art being used?
        /// </summary>
        ArtUsed = 0x0400000000,
        /// <summary>
        /// Unused8 ??
        /// </summary>
        Unused8 = 0x08000000000,
        /// <summary>
        /// Disallow shadow on this tile, lightsource? lava?
        /// </summary>
        NoShadow = 0x1000000000,
        /// <summary>
        /// Let pixels bleed in to other tiles? Is this Disabling Texture Clamp?
        /// </summary>
        PixelBleed = 0x2000000000,
        /// <summary>
        /// Play tile animation once.
        /// </summary>
        PlayAnimOnce = 0x4000000000,
        /// <summary>
        /// Movable multi? Cool ships and vehicles etc? Something related to the masts ???
        /// </summary>
        MultiMovable = 0x10000000000,
        /// <summary>
        /// Unused10
        /// </summary>
        Unused10 = 0x20000000000,
        /// <summary>
        /// Unused11
        /// </summary>
        Unused11 = 0x40000000000,
        /// <summary>
        /// Unused12
        /// </summary>
        Unused12 = 0x80000000000,
        /// <summary>
        /// Unused13
        /// </summary>
        Unused13 = 0x100000000000,
        /// <summary>
        /// Unused14
        /// </summary>
        Unused14 = 0x200000000000,
        /// <summary>
        /// Unused15
        /// </summary>
        Unused15 = 0x400000000000,
        /// <summary>
        /// Unused16
        /// </summary>
        Unused16 = 0x800000000000,
        /// <summary>
        /// Unused17
        /// </summary>
        Unused17 = 0x1000000000000,
        /// <summary>
        /// Unused18
        /// </summary>
        Unused18 = 0x2000000000000,
        /// <summary>
        /// Unused19
        /// </summary>
        Unused19 = 0x4000000000000,
        /// <summary>
        /// Unused20
        /// </summary>
        Unused20 = 0x8000000000000,
        /// <summary>
        /// Unused21
        /// </summary>
        Unused21 = 0x10000000000000,
        /// <summary>
        /// Unused22
        /// </summary>
        Unused22 = 0x20000000000000,
        /// <summary>
        /// Unused23
        /// </summary>
        Unused23 = 0x40000000000000,
        /// <summary>
        /// Unused24
        /// </summary>
        Unused24 = 0x80000000000000,
        /// <summary>
        /// Unused25
        /// </summary>
        Unused25 = 0x100000000000000,
        /// <summary>
        /// Unused26
        /// </summary>
        Unused26 = 0x200000000000000,
        /// <summary>
        /// Unused27
        /// </summary>
        Unused27 = 0x400000000000000,
        /// <summary>
        /// Unused28
        /// </summary>
        Unused28 = 0x800000000000000,
        /// <summary>
        /// Unused29
        /// </summary>
        Unused29 = 0x1000000000000000,
        /// <summary>
        /// Unused30
        /// </summary>
        Unused30 = 0x2000000000000000,
        /// <summary>
        /// Unused31
        /// </summary>
        Unused31 = 0x4000000000000000,
        /// <summary>
        /// Unused32
        /// </summary>
        Unused32 = 0x8000000000000000
}