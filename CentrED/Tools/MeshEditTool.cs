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

    // Layout constants
    private const float LABEL_WIDTH = 140f;
    private const float INPUT_WIDTH = 100f;
    private const float SLIDER_WIDTH = 232f;

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
    
    // Slider values for parameter distribution
    private float _slider1Value = 0.0f; // Controls distribution between _param1 and _param2
    private float _slider2Value = 0.0f; // Controls distribution between _param2 and _param3

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
        if (_useAdditionalRadius1 && _additionalRadius1 <= _innerRadius)
            _additionalRadius1 = _innerRadius + 1;
        
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

    private void NormalizeParams()
    {
        // Calculate current sum
        float sum = _param1 + _param2 + _param3;
        
        // If sum is already very close to 1.0, don't adjust
        if (Math.Abs(sum - 1.0f) < 0.001f)
            return;
        
        // Handle case where all params are 0 (to avoid division by zero)
        if (sum < 0.001f)
        {
            SetDefaultParameterValues();
            return;
        }
        
        // Scale all parameters proportionally
        _param1 = Math.Max(0.0f, _param1 / sum);
        _param2 = Math.Max(0.0f, _param2 / sum);
        _param3 = Math.Max(0.0f, _param3 / sum);
        
        // Final adjustment to ensure exactly 1.0
        sum = _param1 + _param2 + _param3;
        float diff = 1.0f - sum;
        
        // Add the difference to the largest parameter to minimize visual disruption
        if (_param1 >= _param2 && _param1 >= _param3)
            _param1 += diff;
        else if (_param2 >= _param1 && _param2 >= _param3)
            _param2 += diff;
        else
            _param3 += diff;
            
        // Update slider values based on normalized parameters
        UpdateSliderValues();
    }

    private void SetDefaultParameterValues()
    {
        // Set default values based on which radius options are enabled
        if (_useAdditionalRadius1 && !_useAdditionalRadius2)
        {
            _param1 = 0.0f;
            _param2 = 1.0f;
            _param3 = 0.0f;
            _slider1Value = 0.0f;
        }
        else if (!_useAdditionalRadius1 && _useAdditionalRadius2)
        {
            _param1 = 0.0f;
            _param2 = 0.0f; 
            _param3 = 1.0f;
            _slider2Value = 0.0f;
        }
        else if (_useAdditionalRadius1 && _useAdditionalRadius2)
        {
            _param1 = 0.0f;
            _param2 = 0.5f;
            _param3 = 0.5f;
            _slider1Value = 0.0f;
            _slider2Value = 0.5f;
        }
        else
        {
            // Default values when nothing is enabled
            _param1 = 0.0f;
            _param2 = 1.0f;
            _param3 = 0.0f;
            _slider1Value = 0.0f;
            _slider2Value = 1.0f;
        }
    }
    
    // Helper method to update slider values based on parameter values
    private void UpdateSliderValues()
    {
        // Update slider1 position based on the ratio between _param1 and (_param1 + _param2)
        _slider1Value = (_param1 + _param2 > 0.001f) ? _param1 / (_param1 + _param2) : 0.0f;
            
        // Update slider2 position based on the ratio between _param2 and (_param2 + _param3)
        _slider2Value = (_param2 + _param3 > 0.001f) ? _param2 / (_param2 + _param3) : 1.0f;
    }
    
    // Helper method to update parameters based on slider values
    private void UpdateParamsFromSlider1()
    {
        if (_useAdditionalRadius1)
        {
            // Remember how much of the total 1.0 is allocated to _param3
            float param3Share = _param3;
            
            // Distribute the remaining (1.0 - _param3) between _param1 and _param2
            // based on slider1's position
            float remainingShare = 1.0f - param3Share;
            _param1 = remainingShare * _slider1Value;
            _param2 = remainingShare * (1.0f - _slider1Value);
            
            // Keep _param3 unchanged
            _param3 = param3Share;
            
            // Ensure parameters are normalized to exactly 1.0 total
            NormalizeParams();
        }
    }

    private void UpdateParamsFromSlider2()
    {
        if (_useAdditionalRadius2)
        {
            // Remember how much of the total 1.0 is allocated to _param1
            float param1Share = _param1;
            
            // Distribute the remaining (1.0 - _param1) between _param2 and _param3
            // based on slider2's position
            float remainingShare = 1.0f - param1Share;
            _param2 = remainingShare * _slider2Value;
            _param3 = remainingShare * (1.0f - _slider2Value);
            
            // Keep _param1 unchanged
            _param1 = param1Share;
            
            // Ensure parameters are normalized to exactly 1.0 total
            NormalizeParams();
        }
    }

    // Reset parameters when checkboxes change
    private void ResetParamsOnCheckboxChange()
    {
        // When neither checkbox is enabled, reset to default values
        if (!_useAdditionalRadius1 && !_useAdditionalRadius2)
        {
            _param1 = 0.0f;
            _param2 = 1.0f;
            _param3 = 0.0f;
            _slider1Value = 0.0f;
            _slider2Value = 1.0f;
            return;
        }
        
        // When only additional radius 1 is enabled
        if (_useAdditionalRadius1 && !_useAdditionalRadius2)
        {
            // Move any value from _param3 into _param2
            _param2 = _param2 + _param3;
            _param3 = 0.0f;
            
            // Now normalize to ensure sum is 1.0
            NormalizeParams();
            return;
        }
        
        // When only additional radius 2 is enabled
        if (!_useAdditionalRadius1 && _useAdditionalRadius2)
        {
            // Move any value from _param1 into _param2
            _param2 = _param2 + _param1;
            _param1 = 0.0f;
            
            // Now normalize to ensure sum is 1.0
            NormalizeParams();
            return;
        }
        
        // When both are enabled, just normalize
        NormalizeParams();
    }

    // Handle a parameter being set to exactly 1.0
    private void HandleParamSetToOne(int paramIndex)
    {
        switch (paramIndex)
        {
            case 1:
                _param1 = 1.0f;
                _param2 = 0.0f;
                _param3 = 0.0f;
                break;
            case 2:
                _param1 = 0.0f;
                _param2 = 1.0f;
                _param3 = 0.0f;
                break;
            case 3:
                _param1 = 0.0f;
                _param2 = 0.0f;
                _param3 = 1.0f;
                break;
        }
        UpdateSliderValues();
    }

    // Create input field with label
    private bool LabeledIntInput(string label, ref int value, float labelWidth = LABEL_WIDTH, float inputWidth = INPUT_WIDTH)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(inputWidth);
        return ImGui.InputInt($"##{label.Replace(" ", "")}", ref value);
    }

    // Create disabled input field with label and checkbox
    private bool CheckboxIntInput(string label, ref bool enabled, ref int value, float labelWidth = LABEL_WIDTH, float inputWidth = INPUT_WIDTH)
    {
        bool prevEnabled = enabled;
        ImGui.Checkbox(label, ref enabled);
        
        if (prevEnabled != enabled)
            ResetParamsOnCheckboxChange();
            
        ImGui.SameLine(labelWidth);
        ImGui.BeginDisabled(!enabled);
        ImGui.SetNextItemWidth(inputWidth);
        bool changed = ImGui.InputInt($"##{label.Replace(" ", "")}", ref value);
        ImGui.EndDisabled();
        
        return changed;
    }

    // Create a slider with proper positioning and width
    private bool ParameterSlider(string id, ref float value, bool enabled, float width = SLIDER_WIDTH)
    {
        ImGui.BeginDisabled(!enabled);
        ImGui.SetNextItemWidth(width);
        bool changed = ImGui.SliderFloat(id, ref value, 0.0f, 1.0f, "");
        ImGui.EndDisabled();
        return changed;
    }

    internal override void Draw()
    {
        DrawGeometrySection();
        ImGui.Spacing();
        DrawConditionsSection();
        ImGui.Spacing();
        DrawOverlayOptionsSection();
    }

    private void DrawGeometrySection()
    {
        ImGui.Text("Geometry");
        ImGui.BeginChild("GeometrySection", new System.Numerics.Vector2(-1, 240), ImGuiChildFlags.Border);
        
        // Inner radius
        if (LabeledIntInput("Inner radius r.:", ref _innerRadius))
            ValidateRadii();
        
        // Additional radius 1
        bool additionalRadius1Changed = CheckboxIntInput("Add'l radius 1:", ref _useAdditionalRadius1, ref _additionalRadius1);
        
        // Slider for param1/param2 distribution
        if (ParameterSlider("##slider1", ref _slider1Value, _useAdditionalRadius1))
            UpdateParamsFromSlider1();
        
        // Additional radius 2
        bool additionalRadius2Changed = CheckboxIntInput("Add'l radius 2:", ref _useAdditionalRadius2, ref _additionalRadius2);
        
        // Slider for param2/param3 distribution
        if (ParameterSlider("##slider2", ref _slider2Value, _useAdditionalRadius2))
            UpdateParamsFromSlider2();
        
        // Outer radius
        if (LabeledIntInput("Outer radius:", ref _outerRadius))
            ValidateRadii();
            
        // Removed redundant ValidateRadii() call since CheckboxIntInput already handles validation when needed
        
        // Height/Depth
        LabeledIntInput("Height / Depth:", ref _heightDepth);
        
        // Parameter fields
        ImGui.Text("Parameters:");
        ImGui.SameLine(LABEL_WIDTH);
        
        // Param1
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        ImGui.BeginDisabled(!_useAdditionalRadius1);
        if (ImGui.InputFloat("##param1", ref _param1, 0.0f, 0.0f, "%.2f"))
        {
            if (_param1 == 1.0f)
                HandleParamSetToOne(1);
            else
                NormalizeParams();
        }
        ImGui.EndDisabled();
        
        // Param2
        ImGui.SameLine();
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        ImGui.BeginDisabled(!(_useAdditionalRadius1 || _useAdditionalRadius2));
        if (ImGui.InputFloat("##param2", ref _param2, 0.0f, 0.0f, "%.2f"))
        {
            if (_param2 == 1.0f)
                HandleParamSetToOne(2);
            else
                NormalizeParams();
        }
        ImGui.EndDisabled();
        
        // Param3
        ImGui.SameLine();
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        ImGui.BeginDisabled(!_useAdditionalRadius2);
        if (ImGui.InputFloat("##param3", ref _param3, 0.0f, 0.0f, "%.2f"))
        {
            if (_param3 == 1.0f)
                HandleParamSetToOne(3);
            else
                NormalizeParams();
        }
        ImGui.EndDisabled();
        
        ImGui.EndChild();
    }

    private void DrawConditionsSection()
    {
        ImGui.Text("Conditions for limitations");
        ImGui.BeginChild("ConditionsSection", new System.Numerics.Vector2(-1, 120), ImGuiChildFlags.Border);
        
        // Fixed altitude
        ImGui.Checkbox("Force fixed altitude:", ref _useFixedAltitude);
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        ImGui.InputInt("##fixedAltitude", ref _fixedAltitude);
        
        // Min Z threshold
        ImGui.Checkbox("Minimal Z threshold:", ref _useMinZThreshold);
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        ImGui.InputInt("##minZThreshold", ref _minZThreshold);
        
        // Max Z threshold
        ImGui.Checkbox("Maximum Z threshold:", ref _useMaxZThreshold);
        ImGui.SameLine(185);
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        ImGui.InputInt("##maxZThreshold", ref _maxZThreshold);
        
        ImGui.EndChild();
    }

    private void DrawOverlayOptionsSection()
    {
        ImGui.Text("Overlay options");
        ImGui.BeginChild("OverlayOptions", new System.Numerics.Vector2(-1, 150), ImGuiChildFlags.Border);
        
        // Elevation/Lowering radio buttons
        bool isElevation = _selectedElevationOption == 0;
        bool isLowering = _selectedElevationOption == 1;
        
        if (ImGui.RadioButton("Elevation", isElevation))
            _selectedElevationOption = 0;
            
        ImGui.SameLine(180);
        if (ImGui.RadioButton("Lowering", isLowering))
            _selectedElevationOption = 1;
        
        // Random altitude
        bool useRandomAltitude = _randomAltitude > 0;
        if (ImGui.Checkbox("Add altitude (random)", ref useRandomAltitude))
            _randomAltitude = useRandomAltitude ? Math.Max(5, _randomAltitude) : 0;
        
        ImGui.SameLine(180);
        ImGui.BeginDisabled(!useRandomAltitude);
        ImGui.SetNextItemWidth(INPUT_WIDTH);
        if (ImGui.InputInt("##randomAltitude", ref _randomAltitude) && useRandomAltitude && _randomAltitude <= 0)
            _randomAltitude = 1;
        ImGui.EndDisabled();
        
        // Spacing
        ImGui.Dummy(new System.Numerics.Vector2(0, 10));
        
        // Mode selection
        ImGui.Text("Mode:");
        if (ImGui.RadioButton("Additions", _selectedOverlayOption == 0))
            _selectedOverlayOption = 0;
            
        ImGui.SameLine();
        if (ImGui.RadioButton("Replacement", _selectedOverlayOption == 1))
            _selectedOverlayOption = 1;
            
        ImGui.SameLine();
        if (ImGui.RadioButton("Blended", _selectedOverlayOption == 2))
            _selectedOverlayOption = 2;
        
        ImGui.EndChild();
    }

    private float CalculateDistanceFactor(double distance)
    {
        // Handle each distance range separately
        if (distance <= _innerRadius)
            return 1.0f; // Full height within inner radius (flat top)
            
        else if (_useAdditionalRadius1 && distance <= _additionalRadius1)
        {
            // First transition ring
            float progress = (float)(1.0f - (distance - _innerRadius) / (_additionalRadius1 - _innerRadius));
            return progress * (1.0f - _param2) + _param2;
        }
        
        else if (_useAdditionalRadius2 && distance <= _additionalRadius2)
        {
            // Second transition ring
            float startRadius = _useAdditionalRadius1 ? _additionalRadius1 : _innerRadius;
            float startFactor = _useAdditionalRadius1 ? _param2 : 1.0f;
            float progress = (float)(1.0f - (distance - startRadius) / (_additionalRadius2 - startRadius));
            return progress * (startFactor - _param3) + _param3;
        }
        
        else if (distance <= _outerRadius && !_useAdditionalRadius2)
        {
            // Final transition ring
            float startRadius = _useAdditionalRadius1 ? _additionalRadius1 : _innerRadius;
            float startFactor = _useAdditionalRadius1 ? _param2 : 1.0f;
            float progress = (float)(1.0f - (distance - startRadius) / (_outerRadius - startRadius));
            
            // Apply smooth falloff near the edge
            if (distance > _outerRadius * 0.8f)
            {
                float edgeFactor = (float)((distance - (_outerRadius * 0.8f)) / (_outerRadius * 0.2f));
                progress *= (1.0f - edgeFactor);
            }
            
            return progress * startFactor;
        }
        
        // Default - outside all radii or additional radius 2 is enabled
        return 0.0f;
    }

    private sbyte CalculateNewZ(LandObject lo, double distance, sbyte centerZ)
    {
        sbyte currentZ = lo.Tile.Z;
        
        // Don't modify tiles outside outer radius
        if (distance > _outerRadius)
            return currentZ;
        
        // Apply thresholds if enabled
        if (_useMinZThreshold && currentZ < _minZThreshold)
            return (sbyte)_minZThreshold;
            
        if (_useMaxZThreshold && currentZ > _maxZThreshold)
            return (sbyte)_maxZThreshold;
            
        // Force fixed altitude if enabled
        if (_useFixedAltitude)
            return (sbyte)_fixedAltitude;
        
        // Get random adjustment if enabled
        int randomAdjustment = _randomAltitude > 0 ? Random.Next(-_randomAltitude, _randomAltitude + 1) : 0;
        
        // Calculate the distance factor
        float factor = CalculateDistanceFactor(distance);
        
        // Apply appropriate mode logic
        return CalculateZForMode(currentZ, centerZ, factor, randomAdjustment);
    }

    private sbyte CalculateZForMode(sbyte currentZ, sbyte centerZ, float factor, int randomAdjustment)
    {
        // Additions mode - modify current terrain
        if (_selectedOverlayOption == 0)
        {
            if (_selectedElevationOption == 0) // Elevation
            {
                int heightAddition = (int)(_heightDepth * factor);
                return (sbyte)Math.Min(currentZ + heightAddition + randomAdjustment, 127);
            }
            else // Lowering
            {
                int depthAddition = (int)(_heightDepth * factor);
                return (sbyte)Math.Max(currentZ - depthAddition + randomAdjustment, -128);
            }
        }
        
        // Replacement mode - respect existing terrain constraints
        else if (_selectedOverlayOption == 1)
        {
            if (_selectedElevationOption == 0) // Elevation
            {
                int targetHeight = centerZ + (int)(_heightDepth * factor);
                return currentZ > targetHeight ? 
                    currentZ : 
                    (sbyte)Math.Min(targetHeight + randomAdjustment, 127);
            }
            else // Lowering
            {
                int targetDepth = centerZ - (int)(_heightDepth * factor);
                return currentZ < targetDepth ? 
                    currentZ : 
                    (sbyte)Math.Max(targetDepth + randomAdjustment, -128);
            }
        }
        
        // Blended mode - ignore existing terrain
        else
        {
            if (_selectedElevationOption == 0) // Elevation
                return (sbyte)Math.Min(centerZ + (int)(_heightDepth * factor) + randomAdjustment, 127);
            else // Lowering
                return (sbyte)Math.Max(centerZ - (int)(_heightDepth * factor) + randomAdjustment, -128);
        }
    }

    protected override void GhostApply(TileObject? o)
    {
        if (o is not LandObject centerLo)
            return;
            
        // Clear all previous ghosts
        ClearAllGhosts();
        
        // Get center coordinates and Z value
        int centerX = centerLo.Tile.X;
        int centerY = centerLo.Tile.Y;
        sbyte centerZ = centerLo.Tile.Z;
        
        // Generate ghost tiles
        Dictionary<(int, int), LandObject> pendingGhostTiles = CreateGhostTiles(centerX, centerY, centerZ);
        
        // Update all tiles for visual preview
        foreach (var ghostTile in pendingGhostTiles.Values)
            MapManager.OnLandTileElevated(ghostTile.LandTile, ghostTile.LandTile.Z);
    }

    private void ClearAllGhosts()
    {
        foreach (var pair in new Dictionary<LandObject, LandObject>(MapManager.GhostLandTiles))
        {
            pair.Key.Reset();
            MapManager.GhostLandTiles.Remove(pair.Key);
        }
    }

    private Dictionary<(int, int), LandObject> CreateGhostTiles(int centerX, int centerY, sbyte centerZ)
    {
        Dictionary<(int, int), LandObject> pendingGhostTiles = new();
        int extendedRadius = _outerRadius + 1;
        
        for (int x = centerX - extendedRadius; x <= centerX + extendedRadius; x++)
        {
            for (int y = centerY - extendedRadius; y <= centerY + extendedRadius; y++)
            {
                // Skip if out of bounds
                if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                    continue;

                // Calculate distance from center
                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                bool isBufferZone = distance > _outerRadius && distance <= extendedRadius;
                
                // Get land object
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null)
                    continue;

                // Create ghost tile
                sbyte newZ = isBufferZone ? lo.Tile.Z : CalculateNewZ(lo, distance, centerZ);
                lo.Visible = false;
                var newTile = new LandTile(lo.LandTile.Id, lo.Tile.X, lo.Tile.Y, newZ);
                var ghostTile = new LandObject(newTile);
                
                // Store the ghost tile
                pendingGhostTiles[(x, y)] = ghostTile;
                MapManager.GhostLandTiles[lo] = ghostTile;
            }
        }
        
        return pendingGhostTiles;
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is not LandObject centerLo)
            return;

        int centerX = centerLo.Tile.X;
        int centerY = centerLo.Tile.Y;
        int extendedRadius = _outerRadius + 1;

        // Clear ghosts within radius
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

    protected override void InternalApply(TileObject? o)
    {
        if (o is not LandObject centerLo || Random.Next(100) >= _chance)
            return;

        int centerX = centerLo.Tile.X;
        int centerY = centerLo.Tile.Y;
        // Removed unused centerZ variable that was causing the warning

        // Apply to all tiles within outer radius
        for (int x = centerX - _outerRadius; x <= centerX + _outerRadius; x++)
        {
            for (int y = centerY - _outerRadius; y <= centerY + _outerRadius; y++)
            {
                // Skip if out of bounds
                if (!Client.IsValidX((ushort)x) || !Client.IsValidY((ushort)y))
                    continue;

                // Calculate distance
                double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                if (distance > _outerRadius)
                    continue;

                // Get land object
                LandObject? lo = MapManager.LandTiles[x, y];
                if (lo == null)
                    continue;

                // Apply height change if ghost exists
                if (MapManager.GhostLandTiles.TryGetValue(lo, out var ghostTile))
                    lo.LandTile.ReplaceLand(lo.LandTile.Id, ghostTile.Tile.Z);
            }
        }
    }
}