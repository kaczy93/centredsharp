using System.Numerics;
using CentrED.IO;
using CentrED.IO.Models;
using ClassicUO.Assets;
using ImGuiNET;
using static CentrED.IO.Models.Direction;

namespace CentrED.UI.Windows;

public class LandBrushWindow : Window
{
    public override string Name => "LandBrush";

    private static readonly Vector2 TexSize = new(44, 44);
    
    private int _landBrushIndex;
    private string _landBrushName;
    public LandBrush? Selected;
    protected override void InternalDraw()
    {
        ImGui.Text("Landbrush");
        var landBrushes = ProfileManager.ActiveProfile.LandBrush;
        var names = new[] { String.Empty }.Concat(landBrushes.Keys).ToArray();
        if (ImGui.Combo("", ref _landBrushIndex, names, names.Length))
        {
            _landBrushName = names[_landBrushIndex];
            if (_landBrushIndex == 0)
            {
                Selected = null;
            }
            else
            {
                Selected = landBrushes[_landBrushName];
            }
        }
        if (Selected != null)
        {
            ImGui.Text(Selected.Name);
            ImGui.Text("Full tiles:");
            foreach (var fullTile in Selected.Tiles)
            {
                var tex = TexmapsLoader.Instance.GetLandTexture(fullTile, out var bounds);
                Application.CEDGame.UIManager.DrawImage(tex, bounds, TexSize);
                ImGui.SameLine();
                ImGui.Text($"0x{fullTile:X4}");
            }
            ImGui.Text("Transitions:");
            foreach (var (name, transitions) in Selected.Transitions)
            {
                ImGui.Text("To " + name);
                foreach (var transition in transitions)
                {
                    Draw(transition);
                }
            }
        }
    }

    private void Draw(LandBrushTransition transition)
    {
        var tex = TexmapsLoader.Instance.GetLandTexture(transition.TileID, out var bounds);
        if (tex != null)
        {
            Application.CEDGame.UIManager.DrawImage(tex, bounds, TexSize);
            ImGui.SameLine();
        }
        var type = transition.Direction;
        ImGui.Text($"0x{transition.TileID:X4}");
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text($"{f(type, Up)} {f(type, North)} {f(type, Right)}");
        ImGui.Text($"{f(type, West)}   {f(type, East)}");
        ImGui.Text($"{f(type, Left)} {f(type, South)} {f(type, Down)}");
        ImGui.EndGroup();
        ImGui.SameLine();

        ImGui.Text($"{type:F}");
    }

    private byte f(Direction t, Direction f)
    {
        return (byte)((t & f) > 0 ? 1 : 0);
    }
}