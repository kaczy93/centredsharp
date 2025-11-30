using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using CentrED.Map;
using CentrED.UI;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class AltitudeGradientTool : Tool
{
    private enum GradientMode
    {
        PATH, 
        AREA
    }
    
    public override string Name => LangManager.Get(ALTITUDE_GRADIENT_TOOL);
    public override Keys Shortcut => Keys.F9;
    
    private int _mode = 0;
    private int _pathWidth = 5;
    private int _smoothEdgeWidth = 0;
    private float _blendFactor = 0.5f;
    private int _randomZ = 0;
    
    private bool _active;
    private TileObject? _startTile;
    private TileObject? _endTile;
    private Vector2 _deltaVector;

    private List<LandObject> _ghostedTiles = [];

    internal override void Draw()
    {
        ImGui.Text(LangManager.Get(MODE));
        ImGui.RadioButton(LangManager.Get(PATH), ref _mode, (int)GradientMode.PATH);
        ImGui.RadioButton(LangManager.Get(AREA), ref _mode, (int)GradientMode.AREA);
        
        if (_mode == (int)GradientMode.PATH)
        {
            ImGui.Separator();
            ImGui.Text(LangManager.Get(MODE_PARAMETERS));
            ImGuiEx.DragInt(LangManager.Get(PATH_WIDTH), ref _pathWidth, 1, 1, 256);
        }
        
        ImGui.Separator();
        ImGui.Text(LangManager.Get(COMMON_PARAMETERS));
        ImGuiEx.DragInt(LangManager.Get(SMOOTH_EDGE), ref _smoothEdgeWidth, 1, 0, 256);
        ImGui.SetItemTooltip(LangManager.Get(SMOOTH_EDGE_TOOLTIP));
        
        ImGuiEx.DragInt(LangManager.Get(ADD_RANDOM_Z), ref _randomZ, 1, 0, 127);
        ImGui.SetItemTooltip(LangManager.Get(ADD_RANDOM_Z_TOOLTIP));
        
        ImGui.SliderFloat(LangManager.Get(BLEND_FACTOR), ref _blendFactor, 0.0f, 1.0f, "%.2f");
        ImGui.SetItemTooltip(LangManager.Get(BLEND_FACTOR_TOOLTIP));

        ImGui.Spacing();

        if (_startTile != null && _endTile != null)
        {
            ImGui.Separator();
            ImGui.Text($"Start: ({_startTile.Tile.X},{_startTile.Tile.Y}, Z:{_startTile.Tile.Z})");
            ImGui.Text($"End: ({_endTile.Tile.X},{_endTile.Tile.Y}, Z:{_endTile.Tile.Z})");

            int heightDiff = Math.Abs(_endTile.Tile.Z - _startTile.Tile.Z);
            ImGui.Text($"Height Difference: {heightDiff} tiles");

            float rise = _endTile.Tile.Z - _startTile.Tile.Z;
            float run  = _deltaVector.Length();
            if (Math.Abs(rise) < 0.001f)
            {
                ImGui.Text("Slope: Flat (0%)"u8);
            }
            else
            {
                float pct = MathF.Abs(rise) / run * 100f;
                string dir = rise > 0 ? "Up" : "Down";
                ImGui.Text($"Slope: {pct:0.0}% ({dir})");
            }
        }
    }

    public override void OnDeactivated(TileObject? o)
    {
        base.OnDeactivated(o);
        ClearGhosts();
        _startTile = null;
        _endTile = null;
        _deltaVector = Vector2.Zero;
    }

    public override void OnMousePressed(TileObject? o)
    {
        if (_active)
            return;
        
        _active = true;
        _startTile = o;
        _endTile = o;
    }

    public override void OnMouseReleased(TileObject? o)
    {
        if (!_active)
            return;

        if (_startTile != _endTile)
        {
            _active = false;
            Client.BeginUndoGroup();
            InternalApply(o);
            Client.EndUndoGroup();
        }
    }

    public override void OnMouseEnter(TileObject? o)
    {
        if (!_active || _startTile == null)
            return;
        
        _endTile = o switch
        {
            LandObject lo => lo,
            not null => MapManager.LandTiles[o.Tile.X, o.Tile.Y],
            _ => null
        };
        
        if (_endTile == null)
        {
            return;
        }
        
        var dx = _endTile.Tile.X - _startTile.Tile.X;
        var dy = _endTile.Tile.Y - _startTile.Tile.Y;
        _deltaVector = new Vector2(dx, dy);

        if (_deltaVector.Length() < 1.0f)
        {
            return;
        }

        ApplyGhosts();
    }

    public override void OnMouseLeave(TileObject? o)
    {
        if (!_active)
            return;
        
        ClearGhosts();
    }

    
    private void InternalApply(TileObject? o)
    {
        if (_startTile == null || _endTile == null)
            return;
        
        // Collect debug info
        int startX = _startTile.Tile.X;
        int startY = _startTile.Tile.Y;
        int endX = _endTile.Tile.X;
        int endY = _endTile.Tile.Y;
        sbyte startZ = _startTile.Tile.Z;
        sbyte endZ = _endTile.Tile.Z;
        float dx = endX - startX;
        float dy = endY - startY;
        int pathWidth = _pathWidth;
        int edgeFadeWidth = _smoothEdgeWidth;
        bool useEdgeFade = _smoothEdgeWidth > 0;
        int minX = Math.Min(startX, endX) - (pathWidth / 2 + 1);
        int maxX = Math.Max(startX, endX) + (pathWidth / 2 + 1);
        int minY = Math.Min(startY, endY) - (pathWidth / 2 + 1);
        int maxY = Math.Max(startY, endY) + (pathWidth / 2 + 1);
        int nwCount = 0, neCount = 0, swCount = 0, seCount = 0;
        int totalModified = 0;
        foreach (var pair in MapManager.GhostLandTiles)
        {
            pair.Key.LandTile.ReplaceLand(pair.Key.LandTile.Id, pair.Value.Tile.Z);
            // Quadrant counting
            int x = pair.Key.Tile.X;
            int y = pair.Key.Tile.Y;
            float normalizedX = x - startX;
            float normalizedY = y - startY;
            if (normalizedX >= 0) {
                if (normalizedY >= 0) seCount++;
                else neCount++;
            } else {
                if (normalizedY >= 0) swCount++;
                else nwCount++;
            }
            totalModified++;
        }
        if (totalModified > 0)
        {
            Console.WriteLine($"===== NEW PATH =====");
            Console.WriteLine($"Start: ({startX},{startY}) Z:{startZ}");
            Console.WriteLine($"End: ({endX},{endY}) Z:{endZ}");
            Console.WriteLine($"Direction: ({dx},{dy})");
            Console.WriteLine($"Scanning area: ({minX},{minY}) to ({maxX},{maxY})");
            Console.WriteLine($"Path width: {pathWidth}");
            Console.WriteLine($"Path width setting: {pathWidth} tiles");
            Console.WriteLine($"Edge fade setting: {(useEdgeFade ? edgeFadeWidth.ToString() : "disabled")} tiles");
            Console.WriteLine($"Modified tiles by quadrant - NW: {nwCount}, NE: {neCount}, SW: {swCount}, SE: {seCount}");
            Console.WriteLine($"Total modified: {totalModified} tiles");
        }
        
        ClearGhosts();
        _startTile = null;
        _endTile = null;
    }
    
    private void ClearGhosts()
    {
        foreach (var lo in _ghostedTiles)
        {
            lo.Reset();
            MapManager.GhostLandTiles.Remove(lo);
            MapManager.OnLandTileElevated(lo.LandTile, lo.LandTile.Z);
        }
        _ghostedTiles.Clear();
    }
    
    private void ApplyGhosts()
    {
        if (_startTile == null || _endTile == null)
            return;
            
        if (_mode == (int)GradientMode.PATH)
        {
            ApplyGhostsPath();
        }
        else
        {
            ApplyGhostsArea();
        }
    }

    private void ApplyGhostsPath()
    {
        // Create a bounding box with minimal padding
        int padding = _pathWidth / 2 + 1;
        
        var x1 = _startTile.Tile.X;
        var y1 =  _startTile.Tile.Y;
        var z1 = _startTile.Tile.Z;
        var x2 = _endTile.Tile.X;
        var y2 = _endTile.Tile.Y;
        var z2 = _endTile.Tile.Z;
        var minX = Math.Min(x1, x2) - padding;
        var maxX = Math.Max(x1, x2) + padding;
        var minY = Math.Min(y1, y2) - padding;
        var maxY = Math.Max(y1, y2) + padding;
        
        // Create line equation ax + by + c = 0
        float a = y2 - y1;
        float b = x1 - x2;
        float c = x2 * y1 - x1 * y2;
        var lineLengthFactor = (float)Math.Sqrt(a * a + b * b);
        var maxDistance = _pathWidth / 2.0f;
        
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                if (!Client.IsValidX(x) || !Client.IsValidY(y))
                    continue;
                
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null)
                    continue;
                
                // Calculate perpendicular distance using line equation
                var distanceToLine = Math.Abs(a * x + b * y + c) / lineLengthFactor;
                if (distanceToLine > maxDistance)
                    continue;
                
                // Calculate position along the path
                float px = x - x1;
                float py = y - y1;
                float t = 0;
                if (_deltaVector.LengthSquared() > 0)
                    t = (px * _deltaVector.X + py * _deltaVector.Y) / _deltaVector.LengthSquared();
                
                // Skip if beyond path endpoints with small buffer
                float buffer = 0.1f;
                if (t < -buffer || t > 1.0f + buffer)
                    continue;
                
                // Get current height and calculate target height
                sbyte currentZ = lo.Tile.Z;
                float gradientFactor = GetGradientFactor(Math.Clamp(t, 0, 1));
                int targetHeight = (int)(z1 + (z2 - z1) * gradientFactor);
                
                if (_smoothEdgeWidth > 0)
                {
                    // Calculate the width of the central flat section
                    float centralWidth = Math.Max(0.5f, _pathWidth - _smoothEdgeWidth * 2);
                    float fadeStart = centralWidth / 2.0f;
                    
                    // If we're in the fade zone
                    if (distanceToLine > fadeStart)
                    {
                        // Calculate how far into fade zone (0 to 1)
                        float fadeProgress = (distanceToLine - fadeStart) / _smoothEdgeWidth;
                        fadeProgress = Math.Clamp(fadeProgress, 0, 1);
                        
                        // Blend between path height and original height
                        targetHeight = (int)Math.Round(currentZ * fadeProgress + targetHeight * (1 - fadeProgress));
                    }
                }
                
                ApplyGhostCommon(lo, targetHeight);
            }
        }
    }

    private void ApplyGhostsArea()
    {
        var x1 = _startTile.Tile.X;
        var y1 =  _startTile.Tile.Y;
        var z1 = _startTile.Tile.Z;
        var x2 = _endTile.Tile.X;
        var y2 = _endTile.Tile.Y;
        var z2 = _endTile.Tile.Z;
        var minX = Math.Min(x1, x2);
        var maxX = Math.Max(x1, x2);
        var minY = Math.Min(y1, y2);
        var maxY = Math.Max(y1, y2);

        // Unit normal to gradient direction
        var nvector = Vector2.Normalize(_deltaVector);
        var nx = nvector.X;
        var ny = nvector.Y;

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                if (!Client.IsValidX(x) || !Client.IsValidY(y))
                    continue;
                    
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null) 
                    continue;

                // Projection t along start->end, direction-agnostic
                float px = x - x1, py = y - y1;
                float t = (px * _deltaVector.X + py * _deltaVector.Y) / _deltaVector.LengthSquared();
                t = Math.Clamp(t, 0f, 1f);

                float gradientFactor = GetGradientFactor(t);
                int targetHeight = (int)(z1 + (z2 - z1) * gradientFactor);
                sbyte currentZ = lo.Tile.Z;

                // Edge fade measured along normal to the gradient direction
                if (_smoothEdgeWidth > 0)
                {
                    float d1 = (Math.Abs(nx) > 1e-6f) ? Math.Abs((minX - x) / nx) : float.PositiveInfinity;
                    float d2 = (Math.Abs(nx) > 1e-6f) ? Math.Abs((maxX - x) / nx) : float.PositiveInfinity;
                    float d3 = (Math.Abs(ny) > 1e-6f) ? Math.Abs((minY - y) / ny) : float.PositiveInfinity;
                    float d4 = (Math.Abs(ny) > 1e-6f) ? Math.Abs((maxY - y) / ny) : float.PositiveInfinity;
                    float edgeDist = MathF.Min(MathF.Min(d1, d2), MathF.Min(d3, d4)); // tiles along normal

                    if (edgeDist < _smoothEdgeWidth)
                    {
                        float fadeProgress = (_smoothEdgeWidth - edgeDist) / _smoothEdgeWidth; // 0..1
                        fadeProgress = Math.Clamp(fadeProgress, 0f, 1f);
                        targetHeight = (int)Math.Round(currentZ * fadeProgress + targetHeight * (1 - fadeProgress));
                    }
                }
                
                ApplyGhostCommon(lo, targetHeight);
            }
        }
    }

    private void ApplyGhostCommon(LandObject lo, int targetHeight)
    {
        var currentZ = lo.Tile.Z;
        if (_randomZ > 0)
        {
            targetHeight += Random.Shared.Next(-_randomZ, _randomZ + 1);
        }

        if (_blendFactor < 1.0f)
        {
            targetHeight = (int)Math.Round(currentZ * (1 - _blendFactor) + targetHeight * _blendFactor);
        }

        if (targetHeight == currentZ) return;

        sbyte newZ = (sbyte)Math.Clamp(targetHeight, -128, 127);
        var newTile = new LandTile(lo.LandTile.Id, lo.Tile.X, lo.Tile.Y, newZ);
        var ghostTile = new LandObject(newTile);
        MapManager.GhostLandTiles[lo] = ghostTile;
        lo.Visible = false;
        MapManager.OnLandTileElevated(ghostTile.LandTile, ghostTile.LandTile.Z);
        _ghostedTiles.Add(lo);
    }

    private float GetGradientFactor(float t)
    {
        return t; //Linear
    }
}