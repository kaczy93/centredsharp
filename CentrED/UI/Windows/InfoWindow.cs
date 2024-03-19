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
            if (texture != null)
            {
                CEDGame.UIManager.DrawImage(texture, bounds);
            }
            else
            {
                ImGui.TextColored(UIManager.Red, "Art Invalid!");
            }
            var tileData = TileDataLoader.Instance.LandData[landTile.Id];
            ImGui.Text(tileData.Name ?? "");
            ImGui.Text($"x:{landTile.X} y:{landTile.Y} z:{landTile.Z}");
            ImGui.Text($"id: 0x{landTile.Id:X4} ({landTile.Id})");
            ImGui.Text("Flags:");
            ImGui.Text(tileData.Flags.ToString().Replace(", ", "\n"));
        }
        else if (Selected is StaticObject so)
        {
            var staticTile = so.StaticTile;
            ImGui.Text("Static");
            ref var indexEntry = ref ArtLoader.Instance.GetValidRefEntry(staticTile.Id + 0x4000);
            var texture = ArtLoader.Instance.GetStaticTexture((uint)(staticTile.Id + indexEntry.AnimOffset), out var bounds);
            if(texture != null)
            {
                var realBounds = ArtLoader.Instance.GetRealArtBounds(staticTile.Id);
                CEDGame.UIManager.DrawImage
                (
                    texture,
                    new Rectangle(bounds.X + realBounds.X, bounds.Y + realBounds.Y, realBounds.Width, realBounds.Height)
                );
            }
            else
            {
                ImGui.TextColored(UIManager.Red, "Art Invalid!");
            }
            var tileData = TileDataLoader.Instance.StaticData[staticTile.Id];
            ImGui.Text(tileData.Name ?? "");
            ImGui.Text($"x:{staticTile.X} y:{staticTile.Y} z:{staticTile.Z}");
            ImGui.Text($"id: 0x{staticTile.Id:X4} ({staticTile.Id})");
            ImGui.Text($"hue: 0x{staticTile.Hue:X4} ({staticTile.Hue})");
            ImGui.Text($"height: {tileData.Height}");
            ImGui.Text("Flags:");
            ImGui.Text(tileData.Flags.ToString().Replace(", ", "\n"));
        }
    }
}