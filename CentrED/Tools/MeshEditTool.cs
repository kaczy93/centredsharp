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
    private int _selectedOverlayOption = 0; // 0=Additions, 1=Replacement, 2=Blended

    private void ValidateRadii()
    {
        // Ensure inner radius is at least 1
        if (_innerRadius < 1)
            _innerRadius = 1;
        
        // For Additional Radius 1, if enabled
        if (_useAdditionalRadius1)
        {
            // Ensure it's greater than inner radius
            if (_additionalRadius1 <= _innerRadius)
                _additionalRadius1 = _innerRadius + 1;
        }
        
        // For Additional Radius 2, if enabled
        if (_useAdditionalRadius2)
        {
            // If Additional Radius 1 is also enabled, ensure proper ordering
            if (_useAdditionalRadius1 && _additionalRadius2 <= _additionalRadius1)
                _additionalRadius2 = _additionalRadius1 + 1;
            // Otherwise just ensure it's greater than inner radius
            else if (_additionalRadius2 <= _innerRadius)
                _additionalRadius2 = _innerRadius + 1;
        }
        
        // Ensure outer radius is greater than the largest enabled radius
        int largestEnabledRadius = _innerRadius;
        if (_useAdditionalRadius1)
            largestEnabledRadius = Math.Max(largestEnabledRadius, _additionalRadius1);
        if (_useAdditionalRadius2)
            largestEnabledRadius = Math.Max(largestEnabledRadius, _additionalRadius2);
        
        if (_outerRadius <= largestEnabledRadius)
            _outerRadius = largestEnabledRadius + 1;
    }

    internal override void Draw()
    {
        // Geometry section with a border box
        ImGui.Text("Geometry");
        ImGui.BeginChild("GeometrySection", new System.Numerics.Vector2(-1, 220), ImGuiChildFlags.Border);
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Inner radius r.:");
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        bool innerRadiusChanged = ImGui.InputInt("##innerRadius", ref _innerRadius);
        
        bool additionalRadius1Changed = false;
        ImGui.Checkbox("Add'l radius 1:", ref _useAdditionalRadius1);
        ImGui.SameLine(140);
        ImGui.BeginDisabled(!_useAdditionalRadius1);
        ImGui.SetNextItemWidth(100);
        additionalRadius1Changed = ImGui.InputInt("##addRadius1", ref _additionalRadius1);
        ImGui.EndDisabled();
        
        bool additionalRadius2Changed = false;
        // Allow Additional Radius 2 to be enabled independently
        ImGui.Checkbox("Add'l radius 2:", ref _useAdditionalRadius2);
        ImGui.SameLine(140);
        ImGui.BeginDisabled(!_useAdditionalRadius2);
        ImGui.SetNextItemWidth(100);
        additionalRadius2Changed = ImGui.InputInt("##addRadius2", ref _additionalRadius2);
        ImGui.EndDisabled();
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Outer radius:");
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        bool outerRadiusChanged = ImGui.InputInt("##outerRadius", ref _outerRadius);
        
        // Validate and adjust radii if needed
        if (innerRadiusChanged || additionalRadius1Changed || additionalRadius2Changed || outerRadiusChanged)
            ValidateRadii();
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Height / Depth:");
        ImGui.SameLine(140);
        ImGui.SetNextItemWidth(100);
        ImGui.InputInt("##heightDepth", ref _heightDepth);
        
        // Three parameter fields with conditional enabling based on radius settings
        ImGui.SetNextItemWidth(100);
        ImGui.BeginDisabled(!_useAdditionalRadius1); // Param1 enabled only when radius1 is enabled
        ImGui.InputFloat("##param1", ref _param1, 0.0f, 0.0f, "%.2f");
        ImGui.EndDisabled();
        
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.BeginDisabled(!(_useAdditionalRadius1 || _useAdditionalRadius2)); // Param2 enabled when either radius is enabled
        ImGui.InputFloat("##param2", ref _param2, 0.0f, 0.0f, "%.2f");
        ImGui.EndDisabled();
        
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.BeginDisabled(!_useAdditionalRadius2); // Param3 enabled only when radius2 is enabled
        ImGui.InputFloat("##param3", ref _param3, 0.0f, 0.0f, "%.2f");
        ImGui.EndDisabled();
        
        ImGui.EndChild(); // End the bordered box for Geometry section
        
        ImGui.Spacing();
        
        // Conditions for limitations section with a border box
        ImGui.Text("Conditions for limitations");
        ImGui.BeginChild("ConditionsSection", new System.Numerics.Vector2(-1, 120), ImGuiChildFlags.Border);
        
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
        
        ImGui.EndChild(); // End the bordered box for Conditions section
        
        ImGui.Spacing();
        
        // Overlay options section with a border box
        ImGui.Text("Overlay options");
        ImGui.BeginChild("OverlayOptions", new System.Numerics.Vector2(-1, 150), ImGuiChildFlags.Border);
        
        // First row - Elevation/Lowering radio buttons (mutually exclusive)
        bool isElevation = _selectedElevationOption == 0;
        bool isLowering = _selectedElevationOption == 1;
        
        if (ImGui.RadioButton("Elevation", isElevation))
        {
            _selectedElevationOption = 0;
        }
        ImGui.SameLine(180);
        if (ImGui.RadioButton("Lowering", isLowering))
        {
            _selectedElevationOption = 1;
        }
        
        // Second row - Random altitude checkbox as a completely separate option
        bool useRandomAltitude = _randomAltitude > 0;
        
        // Keep the entire row enabled/disabled together
        if (ImGui.Checkbox("Add altitude (random)", ref useRandomAltitude))
        {
            // If turned on, set to a default value of 5, otherwise set to 0 to disable
            _randomAltitude = useRandomAltitude ? Math.Max(5, _randomAltitude) : 0;
        }
        
        ImGui.SameLine(180);
        ImGui.BeginDisabled(!useRandomAltitude);
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("##randomAltitude", ref _randomAltitude))
        {
            // Ensure _randomAltitude stays positive when enabled
            if (useRandomAltitude && _randomAltitude <= 0)
                _randomAltitude = 1;
        }
        ImGui.EndDisabled();
        
        // Third row - Empty for spacing
        ImGui.Dummy(new System.Numerics.Vector2(0, 10));
        
        // Fourth row - Mode selection (mutually exclusive)
        ImGui.Text("Mode:");
        bool isAdditionsMode = _selectedOverlayOption == 0;
        bool isReplacementMode = _selectedOverlayOption == 1;
        bool isBlendedMode = _selectedOverlayOption == 2;
        
        if (ImGui.RadioButton("Additions", isAdditionsMode))
        {
            _selectedOverlayOption = 0;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Replacement", isReplacementMode))
        {
            _selectedOverlayOption = 1;
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Blended", isBlendedMode))
        {
            _selectedOverlayOption = 2;
        }
        
        ImGui.EndChild(); // End the bordered box for Overlay options
    }

    private sbyte CalculateNewZ(LandObject lo, double distance, sbyte centerZ)
    {
        sbyte currentZ = lo.Tile.Z;
        
        // Don't modify buffer zone tiles at all
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
        
        // Calculate random altitude adjustment if enabled
        int randomAdjustment = _randomAltitude > 0 ? Random.Next(-_randomAltitude, _randomAltitude + 1) : 0;
        
        // Calculate the distance factor (used by all modes)
        float factor = CalculateDistanceFactor(distance);
        
        // ADDITION MODE - add to existing terrain with smooth transition
        if (_selectedOverlayOption == 0) // Additions mode
        {
            if (_selectedElevationOption == 0) // Elevation
            {
                // Add height with smooth transition based on distance
                // Full height at center, gradually diminishing toward edges
                int heightAddition = (int)(_heightDepth * factor);
                return (sbyte)Math.Min(currentZ + heightAddition + randomAdjustment, 127);
            }
            else if (_selectedElevationOption == 1) // Lowering
            {
                // Subtract height with smooth transition based on distance
                // Full depth at center, gradually diminishing toward edges
                int depthAddition = (int)(_heightDepth * factor);
                return (sbyte)Math.Max(currentZ - depthAddition + randomAdjustment, -128);
            }
        }
        
        // REPLACEMENT MODE - smooth hills that respect height constraints
        else if (_selectedOverlayOption == 1) // Replacement mode
        {
            if (_selectedElevationOption == 0) // Elevation
            {
                // Calculate smooth target height based on distance from center
                int smoothTargetHeight = centerZ + (int)(_heightDepth * factor);
                
                // If current terrain is higher than target height at this point, leave it alone
                if (currentZ > smoothTargetHeight)
                    return currentZ;
                
                // Otherwise use the smoothed height based on distance
                return (sbyte)Math.Min(smoothTargetHeight + randomAdjustment, 127);
            }
            else if (_selectedElevationOption == 1) // Lowering
            {
                // Calculate smooth target depth based on distance from center
                int smoothTargetDepth = centerZ - (int)(_heightDepth * factor);
                
                // If current terrain is lower than target depth at this point, leave it alone
                if (currentZ < smoothTargetDepth)
                    return currentZ;
                
                // Otherwise use the smoothed depth based on distance
                return (sbyte)Math.Max(smoothTargetDepth + randomAdjustment, -128);
            }
        }
        
        // BLENDED MODE - smooth transitions without height constraints
        else if (_selectedOverlayOption == 2) // Blended mode
        {
            if (_selectedElevationOption == 0) // Elevation
            {
                int elevation = (int)(_heightDepth * factor);
                return (sbyte)Math.Min(centerZ + elevation + randomAdjustment, 127);
            }
            else if (_selectedElevationOption == 1) // Lowering
            {
                int lowering = (int)(_heightDepth * factor);
                return (sbyte)Math.Max(centerZ - lowering + randomAdjustment, -128);
            }
        }
        
        return currentZ;
    }
    
    private float CalculateDistanceFactor(double distance)
    {
        float factor = 1.0f;
        
        if (distance <= _innerRadius)
        {
            // Full height change within inner radius (flat top of hill)
            factor = 1.0f;
        }
        else if (_useAdditionalRadius1 && distance <= _additionalRadius1)
        {
            // First transition ring - from inner radius to additional radius 1
            // Linear interpolation with custom parameter influence
            float progress = (float)(1.0f - (distance - _innerRadius) / (_additionalRadius1 - _innerRadius));
            
            // For a smooth transition from full height (1.0f) at inner radius to _param1 at additional radius 1
            factor = progress * (1.0f - _param1) + _param1;
        }
        else if (_useAdditionalRadius2 && distance <= _additionalRadius2)
        {
            // Second transition ring - transition from additional radius 1 to additional radius 2
            float startRadius = _useAdditionalRadius1 ? _additionalRadius1 : _innerRadius;
            float startFactor = _useAdditionalRadius1 ? _param1 : 1.0f;
            
            float progress = (float)(1.0f - (distance - startRadius) / (_additionalRadius2 - startRadius));
            
            // Transition from startFactor to _param3
            factor = progress * (startFactor - _param3) + _param3;
        }
        else if (distance <= _outerRadius)
        {
            // Only apply transitions beyond additional radius 2 if it's not enabled
            if (!_useAdditionalRadius2)
            {
                // Final transition ring - determine proper starting point
                float startRadius;
                float startFactor;
                
                if (_useAdditionalRadius1)
                {
                    startRadius = _additionalRadius1;
                    startFactor = _param1;
                }
                else
                {
                    startRadius = _innerRadius;
                    startFactor = 1.0f;
                }
                
                // Linear interpolation from starting height to zero
                float progress = (float)(1.0f - (distance - startRadius) / (_outerRadius - startRadius));
                factor = progress * startFactor;
                
                // Create a smooth falloff to zero near the outer edge
                float edgeFalloff = 0.8f;
                if (distance > _outerRadius * edgeFalloff)
                {
                    float edgeFactor = (float)((distance - (_outerRadius * edgeFalloff)) / (_outerRadius * (1.0f - edgeFalloff)));
                    factor *= (1.0f - edgeFactor);
                }
            }
            else
            {
                // If additional radius 2 is enabled, don't modify terrain outside it
                factor = 0.0f;
            }
        }
        
        // Clamp factor between 0 and 1
        return Math.Max(0, Math.Min(1, factor));
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

            // Get the center coordinates and Z value
            int centerX = centerLo.Tile.X;
            int centerY = centerLo.Tile.Y;
            sbyte centerZ = centerLo.Tile.Z;  // Get center Z for replacement mode
            
            // First pass: Calculate and create all ghost tiles
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
                    sbyte newZ = isBufferZone ? lo.Tile.Z : CalculateNewZ(lo, distance, centerZ);
                    
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
            sbyte centerZ = centerLo.Tile.Z; // Get the center Z value for replacement mode

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
}