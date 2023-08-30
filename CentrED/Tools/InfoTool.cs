using CentrED.Map;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace CentrED.Tools; 

public class InfoTool : Tool {
    internal InfoTool(UIManager uiManager, MapManager mapManager) : base(uiManager, mapManager) { }

    private Object? _selected;
    public override string Name => "InfoTool";

    protected override void DrawWindowInternal() {
        if (_selected is LandObject lo) {
            var land = lo.root;
            ImGui.Text("Land");
            var texture = ArtLoader.Instance.GetLandTexture(land.Id, out var bounds);
            _uiManager.DrawImage(texture, bounds);
            ImGui.Text($"x:{land.X} y:{land.Y} z:{land.Z}");
            ImGui.Text($"id: {land.Id}");
        }
        else if (_selected is StaticObject so) {
            var staticTile = so.root;
            ImGui.Text("Static");
            var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
            var realBounds = ArtLoader.Instance.GetRealArtBounds(staticTile.Id);
            _uiManager.DrawImage(texture, new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height));
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: {staticTile.Id}");
            ImGui.Text($"hue: {staticTile.Hue}");
        }
    }

    public override void OnMousePressed(Object? selected) {
        _selected = selected;
    }
}