using CentrED.Map;
using CentrED.UI;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static CentrED.Application;

namespace CentrED.Tools;

public class MeshEditTool : BaseTool
{
    public override string Name => "MeshEdit";
    public override Keys Shortcut => Keys.F8;

    // Geometry settings
    private int _innerRadius = 1;
    private bool _useAdditionalRadius1 = false;
    private int _additionalRadius1 = 4;
    private bool _useAdditionalRadius2 = false;
    private int _additionalRadius2 = 7;
    private int _outerRadius = 10;
    private int _heightDepth = 20;
    private float _param1 = 0.00f;
    private float _param2 = 1.00f;
    private float _param3 = 0.00f;

    // Limitation conditions
    private bool _useFixedAltitude = false;
    private int _fixedAltitude = 0;
    private bool _useMinZThreshold = false;
    private int _minZThreshold = -128;
    private bool _useMaxZThreshold = false;
    private int _maxZThreshold = 127;

    // Overlay options
    private int _selectedElevationOption = 0; // 0=Elevation, 1=Lowering, 2=Add altitude
    private int _randomAltitude = 0;
    private int _selectedOverlayOption = 0; // 0=Additions, 1=Replacement

    internal override void Draw()
    {
        // Geometry section
        ImGui.Text("Geometry");
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Inner radius r.:");
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##innerRadius", ref _innerRadius);
        
        ImGui.Checkbox("Add'l radius 1:", ref _useAdditionalRadius1);
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##addRadius1", ref _additionalRadius1);
        
        ImGui.Checkbox("Add'l radius 2:", ref _useAdditionalRadius2);
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##addRadius2", ref _additionalRadius2);
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Outer radius:");
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##outerRadius", ref _outerRadius);
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Height / Depth:");
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##heightDepth", ref _heightDepth);
        
        // Three parameter fields
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("##param1", ref _param1, 0.0f, 0.0f, "%.2f");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("##param2", ref _param2, 0.0f, 0.0f, "%.2f");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.InputFloat("##param3", ref _param3, 0.0f, 0.0f, "%.2f");

        ImGui.Separator();
        
        // Conditions for limitations section
        ImGui.Text("Conditions for limitations");
        
        ImGui.Checkbox("Force fixed altitude:", ref _useFixedAltitude);
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##fixedAltitude", ref _fixedAltitude);
        
        ImGui.Checkbox("Minimal Z threshold:", ref _useMinZThreshold);
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##minZThreshold", ref _minZThreshold);
        
        ImGui.Checkbox("Maximum Z threshold:", ref _useMaxZThreshold);
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##maxZThreshold", ref _maxZThreshold);
        
        ImGui.Separator();
        
        // Overlay options section
        ImGui.Text("Overlay options");
        
        // First row of radio buttons
        bool isElevation = _selectedElevationOption == 0;
        if (ImGui.RadioButton("Elevation", isElevation))
            _selectedElevationOption = 0;
            
        ImGui.SameLine(180);
        bool isAdditions = _selectedOverlayOption == 0;
        if (ImGui.RadioButton("Additions", isAdditions))
            _selectedOverlayOption = 0;
            
        // Second row
        bool isLowering = _selectedElevationOption == 1;
        if (ImGui.RadioButton("Lowering", isLowering))
            _selectedElevationOption = 1;
            
        ImGui.SameLine(180);
        bool isReplacement = _selectedOverlayOption == 1;
        if (ImGui.RadioButton("Replacement", isReplacement))
            _selectedOverlayOption = 1;
            
        // Third row
        bool isRandomAltitude = _selectedElevationOption == 2;
        if (ImGui.RadioButton("Add altitude (random):", isRandomAltitude))
            _selectedElevationOption = 2;
            
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##randomAltitude", ref _randomAltitude);
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is LandObject centerLo)
        {
            // Clear all previous ghosts first
            foreach (var pair in new Dictionary<LandObject, LandObject>(MapManager.GhostLandTiles))
            {
                pair.Key.Reset();
                MapManager.GhostLandTiles.Remove(pair.Key);
            }

            // Get the center coordinates
            int centerX = centerLo.Tile.X;
            int centerY = centerLo.Tile.Y;
            
            // First pass: Calculate and create all ghost tiles
            // IMPORTANT: Add a buffer zone of 1 tile beyond the outerRadius to create proper transitions
            Dictionary<(int, int), LandObject> pendingGhostTiles = new Dictionary<(int, int), LandObject>();

            // Apply to all tiles within the outer radius PLUS ONE buffer tile for smooth transitions
            int extendedRadius = _outerRadius + 1;
            for (int x = centerX - extendedRadius; x <= centerX + extendedRadius; x++)
            {
                for (int y = centerY - extendedRadius; y <= centerY + extendedRadius; y++)
                {
                    // Skip if coordinates are out of bounds
                    if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                        continue;

                    // Calculate distance from center
                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    
                    // Create ghost tiles for the buffer zone too, but don't modify their height
                    bool isBufferZone = distance > _outerRadius && distance <= extendedRadius;
                    
                    // Get the land object at this position
                    LandObject lo = MapManager.LandTiles[x, y];
                    
                    // Skip if we can't modify this tile for some reason
                    if (lo == null)
                        continue;

                    // Calculate height adjustment based on distance (original height for buffer zone)
                    sbyte newZ = isBufferZone ? lo.Tile.Z : CalculateNewZ(lo, distance);
                    
                    // Create ghost tile for preview
                    lo.Visible = false;
                    var newTile = new LandTile(lo.LandTile.Id, lo.Tile.X, lo.Tile.Y, newZ);
                    var ghostTile = new LandObject(newTile);
                    
                    // Store in pending dictionary
                    pendingGhostTiles[(x, y)] = ghostTile;
                    
                    // Add to official ghost tiles
                    MapManager.GhostLandTiles[lo] = ghostTile;
                }
            }

            // Second pass remains the same
            foreach (var kvp in pendingGhostTiles)
            {
                int x = kvp.Key.Item1;
                int y = kvp.Key.Item2;
                LandObject ghostTile = kvp.Value;
                
                ghostTile.UpdateAsGhost(pendingGhostTiles);
            }
        }
    }

    private void CalculateGhostNormals(LandObject ghostTile, int x, int y, Dictionary<(int, int), LandObject> ghostTiles)
    {
        // Use the ghost-aware update method
        ghostTile.UpdateAsGhost(ghostTiles);
    }

    protected override void GhostClear(TileObject? o)
    {
        // Clear all ghosts in the radius (including buffer zone)
        if (o is LandObject centerLo)
        {
            int centerX = centerLo.Tile.X;
            int centerY = centerLo.Tile.Y;
            int extendedRadius = _outerRadius + 1;

            foreach (var pair in new Dictionary<LandObject, LandObject>(MapManager.GhostLandTiles))
            {
                int x = pair.Key.Tile.X;
                int y = pair.Key.Tile.Y;
                
                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                
                if (distance <= extendedRadius)
                {
                    pair.Key.Reset();
                    MapManager.GhostLandTiles.Remove(pair.Key);
                }
            }
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if (o is LandObject centerLo && Random.Next(100) < _chance)
        {
            int centerX = centerLo.Tile.X;
            int centerY = centerLo.Tile.Y;

            // Apply to all tiles within the outer radius
            for (int x = centerX - _outerRadius; x <= centerX + _outerRadius; x++)
            {
                for (int y = centerY - _outerRadius; y <= centerY + _outerRadius; y++)
                {
                    // Skip if coordinates are out of bounds
                    if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                        continue;

                    // Calculate distance from center
                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    
                    // Skip tiles outside the outer radius
                    if (distance > _outerRadius)
                        continue;

                    // Get the land object at this position
                    LandObject lo = MapManager.LandTiles[x, y];
                    
                    // Skip if we can't modify this tile
                    if (lo == null)
                        continue;

                    // Apply the height change if we have a ghost for this tile
                    if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
                    {
                        lo.LandTile.ReplaceLand(lo.LandTile.Id, ghostTile.Tile.Z);
                    }
                }
            }
        }
    }

    private sbyte CalculateNewZ(LandObject lo, double distance)
    {
        sbyte currentZ = lo.Tile.Z;
        
        // Don't modify buffer zone tiles at all - they should just be used for corner calculations
        if (distance > _outerRadius)
            return currentZ;
        
        // Apply min/max thresholds if enabled
        if (_useMinZThreshold && currentZ < _minZThreshold)
            return (sbyte)_minZThreshold;
            
        if (_useMaxZThreshold && currentZ > _maxZThreshold)
            return (sbyte)_maxZThreshold;
            
        // Force fixed altitude if enabled
        if (_useFixedAltitude)
            return (sbyte)_fixedAltitude;
            
        // Calculate interpolation factor based on distance
        float factor = 1.0f;
        
        if (distance <= _innerRadius)
        {
            // Full height change within inner radius
            factor = 1.0f;
        }
        else if (_useAdditionalRadius1 && distance <= _additionalRadius1)
        {
            // Interpolate between inner radius and additional radius 1
            factor = (float)(1.0f - (distance - _innerRadius) / (_additionalRadius1 - _innerRadius));
            factor = factor * _param1 + _param2;
        }
        else if (_useAdditionalRadius2 && distance <= _additionalRadius2)
        {
            // Interpolate between additional radius 1 and 2
            factor = (float)(1.0f - (distance - _additionalRadius1) / (_additionalRadius2 - _additionalRadius1));
            factor = factor * _param2 + _param3;
        }
        else if (distance <= _outerRadius)
        {
            // Interpolate between additional radius 2 (or inner if not used) and outer radius
            float startRadius = _useAdditionalRadius2 ? _additionalRadius2 : 
                               _useAdditionalRadius1 ? _additionalRadius1 : _innerRadius;
            factor = (float)(1.0f - (distance - startRadius) / (_outerRadius - startRadius));
            
            // Only apply param3 when using additional radii, otherwise use direct linear interpolation
            if (_useAdditionalRadius1 || _useAdditionalRadius2)
                factor = factor * _param3;
                
            // IMPORTANT: Create a smooth falloff to zero near the outer edge
            // This creates a natural transition between modified and unmodified terrain
            float edgeFalloff = 0.8f; // Start the falloff at 80% of the way to the outer radius
            if (distance > _outerRadius * edgeFalloff)
            {
                // Apply extra falloff at the edges
                float edgeFactor = (float)((distance - (_outerRadius * edgeFalloff)) / (_outerRadius * (1.0f - edgeFalloff)));
                factor *= (1.0f - edgeFactor);
            }
        }
        
        // Clamp factor between 0 and 1
        factor = Math.Max(0, Math.Min(1, factor));
        
        // Apply selected elevation option with interpolation factor
        sbyte newZ = currentZ;
        switch (_selectedElevationOption)
        {
            case 0: // Elevation
                int elevation = (int)(_heightDepth * factor);
                newZ = (sbyte)Math.Min(currentZ + elevation, 127);
                break;
            case 1: // Lowering
                int lowering = (int)(_heightDepth * factor);
                newZ = (sbyte)Math.Max(currentZ - lowering, -128);
                break;
            case 2: // Random altitude
                if (factor > 0)
                {
                    int range = (int)(_randomAltitude * factor);
                    var randomValue = Random.Next(range * 2) - range;
                    newZ = (sbyte)Math.Clamp(currentZ + randomValue, -128, 127);
                }
                break;
        }
        
        return newZ;
    }
}