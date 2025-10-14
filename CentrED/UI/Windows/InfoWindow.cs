using CentrED.IO.Models;
using CentrED.Map;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class InfoWindow : Window
{
    public override string Name => LangManager.Get(INFO_WINDOW) + "###Info";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoScrollWithMouse;

    public TileObject? Selected
    {
        get => _Selected;
        set
        {
            _Selected = value;
            if (_Selected != null)
            {
                _otherTiles.Clear();
                var landTile = CEDGame.MapManager.LandTiles[_Selected.Tile.X, _Selected.Tile.Y];
                if(landTile != null)
                {
                    _otherTiles.Add(landTile);
                }
                var staticTiles = CEDGame.MapManager.StaticsManager.Get(_Selected.Tile.X, _Selected.Tile.Y);
                if (staticTiles != null)
                {
                    _otherTiles.AddRange(staticTiles);
                }
                _otherTilesNames = _otherTiles.Select(o=> o.Tile.ShortString()).ToArray();
            }
            UpdateSelectedOtherTile(0);
        }
    }

    private TileObject? _Selected;

    private List<TileObject> _otherTiles = [];
    private string?[] _otherTilesNames = [];
    private int _otherTileIndex;
    private TileObject? _otherSelected;
    
    protected override void InternalDraw()
    {
        if (_Selected == null) return;
        
        DrawTileInfo(_Selected);
     
        ImGui.SeparatorText($"{LangManager.Get(ALL_TILES_AT)} {_Selected.Tile.X},{_Selected.Tile.Y}");
        if(ImGui.Combo("##OtherTiles", ref _otherTileIndex, _otherTilesNames, _otherTiles.Count))
        {
            UpdateSelectedOtherTile(_otherTileIndex);
            
        }
        if (ImGui.GetIO().MouseWheel != 0 && ImGui.IsItemHovered())
        {
            var incVal = ImGui.GetIO().MouseWheel > 0 ? -1 : 1;
            UpdateSelectedOtherTile(_otherTileIndex + incVal);
            ImGui.GetIO().MouseWheel = 0;
        }
        if (_otherSelected != null)
        {
            if (ImGui.Button(LangManager.Get(APPLY_TOOL)))
            {
                CEDGame.MapManager.ActiveTool.Apply(_otherSelected);
            }
            DrawTileInfo(_otherSelected);
        }
    }

    private void UpdateSelectedOtherTile(int newIndex)
    {
        _otherTileIndex = newIndex;
        if (_otherTiles.Count == 0)
        {
            _otherSelected = null;
        }
        else {
            if (_otherTileIndex < 0)
                _otherTileIndex = 0;
            else if (_otherTileIndex >= _otherTiles.Count)
                _otherTileIndex = _otherTiles.Count - 1;
            
            _otherSelected = _otherTiles[_otherTileIndex];
        }
    }

    private void DrawTileInfo(TileObject? o)
    {
        if (o is LandObject lo)
        {
            var landTile = lo.Tile;
            ImGui.Text(LangManager.Get(LAND));
            var spriteInfo = CEDGame.MapManager.Arts.GetLand(landTile.Id);
            if (!CEDGame.UIManager.DrawImage(spriteInfo.Texture, spriteInfo.UV))
            {
                ImGui.TextColored(ImGuiColor.Red, LangManager.Get(TEXTURE_NOT_FOUND));
            }
            var tileData = CEDGame.MapManager.UoFileManager.TileData.LandData[landTile.Id];
            ImGui.Text(tileData.Name ?? "");
            ImGui.Text($"X:{landTile.X} Y:{landTile.Y} Z:{landTile.Z}");
            ImGui.Text($"ID: 0x{landTile.Id:X4} ({landTile.Id})");
            ImGui.Text(LangManager.Get(FLAGS));
            ImGui.Text(tileData.Flags.ToString().Replace(", ", "\n"));
        }
        else if (o is StaticObject so)
        {
            var staticTile = so.StaticTile;
            ImGui.Text(LangManager.Get(STATIC));
            ref var indexEntry = ref CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(staticTile.Id + 0x4000);
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
                ImGui.TextColored(ImGuiColor.Red, LangManager.Get(TEXTURE_NOT_FOUND));
            }
            var tileData = CEDGame.MapManager.UoFileManager.TileData.StaticData[staticTile.Id];
            ImGui.Text(tileData.Name ?? "");
            ImGui.Text($"X:{staticTile.X} Y:{staticTile.Y} Z:{staticTile.Z}");
            ImGui.Text($"ID: 0x{staticTile.Id:X4} ({staticTile.Id})");
            ImGui.Text($"{LangManager.Get(HUE)}: 0x{staticTile.Hue:X4} ({staticTile.Hue})");
            ImGui.Text($"{LangManager.Get(HEIGHT)}: {tileData.Height}");
            ImGui.Text(LangManager.Get(FLAGS));
            ImGui.Text(tileData.Flags.ToString().Replace(", ", "\n"));
        }
    }
}