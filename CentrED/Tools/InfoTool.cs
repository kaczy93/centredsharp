using System.Numerics;
using CentrED.UI;
using ClassicUO.Assets;
using ImGuiNET;

namespace CentrED.Tools; 

public class InfoTool : Tool {
    internal InfoTool(UIManager uiManager) : base(uiManager) { }

    private Object? _selected;
    public override string Name => "InfoTool";

    protected override void DrawWindowInternal() {
        if (_selected is LandTile land) {
            ImGui.Text("Land");
            ImGui.Text($"x:{land.X} y:{land.Y} z:{land.Z}");
            ImGui.Text($"id: {land.Id}");
        }
        else if (_selected is StaticTile staticTile) {
            ImGui.Text("Static");
            var texture = ArtLoader.Instance.GetStaticTexture(staticTile.Id, out var bounds);
            _uiManager.DrawImage(texture, bounds);
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: {staticTile.Id}");
            ImGui.Text($"hue: {staticTile.Hue}");
        }
    }

    public override void Action(Object? selected) {
        _selected = selected;
    }
}