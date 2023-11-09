using CentrED.Map;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class InfoWindow : Window
{
    public override string Name => "Info";
    public MapObject? Selected;

    public override void Draw()
    {
        if (!Show)
            return;

        ImGui.Begin(Name, ref _show);
        if (Selected is LandObject lo)
        {
            var land = lo.Tile;
            ImGui.Text("Land");
            var texture = ArtLoader.Instance.GetLandTexture(land.Id, out var bounds);
            CEDGame.UIManager.DrawImage(texture, bounds);
            ImGui.Text($"x:{land.X} y:{land.Y} z:{land.Z}");
            ImGui.Text($"id: 0x{land.Id:X4} ({land.Id})");
        }
        else if (Selected is StaticObject so)
        {
            var staticTile = so.StaticTile;
            ImGui.Text("Static");
            var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
            var realBounds = ArtLoader.Instance.GetRealArtBounds(staticTile.Id);
            CEDGame.UIManager.DrawImage
            (
                texture,
                new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height)
            );
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: 0x{staticTile.Id:X4} ({staticTile.Id})");
            ImGui.Text($"hue: 0x{staticTile.Hue:X4} ({staticTile.Hue})");
        }
        ImGui.End();
    }
}