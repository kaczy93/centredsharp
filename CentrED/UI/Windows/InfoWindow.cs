using CentrED.Map;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace CentrED.UI.Windows; 

public class InfoWindow : Window{
    public InfoWindow(UIManager uiManager) : base(uiManager) { }
    public override string Name => "Info";
    public MapObject? Selected;

    public override void Draw() {
        if (!Show) return;

        ImGui.Begin(Id, ref _show);
        if (Selected is LandObject lo) {
            var land = lo.Tile;
            ImGui.Text("Land");
            var texture = ArtLoader.Instance.GetLandTexture(land.Id, out var bounds);
            _uiManager.DrawImage(texture, bounds);
            ImGui.Text($"x:{land.X} y:{land.Y} z:{land.Z}");
            ImGui.Text($"id: 0x{land.Id:X4} ({land.Id})");
        }
        else if (Selected is StaticObject so) {
            var staticTile = so.StaticTile;
            ImGui.Text("Static");
            var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
            var realBounds = ArtLoader.Instance.GetRealArtBounds(staticTile.Id);
            _uiManager.DrawImage(texture, new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height));
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: 0x{staticTile.Id:X4} ({staticTile.Id})");
            ImGui.Text($"hue: 0x{staticTile.Hue - 1:X4} ({staticTile.Hue - 1})");
        }
        ImGui.End();
    }
}