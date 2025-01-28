namespace CentrED;

public struct TileDataStatic
{
     public TileDataStatic
        (
            ulong flags,
            byte weight,
            byte layer,
            int count,
            ushort animId,
            ushort hue,
            ushort lightIndex,
            byte height,
            string name
        )
        {
            DataFlags = (TileDataFlag) flags;
            Weight = weight;
            Layer = layer;
            Count = count;
            AnimID = animId;
            Hue = hue;
            LightIndex = lightIndex;
            Height = height;
            Name = name;
        }

        public TileDataFlag DataFlags;
        public byte Weight;
        public byte Layer;
        public int Count;
        public ushort AnimID;
        public ushort Hue;
        public ushort LightIndex;
        public byte Height;
        public string Name;

        public bool IsAnimated => (DataFlags & TileDataFlag.Animation) != 0;
        public bool IsBridge => (DataFlags & TileDataFlag.Bridge) != 0;
        public bool IsImpassable => (DataFlags & TileDataFlag.Impassable) != 0;
        public bool IsSurface => (DataFlags & TileDataFlag.Surface) != 0;
        public bool IsWearable => (DataFlags & TileDataFlag.Wearable) != 0;
        public bool IsInternal => (DataFlags & TileDataFlag.Internal) != 0;
        public bool IsBackground => (DataFlags & TileDataFlag.Background) != 0;
        public bool IsNoDiagonal => (DataFlags & TileDataFlag.NoDiagonal) != 0;
        public bool IsWet => (DataFlags & TileDataFlag.Wet) != 0;
        public bool IsFoliage => (DataFlags & TileDataFlag.Foliage) != 0;
        public bool IsRoof => (DataFlags & TileDataFlag.Roof) != 0;
        public bool IsTranslucent => (DataFlags & TileDataFlag.Translucent) != 0;
        public bool IsPartialHue => (DataFlags & TileDataFlag.PartialHue) != 0;
        public bool IsStackable => (DataFlags & TileDataFlag.Generic) != 0;
        public bool IsTransparent => (DataFlags & TileDataFlag.Transparent) != 0;
        public bool IsContainer => (DataFlags & TileDataFlag.Container) != 0;
        public bool IsDoor => (DataFlags & TileDataFlag.Door) != 0;
        public bool IsWall => (DataFlags & TileDataFlag.Wall) != 0;
        public bool IsLight => (DataFlags & TileDataFlag.LightSource) != 0;
        public bool IsNoShoot => (DataFlags & TileDataFlag.NoShoot) != 0;
        public bool IsWeapon => (DataFlags & TileDataFlag.Weapon) != 0;
        public bool IsMultiMovable => (DataFlags & TileDataFlag.MultiMovable) != 0;
        public bool IsWindow => (DataFlags & TileDataFlag.Window) != 0;
}