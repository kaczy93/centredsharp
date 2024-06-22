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
            var spriteInfo = CEDGame.MapManager.Arts.GetLand(landTile.Id);
            if (spriteInfo.Texture != null)
            {
                CEDGame.UIManager.DrawImage(spriteInfo.Texture, spriteInfo.UV);
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
            var spriteInfo = CEDGame.MapManager.Arts.GetArt((uint)(staticTile.Id + indexEntry.AnimOffset));
            if(spriteInfo.Texture != null)
            {
                var realBounds =  CEDGame.MapManager.Arts.GetRealArtBounds(staticTile.Id);
                CEDGame.UIManager.DrawImage
                (
                    spriteInfo.Texture,
                    new Rectangle(spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height)
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