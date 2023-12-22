using CentrED.IO.Models;
using CentrED.Map;
using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class InfoWindow : Window
{
    public override string Name => "Info";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };
    public MapObject? Selected;

    protected override void InternalDraw()
    {
        if (Selected is LandObject lo)
        {
            var landTile = lo.Tile;
            ImGui.Text("Land");
            var texture = ArtLoader.Instance.GetLandTexture(landTile.Id, out var bounds);
            CEDGame.UIManager.DrawImage(texture, bounds);
            ImGui.Text(TileDataLoader.Instance.LandData[landTile.Id].Name ?? "");
            ImGui.Text($"x:{landTile.X} y:{landTile.Y} z:{landTile.Z}");
            ImGui.Text($"id: 0x{landTile.Id:X4} ({landTile.Id})");
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
            ImGui.Text(TileDataLoader.Instance.StaticData[staticTile.Id].Name ?? "");
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: 0x{staticTile.Id:X4} ({staticTile.Id})");
            ImGui.Text($"hue: 0x{staticTile.Hue:X4} ({staticTile.Hue})");
        }
    }
}