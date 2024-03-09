using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public class LandBrushTool : BaseTool
{
    public override string Name => "LandBrush";
    public override Keys Shortcut => Keys.F7;

    public override void OnActivated(TileObject? o)
    {
        UIManager.HuesWindow.Show = true;
    }

    protected override void GhostApply(TileObject? o)
    {
        var targetBrush = UIManager.LandBrushWindow.Selected;
        if (targetBrush == null)
            return;
        if (o is LandObject lo)
        {
            //Consider all tiles around o
            if (MapManager.tileLandBrushesNames.TryGetValue(lo.Tile.Id, out var landBrushesNames))
            {
                var landBrush = ProfileManager.ActiveProfile.LandBrush[landBrushesNames[0]];
                // var toTile = MapManager.LandTiles[lo.Tile.X - 1, lo.Tile.Y - 1];
                // var toTileBrushNames = MapManager.tileLandBrushesNames[toTile.Tile.Id];
                if (landBrush.Transitions.TryGetValue(targetBrush.Name, out var transitions))
                {
                    var lbt = transitions.FirstOrDefault(lbt => lbt.Direction == Direction.West);
                    if (lbt != null)
                    {
                        lo.Visible = false;
                        var newTile = new LandTile(lbt.TileID, o.Tile.X, o.Tile.Y, o.Tile.Z);
                        MapManager.GhostLandTiles[lo] = new LandObject(newTile);
                    }
                }
            }
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is LandObject lo)
        {
            //Clear this tile and all around
            lo.Reset();
            MapManager.GhostLandTiles.Remove(lo);
        }
    }

    protected override void Apply(TileObject? o)
    {
        if(o is LandObject lo)
        {
            //Apply to this tile and all around
            if(MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
            {
                o.Tile.Id = ghostTile.Tile.Id;
            }
        }
    }
}