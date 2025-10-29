using ClassicUO.Assets;

namespace CentrED.Blueprints;

public record BlueprintTile(ushort Id, short X, short Y, short Z, ushort Hue, bool IsVisible)
{
    public BlueprintTile(MultiInfo info): this(info.ID, info.X, info.Y, info.Z, 0, info.IsVisible) { }
}