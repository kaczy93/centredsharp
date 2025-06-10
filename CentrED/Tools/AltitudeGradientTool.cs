using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using CentrED.Map;

namespace CentrED.Tools;

public class AltitudeGradientTool : BaseTool
{
    public override string Name => "Altitude Gradient";
    public override Keys Shortcut => Keys.F9;

    private enum GradientMode { Road, Area }
    private GradientMode _mode = GradientMode.Road;

    private enum AreaDirection { NorthSouth, WestEast }
    private AreaDirection _areaDirection = AreaDirection.NorthSouth;

    // Selection state
    private bool _isSelecting = false;
    private TileObject? _startTile = null;     // Anchor/center point
    private TileObject? _endTile = null;       // Target endpoint
    
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
        ImGui.Text(_mode == GradientMode.Road ? "Road Settings" : "Area Settings");
        ImGui.BeginChild("PathSettings", new System.Numerics.Vector2(-1, 200), ImGuiChildFlags.Borders);
        
        // Mode selector
        ImGui.Text("Mode:");
        ImGui.SameLine(150);
        ImGui.SetNextItemWidth(150);
        string[] modes = { "Road", "Area" };
        int modeIdx = (int)_mode;
        if (ImGui.Combo("##mode", ref modeIdx, modes, modes.Length))
            _mode = (GradientMode)modeIdx;

        // Only show road options in Road mode
        if (_mode == GradientMode.Road)
        {
            // Path width
            ImGui.Text("Path Width:");
            ImGui.SameLine(150);
            ImGui.SetNextItemWidth(150);
            ImGui.InputInt("##pathWidth", ref _pathWidth);
            if (_pathWidth < 1) _pathWidth = 1;
        }

        if (_mode == GradientMode.Area)
        {
            ImGui.Text("Gradient Direction:");
            ImGui.SameLine(150);
            ImGui.SetNextItemWidth(150);
            string[] areaDirs = { "North/South", "West/East" };
            int dirIdx = (int)_areaDirection;
            if (ImGui.Combo("##areadir", ref dirIdx, areaDirs, areaDirs.Length))
                _areaDirection = (AreaDirection)dirIdx;
        }
        
        // Smooth Selection Edges available in both modes
        ImGui.Checkbox("Smooth Selection Edges", ref _useEdgeFade);
        if (_useEdgeFade)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.InputInt("Smooth Edge Width", ref _edgeFadeWidth);
            if (_edgeFadeWidth < 1) _edgeFadeWidth = 1;
        }
        
        // Add Random Altitude option
        ImGui.Checkbox("Add Random Altitude", ref _useRandomAltitude);
        if (_useRandomAltitude)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.InputInt("Random Altitude Range", ref _randomAltitude);
            if (_randomAltitude < 0) _randomAltitude = 0;
        }

        // Terrain blending
        ImGui.Checkbox("Respect Existing Terrain", ref _respectExistingTerrain);
        if (_respectExistingTerrain)
        {
            ImGui.SetNextItemWidth(150);
            ImGui.SliderFloat("Blend Factor", ref _blendFactor, 0.0f, 1.0f, "%.2f");
        }
        
        ImGui.EndChild();
        
        ImGui.Spacing();
        
        // Instructions
        if (_mode == GradientMode.Road)
        {
            ImGui.BulletText("Hold CTRL and click to set the start point.");
            ImGui.BulletText("Drag to create a path to your target area.");
            ImGui.BulletText("Roads can only be created by dragging");
            ImGui.Indent();
            ImGui.Text("from start point towards South-East (bottom-right) direction.");
            ImGui.Unindent();
            ImGui.BulletText("Selections must increase both X and Y.");
            ImGui.BulletText("The tool will create a smooth height transition between different elevations.");
        }
        else
        {
            ImGui.BulletText("Hold CTRL and click to set the start point.");
            ImGui.BulletText("Drag to select a rectangular area.");
            ImGui.BulletText("The gradient will be applied across the entire area,");
            ImGui.Indent();
            ImGui.Text("from the anchor point to the opposite corner,");
            ImGui.Text("in the selected direction (N/S or W/E).");
            ImGui.Unindent();
            ImGui.BulletText("Useful for smoothing large hills or plains.");
        }
        
        // Show current selection status
        if (_startTile != null && _endTile != null)
        {
            ImGui.Separator();
            ImGui.Text($"Start: ({_startTile.Tile.X},{_startTile.Tile.Y}, Z:{_startTile.Tile.Z})");
            ImGui.Text($"End: ({_endTile.Tile.X},{_endTile.Tile.Y}, Z:{_endTile.Tile.Z})");
            
            int heightDiff = Math.Abs(_endTile.Tile.Z - _startTile.Tile.Z);
            ImGui.Text($"Height Difference: {heightDiff} tiles");
            
            if (_pathLength > 0 && heightDiff > 0)
            {
                float slope = heightDiff / _pathLength;
                ImGui.Text($"Average Slope: {slope:F2} (1:{(1/slope):F1})");
            }
        }
    }
    
    // GhostApply - Required by BaseTool
    protected override void GhostApply(TileObject? o)
    {
        if (o is not LandObject landObject)
            return;
        
        bool ctrlPressed = Keyboard.GetState().IsKeyDown(Keys.LeftControl) || 
                           Keyboard.GetState().IsKeyDown(Keys.RightControl);
        bool leftMousePressed = Mouse.GetState().LeftButton == ButtonState.Pressed;
        
        // Only start selection when CTRL+Left mouse button is pressed
        if (ctrlPressed) 
        {
            if (!_isSelecting && leftMousePressed)
            {
                // First click - set the start point (require mouse button press)
                _isSelecting = true;
                _startTile = o;
                _endTile = o;
                ClearGhosts(); // Clear any existing ghosts
            }
            else if (_isSelecting && _startTile != null && leftMousePressed)
            {
                // Only allow end point if both X and Y are greater than or equal to start
                if (o.Tile.X >= _startTile.Tile.X && o.Tile.Y >= _startTile.Tile.Y)
                {
                    // Update the end point as we drag
                    _endTile = o;
                    
                    // Calculate path direction and length
                    float dx = _endTile.Tile.X - _startTile.Tile.X;
                    float dy = _endTile.Tile.Y - _startTile.Tile.Y;
                    _pathDirection = new Vector2(dx, dy);
                    _pathLength = _pathDirection.Length();
                    
                    // Skip if start and end are the same tile
                    if (_pathLength < 0.1f && (_endTile.Tile.X != _startTile.Tile.X || _endTile.Tile.Y != _startTile.Tile.Y))
                        return;
                    
                    if (_pathLength > 0.1f)
                        _pathDirection = Vector2.Normalize(_pathDirection);
                    
                    // Preview the path
                    PreviewPath();
                }
            }
        }
    }
    
    // GhostClear - Required by BaseTool
    protected override void GhostClear(TileObject? o)
    {
        if (_isSelecting && !Keyboard.GetState().IsKeyDown(Keys.LeftControl) && 
            !Keyboard.GetState().IsKeyDown(Keys.RightControl))
        {
            _isSelecting = false;
            _startTile = null;
            _endTile = null;
            ClearGhosts();
        }
    }
    
    // InternalApply - Required by BaseTool
    protected override void InternalApply(TileObject? o)
    {
        if (_startTile == null || _endTile == null || Random.Next(100) >= _chance)
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
        _isSelecting = false;
        _startTile = null;
        _endTile = null;
    }
    
    // Clear all ghost tiles
    private void ClearGhosts()
    {
        foreach (var pair in new Dictionary<LandObject, LandObject>(MapManager.GhostLandTiles))
        {
            pair.Key.Reset();
            MapManager.GhostLandTiles.Remove(pair.Key);
        }
    }
    
    // Preview the path with ghost tiles
    private void PreviewPath()
    {
        if (_startTile == null || _endTile == null)
            return;
            
        // Clear previous ghosts
        ClearGhosts();
        
        // Get tile coordinates
        int startX = _startTile.Tile.X;
        int startY = _startTile.Tile.Y;
        int endX = _endTile.Tile.X; 
        int endY = _endTile.Tile.Y;
        
        // Start and end heights
        sbyte startZ = _startTile.Tile.Z;
        sbyte endZ = _endTile.Tile.Z;
        
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
        // Start area operation tracking in BaseTool
        OnAreaOperationStart((ushort)startX, (ushort)startY);
        OnAreaOperationUpdate((ushort)endX, (ushort)endY);

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
        
        // End area operation tracking
        OnAreaOperationEnd();
        
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
                            MapManager.AddToRecalculate(neighborTile);
                    }
                }
            }
            
            // This triggers proper recalculation of the tile
            MapManager.OnLandTileElevated(ghostTile.LandTile, ghostTile.LandTile.Z);
        }
    }

    // Add area gradient method
    private void GenerateAreaGradient(int startX, int startY, int endX, int endY, sbyte startZ, sbyte endZ)
    {
        // Start area operation tracking for preview
        OnAreaOperationStart((ushort)startX, (ushort)startY);
        OnAreaOperationUpdate((ushort)endX, (ushort)endY);
        int minX = Math.Min(startX, endX);
        int maxX = Math.Max(startX, endX);
        int minY = Math.Min(startY, endY);
        int maxY = Math.Max(startY, endY);
        int width = maxX - minX;
        int height = maxY - minY;
        bool horizontal = _areaDirection == AreaDirection.WestEast;
        int fadeWidth = _edgeFadeWidth;
        Dictionary<(int, int), LandObject> pendingGhostTiles = new();
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                    continue;
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null) continue;
                float t = horizontal
                    ? (width == 0 ? 0 : (float)(x - minX) / width)
                    : (height == 0 ? 0 : (float)(y - minY) / height);
                float gradientFactor = GetGradientFactor(Math.Clamp(t, 0, 1));
                int targetHeight = (int)(startZ + (endZ - startZ) * gradientFactor);
                sbyte currentZ = lo.Tile.Z;
                // Apply edge fade if enabled
                if (_useEdgeFade)
                {
                    float edgeDist = horizontal
                        ? Math.Min(x - minX, maxX - x)
                        : Math.Min(y - minY, maxY - y);
                    if (edgeDist < fadeWidth)
                    {
                        float fadeProgress = fadeWidth == 0 ? 0 : (fadeWidth - edgeDist) / (float)fadeWidth;
                        fadeProgress = Math.Clamp(fadeProgress, 0, 1);
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
        // End area operation tracking for preview
        OnAreaOperationEnd();
        // Update all tiles to ensure proper rendering in preview
        foreach (var kvp in pendingGhostTiles)
        {
            LandObject ghostTile = kvp.Value;
            for (int nx = ghostTile.Tile.X - 3; nx <= ghostTile.Tile.X + 3; nx++)
            {
                for (int ny = ghostTile.Tile.Y - 3; ny <= ghostTile.Tile.Y + 3; ny++)
                {
                    if (Client.IsValidX((ushort)nx) && Client.IsValidY((ushort)ny))
                    {
                        LandObject? neighborTile = MapManager.LandTiles[nx, ny];
                        if (neighborTile != null)
                            MapManager.AddToRecalculate(neighborTile);
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