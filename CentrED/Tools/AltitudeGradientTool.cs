using System.Numerics;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using CentrED.Map;

namespace CentrED.Tools;

public class AltitudeGradientTool : BaseTool
{
    public override string Name => "Altitude Gradient";
    public override Keys Shortcut => Keys.F9;

    private enum GradientMode { Road, Area }
    private GradientMode _mode = GradientMode.Road;

    // Selection state
    private TileObject? _areaEndTile;       // Target endpoint
    
    // Path settings
    private int _pathWidth = 5;                // Width of path in tiles
    private bool _useEdgeFade = true;          // Fade edges of path
    private int _edgeFadeWidth = 2;            // Width of fade area in tiles
    private bool _respectExistingTerrain = true;
    private float _blendFactor = 0.5f;
    private bool _useRandomAltitude = false;
    private int _randomAltitude = 0;          // Random altitude adjustment
    
    // Internal calculation fields
    private Vector2 _pathDirection;            // Direction vector from start to end
    private float _pathLength;                 // Length of path in tiles

    // Main drawing UI
    internal override void Draw()
    {
        ImGui.Text(_mode == GradientMode.Road ? "Road Settings"u8 : "Area Settings"u8);
        ImGui.BeginChild("PathSettings", new Vector2(-1, 200), ImGuiChildFlags.Borders);

        // Mode selector
        ImGui.Text("Mode:"u8);
        ImGui.SameLine(100);
        ImGui.SetNextItemWidth(100);
        string[] modes = { "Road", "Area" };
        int modeIdx = (int)_mode;
        if (ImGui.Combo("##mode", ref modeIdx, modes, modes.Length))
            _mode = (GradientMode)modeIdx;

        // Only show road options in Road mode
        if (_mode == GradientMode.Road)
        {
            // Path width
            ImGui.Text("Path Width:"u8);
            ImGui.SameLine(100);
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("##pathWidth", ref _pathWidth);
            if (_pathWidth < 1) _pathWidth = 1;
        }


        // Smooth Selection Edges available in both modes
        ImGui.Checkbox("Smooth Selection Edges", ref _useEdgeFade);
        if (_useEdgeFade)
        {
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("Smooth Edge Width", ref _edgeFadeWidth);
            if (_edgeFadeWidth < 1) _edgeFadeWidth = 1;
        }

        // Add Random Altitude option
        ImGui.Checkbox("Add Random Altitude", ref _useRandomAltitude);
        if (_useRandomAltitude)
        {
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("Random Altitude Range", ref _randomAltitude);
            if (_randomAltitude < 0) _randomAltitude = 0;
        }

        // Terrain blending
        ImGui.Checkbox("Respect Existing Terrain", ref _respectExistingTerrain);
        if (_respectExistingTerrain)
        {
            ImGui.SetNextItemWidth(100);
            ImGui.SliderFloat("Blend Factor", ref _blendFactor, 0.0f, 1.0f, "%.2f");
        }

        ImGui.EndChild();

        ImGui.Spacing();

        // Instructions
        if (_mode == GradientMode.Road)
        {
            BulletTextWrapped("CTRL+Click to set the start point."u8);
            BulletTextWrapped("Drag in any direction to define the path."u8);
            BulletTextWrapped("Width, edge fade, and blending apply along the path."u8);
        }
        else // Area
        {
            BulletTextWrapped("CTRL+Click to set the anchor."u8);
            BulletTextWrapped("Drag to any opposite corner; gradient follows the drag direction."u8);
            BulletTextWrapped("Edge fade softens toward the area borders."u8);
        }

        // Show current selection status
        if (AreaStartTile != null && _areaEndTile != null && _pathLength > 0.5f)
        {
            ImGui.Separator();
            ImGui.Text($"Start: ({AreaStartTile.Tile.X},{AreaStartTile.Tile.Y}, Z:{AreaStartTile.Tile.Z})");
            ImGui.Text($"End: ({_areaEndTile.Tile.X},{_areaEndTile.Tile.Y}, Z:{_areaEndTile.Tile.Z})");

            int heightDiff = Math.Abs(_areaEndTile.Tile.Z - AreaStartTile.Tile.Z);
            ImGui.Text($"Height Difference: {heightDiff} tiles");

            float rise = _areaEndTile.Tile.Z - AreaStartTile.Tile.Z;
            float run  = _pathLength;
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

    private static void BulletTextWrapped(ReadOnlySpan<byte> text)
    {
        ImGui.Bullet();
        ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
        ImGui.PushTextWrapPos(0);          // 0 => wrap at window content edge
        ImGui.TextWrapped(text);
        ImGui.PopTextWrapPos();
    }
    
    protected override void OnAreaOperationStart(TileObject? o)
    {
        base.OnAreaOperationStart(o);
        _areaEndTile = AreaStartTile;
    }
    
    protected override void OnAreaOperationUpdate(TileObject? to)
    {
        base.OnAreaOperationUpdate(to);
        _areaEndTile = Application.CEDGame.MapManager.LandTiles[Area.X2, Area.Y2];

        if (AreaStartTile == null || _areaEndTile == null){
            ClearGhosts();
            return; 
        }
        
        var dx = _areaEndTile.Tile.X - AreaStartTile.Tile.X;
        var dy = _areaEndTile.Tile.Y - AreaStartTile.Tile.Y;
        _pathDirection = new Vector2(dx, dy);
        _pathLength = _pathDirection.Length();
                
        // Skip if start and end are the same tile
        if (_pathLength < 0.1f && (_areaEndTile.Tile.X != AreaStartTile.Tile.X || _areaEndTile.Tile.Y != AreaStartTile.Tile.Y))
            return;
        if (_pathLength > 0.1f)
            _pathDirection = Vector2.Normalize(_pathDirection);
        
        PreviewPath();
    }
    
    protected override void OnAreaOperationEnd()
    {
        base.OnAreaOperationEnd();
        ClearGhosts();
        _areaEndTile = null;
    }
    
    protected override void GhostApply(TileObject? o)
    {
        //All handled by OnAreaOperationUpdate
    }
    
    protected override void GhostClear(TileObject? o)
    {
        //All handled by OnAreaOperationUpdate/End
    }
    
    protected override void InternalApply(TileObject? o)
    {
        if (AreaStartTile == null || _areaEndTile == null || Random.Next(100) >= _chance)
            return;
        
        // Collect debug info
        int startX = AreaStartTile.Tile.X;
        int startY = AreaStartTile.Tile.Y;
        int endX = _areaEndTile.Tile.X;
        int endY = _areaEndTile.Tile.Y;
        sbyte startZ = AreaStartTile.Tile.Z;
        sbyte endZ = _areaEndTile.Tile.Z;
        float dx = endX - startX;
        float dy = endY - startY;
        int pathWidth = _pathWidth;
        int edgeFadeWidth = _edgeFadeWidth;
        bool useEdgeFade = _useEdgeFade;
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
        
        // Clean up
        ClearGhosts();
        AreaStartTile = null;
        _areaEndTile = null;
    }
    
    private void ClearGhosts()
    {
        foreach (var lo in MapManager.GhostLandTiles.Keys)
        {
            lo.Reset();
        }
        MapManager.GhostLandTiles.Clear();
    }
    
    // Preview the path with ghost tiles
    private void PreviewPath()
    {
        if (AreaStartTile == null || _areaEndTile == null)
            return;
            
        // Clear previous ghosts
        ClearGhosts();
        
        // Get tile coordinates
        int startX = AreaStartTile.Tile.X;
        int startY = AreaStartTile.Tile.Y;
        int endX = _areaEndTile.Tile.X; 
        int endY = _areaEndTile.Tile.Y;
        
        // Start and end heights
        sbyte startZ = AreaStartTile.Tile.Z;
        sbyte endZ = _areaEndTile.Tile.Z;
        
        if (_mode == GradientMode.Road)
        {
            // Calculate direction vector
            Vector2 pathVector = new Vector2(endX - startX, endY - startY);
            float pathLength = pathVector.Length();
            
            if (pathLength < 0.001f)
                return;
            
            // Create modified tiles
            GeneratePathTiles(startX, startY, endX, endY, startZ, endZ);
        }
        else // Area mode
        {
            GenerateAreaGradient(startX, startY, endX, endY, startZ, endZ);
        }
    }

    // Generate path tiles using a better, universal algorithm
    private void GeneratePathTiles(int startX, int startY, int endX, int endY, sbyte startZ, sbyte endZ)
    {
        // Create a bounding box with minimal padding
        int padding = _pathWidth / 2 + 1;
        int minX = Math.Min(startX, endX) - padding;
        int maxX = Math.Max(startX, endX) + padding;
        int minY = Math.Min(startY, endY) - padding;
        int maxY = Math.Max(startY, endY) + padding;
        
        // Calculate path vector and normalization factors
        float dx = endX - startX;
        float dy = endY - startY;
        float pathLengthSquared = dx * dx + dy * dy;
        
        // Dictionary to track modified tiles
        Dictionary<(int, int), LandObject> pendingGhostTiles = new();
        
        // Create line equation ax + by + c = 0
        float a = endY - startY;
        float b = startX - endX;
        float c = endX * startY - startX * endY;
        float lineLengthFactor = (float)Math.Sqrt(a * a + b * b);
        
        // Process all tiles in bounding box
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Skip if out of bounds
                if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                    continue;
                
                // Get the land object
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null)
                    continue;
                
                // Calculate perpendicular distance using line equation
                float perpDistance = Math.Abs(a * x + b * y + c) / lineLengthFactor;
                
                // FIX: Make path width consistent with user expectations
                // The exact maximum distance to include in the path
                float maxPathDistance = _pathWidth / 2.0f;
                
                // Skip if beyond path width
                if (perpDistance > maxPathDistance)
                    continue;
                
                // Calculate position along the path
                float px = x - startX;
                float py = y - startY;
                float t = 0;
                if (pathLengthSquared > 0)
                    t = (px * dx + py * dy) / pathLengthSquared;
                
                // Skip if beyond path endpoints with small buffer
                float buffer = 0.1f;
                if (t < -buffer || t > 1.0f + buffer)
                    continue;
                
                // Get current height and calculate target height
                sbyte currentZ = lo.Tile.Z;
                float gradientFactor = GetGradientFactor(Math.Clamp(t, 0, 1));
                int targetHeight = (int)(startZ + (endZ - startZ) * gradientFactor);
                
                // Apply edge fade if enabled
                if (_useEdgeFade)
                {
                    // Calculate the width of the central flat section
                    float centralWidth = Math.Max(0.5f, _pathWidth - _edgeFadeWidth * 2);
                    float fadeStart = centralWidth / 2.0f;
                    
                    // If we're in the fade zone
                    if (perpDistance > fadeStart)
                    {
                        // Calculate how far into fade zone (0 to 1)
                        float fadeProgress = (perpDistance - fadeStart) / _edgeFadeWidth;
                        fadeProgress = Math.Clamp(fadeProgress, 0, 1);
                        
                        // Blend between path height and original height
                        targetHeight = (int)Math.Round(
                            currentZ * fadeProgress + targetHeight * (1 - fadeProgress)
                        );
                    }
                }
                
                // Apply random altitude if enabled
                if (_useRandomAltitude && _randomAltitude > 0)
                {
                    targetHeight += Random.Next(-_randomAltitude, _randomAltitude + 1);
                }
                
                // Respect existing terrain
                if (_respectExistingTerrain)
                {
                    targetHeight = (int)Math.Round(
                        currentZ * (1 - _blendFactor) + targetHeight * _blendFactor
                    );
                }
                
                // Only create ghost if height changed
                if (targetHeight != currentZ)
                {
                    sbyte newZ = (sbyte)Math.Clamp(targetHeight, -128, 127);
                    var newTile = new LandTile(lo.LandTile.Id, lo.Tile.X, lo.Tile.Y, newZ);
                    var ghostTile = new LandObject(newTile);
                    
                    pendingGhostTiles[(x, y)] = ghostTile;
                    MapManager.GhostLandTiles[lo] = ghostTile;
                    lo.Visible = false;
                }
            }
        }
        
        // Update all tiles to ensure proper rendering
        foreach (var kvp in pendingGhostTiles)
        {
            LandObject ghostTile = kvp.Value;
            
            // Mark the tile and its neighbors for recalculation
            for (int nx = ghostTile.Tile.X - 3; nx <= ghostTile.Tile.X + 3; nx++)
            {
                for (int ny = ghostTile.Tile.Y - 3; ny <= ghostTile.Tile.Y + 3; ny++)
                {
                    if (Client.IsValidX((ushort)nx) && Client.IsValidY((ushort)ny))
                    {
                        LandObject? neighborTile = MapManager.LandTiles[nx, ny];
                        if (neighborTile != null)
                            MapManager._ToRecalculate.Add(neighborTile);
                    }
                }
            }
            
            // This triggers proper recalculation of the tile
            MapManager.OnLandTileElevated(ghostTile.LandTile, ghostTile.LandTile.Z);
        }
    }

    // Area gradient method
    private void GenerateAreaGradient(int startX, int startY, int endX, int endY, sbyte startZ, sbyte endZ)
    {
        int minX = Math.Min(startX, endX);
        int maxX = Math.Max(startX, endX);
        int minY = Math.Min(startY, endY);
        int maxY = Math.Max(startY, endY);

        float dx = endX - startX;
        float dy = endY - startY;
        float denom = dx * dx + dy * dy;
        if (denom < 1e-6f)
        {
            return;
        }

        // Unit normal to gradient direction
        float nx = -dy, ny = dx;
        float nlen = MathF.Sqrt(nx * nx + ny * ny);
        nx /= nlen; ny /= nlen;

        int fadeWidth = _edgeFadeWidth;
        var pendingGhostTiles = new Dictionary<(int, int), LandObject>();

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                    continue;
                    
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null) continue;

                // Projection t along start->end, direction-agnostic
                float px = x - startX, py = y - startY;
                float t = (px * dx + py * dy) / denom;
                t = Math.Clamp(t, 0f, 1f);

                float gradientFactor = GetGradientFactor(t);
                int targetHeight = (int)(startZ + (endZ - startZ) * gradientFactor);
                sbyte currentZ = lo.Tile.Z;

                // Edge fade measured along normal to the gradient direction
                if (_useEdgeFade && fadeWidth > 0)
                {
                    float d1 = (Math.Abs(nx) > 1e-6f) ? Math.Abs((minX - x) / nx) : float.PositiveInfinity;
                    float d2 = (Math.Abs(nx) > 1e-6f) ? Math.Abs((maxX - x) / nx) : float.PositiveInfinity;
                    float d3 = (Math.Abs(ny) > 1e-6f) ? Math.Abs((minY - y) / ny) : float.PositiveInfinity;
                    float d4 = (Math.Abs(ny) > 1e-6f) ? Math.Abs((maxY - y) / ny) : float.PositiveInfinity;
                    float edgeDist = MathF.Min(MathF.Min(d1, d2), MathF.Min(d3, d4)); // tiles along normal

                    if (edgeDist < fadeWidth)
                    {
                        float fadeProgress = (fadeWidth - edgeDist) / fadeWidth; // 0..1
                        fadeProgress = Math.Clamp(fadeProgress, 0f, 1f);
                        targetHeight = (int)Math.Round(currentZ * fadeProgress + targetHeight * (1 - fadeProgress));
                    }
                }
                // Apply random altitude if enabled
                if (_useRandomAltitude && _randomAltitude > 0)
                {
                    targetHeight += Random.Next(-_randomAltitude, _randomAltitude + 1);
                }

                if (_respectExistingTerrain)
                {
                    targetHeight = (int)Math.Round(
                            currentZ * (1 - _blendFactor) + targetHeight * _blendFactor
                        );
                }

                if (targetHeight == currentZ) continue;

                sbyte newZ = (sbyte)Math.Clamp(targetHeight, -128, 127);
                var newTile = new LandTile(lo.LandTile.Id, lo.Tile.X, lo.Tile.Y, newZ);
                var ghostTile = new LandObject(newTile);
                pendingGhostTiles[(x, y)] = ghostTile;
                MapManager.GhostLandTiles[lo] = ghostTile;
                lo.Visible = false;
            }
        }
        // Update all tiles to ensure proper rendering in preview
        foreach (var kvp in pendingGhostTiles)
        {
            LandObject ghostTile = kvp.Value;
            for (int nx2 = ghostTile.Tile.X - 3; nx2 <= ghostTile.Tile.X + 3; nx2++)
            {
                for (int ny2 = ghostTile.Tile.Y - 3; ny2 <= ghostTile.Tile.Y + 3; ny2++)
                {
                    if (Client.IsValidX((ushort)nx2) && Client.IsValidY((ushort)ny2))
                    {
                        LandObject? neighborTile = MapManager.LandTiles[nx2, ny2];
                        if (neighborTile != null)
                            MapManager._ToRecalculate.Add(neighborTile);
                    }
                }
            }
            MapManager.OnLandTileElevated(ghostTile.LandTile, ghostTile.LandTile.Z);
        }
    }

    // Get gradient factor - always linear
    private float GetGradientFactor(float t)
    {
        return t;
    }
}