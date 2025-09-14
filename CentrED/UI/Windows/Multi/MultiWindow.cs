using System.Text.Json;
using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using CentrED.Network;
using CentrED.UI.Windows.Multi;
using Hexa.NET.ImGui;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI.Windows;

public class MultiWindow : Window
{
    public enum PivotPoint
    {
        None = 0,
        Center = 1,
        TopLeft = 2,
        TopRight = 3,
        BottomLeft = 4,
        BottomRight = 5
    }

    

    private Dictionary<ushort,MultiData> _multies = new ();
    private readonly List<ushort> _multiesIds = new List<ushort>();
    private static readonly Vector2 TilesDimensions = new(32, 32);

    // Placement controls
    private int _placementX = 0;
    private int _placementY = 0;
    private int _placementZ = 0;
    private bool _removeStaticsBeforePlacement = false;

    // Ghost preview
    private bool _showPreview = true;
    private List<StaticObject> _ghostTiles = new List<StaticObject>();
    
    // Pivot control - Default to None
    private PivotPoint _pivotPoint = PivotPoint.None;
    // Add rotation field
    private int _rotation = 0; // 0, 90, 180, 270 degrees

    private TileRotationMapping _rotationMapping = new();
    private string _rotationMappingPath = "TileRotations.json";



    
    // Filtering
    private string _filter = "";
    private List<ushort> _filteredMultiesIds = new List<ushort>();
    private InfoWindow? _infoWindow;

    public MultiWindow()
    {

       
        var p = Path.Combine(ProfileManager.ActiveProfile.ClientPath, "FalodyMultis.json");

        try
        {
            
            if (!Path.Exists(p)) {
                return;
            }
            
            var parsed = JsonSerializer.Deserialize<List<MultiData>>(File.ReadAllText(p));
            foreach (var multiData in parsed)
            {
                _multies.Add((ushort)multiData.Id, multiData);
                _multiesIds.Add((ushort)multiData.Id);
                multiData.CalculateDimensions();
            }
       
            //LoadRotationMapping();

            // Initialize filtered list
            FilterMultis();
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        
        
        
    }
    // private void LoadRotationMapping()
    // {
    //     var rotationPath = Path.Combine(ProfileManager.ActiveProfile.ClientPath, _rotationMappingPath);
    //     try
    //     {
    //         if (File.Exists(rotationPath))
    //         {
    //             var json = File.ReadAllText(rotationPath);
    //             _rotationMapping = JsonSerializer.Deserialize<TileRotationMapping>(json) ?? new TileRotationMapping();
    //             Console.WriteLine($"Loaded {_rotationMapping.Mappings.Count} tile rotation mappings");
    //         }
    //         else
    //         {
    //             Console.WriteLine($"Rotation mapping file not found: {rotationPath}");
    //             // Create empty mapping file as template
    //             SaveRotationMapping();
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Error loading rotation mapping: {ex.Message}");
    //         _rotationMapping = new TileRotationMapping();
    //     }
    // }
    //
    // private void SaveRotationMapping()
    // {
    //     try
    //     {
    //         var rotationPath = Path.Combine(ProfileManager.ActiveProfile.ClientPath, _rotationMappingPath);
    //         var json = JsonSerializer.Serialize(_rotationMapping, new JsonSerializerOptions { WriteIndented = true });
    //         File.WriteAllText(rotationPath, json);
    //         Console.WriteLine("Rotation mapping saved");
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Error saving rotation mapping: {ex.Message}");
    //     }
    // }

        
    public MultiData GetMulti(ushort id)
    {
        return _multies[id];    
    }
    
    public override string Name => "Multi Placer";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

   
    public ushort SelectedId { get; set; }

    private void FilterMultis()
    {
        if (string.IsNullOrEmpty(_filter))
        {
            // No filter - show all multis
            _filteredMultiesIds.Clear();
            _filteredMultiesIds.AddRange(_multiesIds);
        }
        else
        {
            // Filter by name or ID
            var filter = _filter.ToLower();
            _filteredMultiesIds.Clear();
            
            foreach (var id in _multiesIds)
            {
                var multi = _multies[id];
                var name = multi.Name?.ToLower() ?? "";
                
                // Check if filter matches name, ID in decimal, or ID in hex
                if (name.Contains(filter) || 
                    id.ToString().Contains(_filter) || 
                    $"0x{id:x4}".Contains(filter) ||
                    $"{id:X4}".Contains(filter))
                {
                    _filteredMultiesIds.Add(id);
                }
            }
        }
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text("Not connected"u8);
            return;
        }
       
        DrawPlacementControls();
        ImGui.Separator();
        DrawFilterControls();
        DrawMultisList();
        
    }
    
    private (short offsetX, short offsetY) CalculatePivotOffset(MultiData multi)
    {
        if (_pivotPoint == PivotPoint.None || multi.Tiles.Count == 0)
            return (0, 0);

        // Find the bounds of the multi
        var minX = multi.Tiles.Min(t => t.OffsetX);
        var maxX = multi.Tiles.Max(t => t.OffsetX);
        var minY = multi.Tiles.Min(t => t.OffsetY);
        var maxY = multi.Tiles.Max(t => t.OffsetY);

        return _pivotPoint switch
        {
            PivotPoint.Center => ((short)((minX + maxX) / 2), (short)((minY + maxY) / 2)),
            PivotPoint.TopLeft => (minX, minY),
            PivotPoint.TopRight => (maxX, minY),
            PivotPoint.BottomLeft => (minX, maxY),
            PivotPoint.BottomRight => (maxX, maxY),
            _ => (0, 0)
        };
    }

    
    private void DrawFilterControls()
    {
        ImGui.Text("Multi Placement");
        
        // Pivot Point Selection
        ImGui.Text("Pivot Point:");
        var pivotNames = new[] { "None", "Center", "Top Left", "Top Right", "Bottom Left", "Bottom Right" };
        int currentPivot = (int)_pivotPoint;
        
        if (ImGui.Combo("##PivotPoint", ref currentPivot, pivotNames, pivotNames.Length))
        {
            _pivotPoint = (PivotPoint)currentPivot;
            UpdateGhostPreview(); // Update preview when pivot changes
        }
        
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Select where the placement coordinates should be anchored");
        }
        
        ImGui.Spacing();

        
        ImGui.Text("Filter");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("##MultiFilter", ref _filter, 64))
        {
            FilterMultis();
        }
        
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Filter by name or ID (supports hex like '0x1234')");
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            _filter = "";
            FilterMultis();
        }
        
        // Show filter results count
        ImGui.SameLine();
        if (_filteredMultiesIds.Count != _multiesIds.Count)
        {
            ImGui.TextColored(ImGuiColor.Green, 
                              $"Showing {_filteredMultiesIds.Count} of {_multiesIds.Count}");
        }
        else
        {
            ImGui.TextColored(ImGuiColor.Green, 
                              $"{_multiesIds.Count} multis total");
        }
        
        ImGui.Spacing();
    }
    
    private void DrawPlacementControls()
    {
        ImGui.Text("Multi Placement");
        
        // Quick coordinate options section
        ImGui.BeginGroup();
        {
          
            _infoWindow = CEDGame.UIManager.GetWindow<InfoWindow>();
            
            var selectedTile = _infoWindow?.Selected;
            if (selectedTile == null)
            {
                ImGui.SameLine();
                ImGui.TextColored(ImGuiColor.Green, $"InfoWindow needs to be open.");
                
            }
            bool hasSelection = selectedTile != null;
            if (!hasSelection)
            {
                ImGui.BeginDisabled();
            }
            
            if (ImGui.Button("Use Selected Tile"))
            {
                if (selectedTile != null)
                {
                    _placementX = selectedTile.Tile.X;
                    _placementY = selectedTile.Tile.Y;
                    _placementZ = selectedTile.Tile.Z;
                    UpdateGhostPreview();
                }
            }
            
            if (!hasSelection)
            {
                ImGui.EndDisabled();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Select a tile in the editor first");
                }
            }
            else
            {
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Use coordinates from selected tile: ({selectedTile.Tile.X}, {selectedTile.Tile.Y}, {selectedTile.Tile.Z})");
                }
            }
            
            ImGui.SameLine();
            
            // Use Camera Position button
            if (ImGui.Button("Use Camera Center"))
            {
                var cameraPos = CEDGame.MapManager.TilePosition;
                _placementX = cameraPos.X;
                _placementY = cameraPos.Y;
                // Keep current Z or use 0 as default
                if (_placementZ == 0) _placementZ = 0;
                UpdateGhostPreview();
            }
            if (ImGui.IsItemHovered())
            {
                var cameraPos = CEDGame.MapManager.TilePosition;
                ImGui.SetTooltip($"Use camera center coordinates: ({cameraPos.X}, {cameraPos.Y})");
            }
        }
        ImGui.EndGroup();
        
        ImGui.Spacing();
        
        // Coordinate input section with helper buttons
        ImGui.BeginGroup();
        {
            // X coordinate input with +/- buttons
            ImGui.SetNextItemWidth(80);
            if (ImGui.InputInt("X", ref _placementX))
            {
                UpdateGhostPreview();
            }
             ImGui.SameLine();
            
            // Y coordinate input with +/- buttons
            ImGui.SetNextItemWidth(80);
            if (ImGui.InputInt("Y", ref _placementY))
            {
                UpdateGhostPreview();
            }
            ImGui.SameLine();
            
            // Z coordinate input with +/- buttons
            ImGui.SetNextItemWidth(80);
            if (ImGui.InputInt("Z", ref _placementZ))
            {
                UpdateGhostPreview();
            }
            
            ImGui.SameLine();
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(80);

        }
        ImGui.EndGroup();
        
        ImGui.Spacing();
        
        // Show current coordinates and selection info
        ImGui.BeginGroup();
        {
            ImGui.Checkbox("Remove existing statics", ref _removeStaticsBeforePlacement);
            ImGuiEx.Tooltip("Remove all static tiles from the area before placing the multi");

            
            ImGui.TextColored(ImGuiColor.Green, $"Target: ({_placementX}, {_placementY}, {_placementZ})");
            var selectedTile = _infoWindow?.Selected;
            bool hasSelection = selectedTile != null;
            
            if (hasSelection)
            {
                ImGui.SameLine();
                ImGui.TextColored(ImGuiColor.Green, $"Selected: ({selectedTile.Tile.X}, {selectedTile.Tile.Y}, {selectedTile.Tile.Z})");
            }
            
            var cameraPos = CEDGame.MapManager.TilePosition;
            ImGui.TextColored(ImGuiColor.Green, $"Camera: ({cameraPos.X}, {cameraPos.Y})");
        }
        ImGui.EndGroup();
        
        ImGui.Spacing();
        
        // Offset controls for quick adjustments
      
        
        
        // Show Preview checkbox
        if (ImGui.Checkbox("Show Preview", ref _showPreview))
        {
            UpdateGhostPreview();
        }
        
        ImGui.Spacing();
        
        // Place Multi button
        bool hasSelectedMulti = SelectedId != 0 && _multies.ContainsKey(SelectedId);
        if (!hasSelectedMulti)
        {
            ImGui.BeginDisabled();
        }
        
        if (ImGui.Button("Place Multi", new Vector2(150, 0)))
        {
            PlaceSelectedMulti();
        }
        
        if (!hasSelectedMulti)
        {
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Select a multi first");
            }
        }
        
        if (hasSelectedMulti)
        {
            var selectedMulti = _multies[SelectedId];
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColor.Green, $"Selected: {selectedMulti.Name} (0x{SelectedId:X4})");
        }
    }
    
    // Add rotation transformation method
    private (short x, short y) ApplyRotation(short offsetX, short offsetY, int rotation)
    {
        return rotation switch
        {
            0 => (offsetX, offsetY),                 // 0°
            1 => ((short)-offsetY, offsetX),         // 90° clockwise
            2 => ((short)-offsetX, (short)-offsetY), // 180°
            3 => (offsetY, (short)-offsetX),         // 270° clockwise (90° counter-clockwise)
            _ => (offsetX, offsetY)
        };
    }

    
    private void ClearGhostPreview()
    {
        // // Remove ghost tiles from MapManager
        // foreach (var ghostTile in _ghostTiles)
        // {
        //     // Find and remove from GhostStaticTiles dictionary
        //     var keysToRemove = new List<TileObject>();
        //     foreach (var kvp in CEDGame.MapManager.StaticsManager.GhostTiles)
        //     {
        //         if (kvp.Value == ghostTile)
        //         {
        //             keysToRemove.Add(kvp.Key);
        //         }
        //     }
        //
        //     foreach (var key in keysToRemove)
        //     {
        //         CEDGame.MapManager.GhostStaticTiles.Remove(key);
        //     }
        // }

        _ghostTiles.Clear();
    }
    
// Add method to get rotated tile ID
    private ushort GetRotatedTileId(ushort originalId, int rotation)
    {
        if (rotation == 0 || !_rotationMapping.Mappings.ContainsKey(originalId))
            return originalId;
            
        return _rotationMapping.Mappings[originalId].GetRotatedId(rotation);
    }
    
    private void UpdateGhostPreview()
    {
        // Clear existing ghost tiles
        ClearGhostPreview();

        if (!_showPreview || SelectedId == 0 || !_multies.ContainsKey(SelectedId))
            return;
        
        var multi = _multies[SelectedId];
        var (pivotOffsetX, pivotOffsetY) = CalculatePivotOffsetWithRotation(multi, _rotation);

        foreach (var multiItem in multi.Tiles)
        {
            // Apply coordinate rotation
            var (rotatedX, rotatedY) = ApplyRotation(multiItem.OffsetX, multiItem.OffsetY, _rotation);
            
            // Get the art-rotated tile ID
            var rotatedTileId = GetRotatedTileId(multiItem.ItemId, _rotation);
            
            var finalX = _placementX - pivotOffsetX + rotatedX;
            var finalY = _placementY - pivotOffsetY + rotatedY;
            var finalZ = (sbyte)(_placementZ + multiItem.OffsetZ);
            
            if (finalX < 0 || finalY < 0)
                continue;
            
            var ghostTile = new StaticTile(
                rotatedTileId, // Use the art-rotated ID
                (ushort)finalX,
                (ushort)finalY,
                finalZ,
                0
            );
            
            var ghostObject = new StaticObject(ghostTile) { Alpha = 0.5f };
            _ghostTiles.Add(ghostObject);

            var land = CEDGame.MapManager.LandTiles[finalX, finalY];
            if (land != null)
            {
                CEDGame.MapManager.StaticsManager.AddGhost(land, ghostObject);
            }
        }
    }

    private void PlaceSelectedMulti()
    {
        if (SelectedId == 0 || !_multies.ContainsKey(SelectedId))
            return;
            
        ClearGhostPreview();
        
        var multi = GetMulti(SelectedId);
        if (multi?.Tiles == null) return;

        try
        {
            CEDClient.BeginUndoGroup();
            
        if (_removeStaticsBeforePlacement)
        {
            RemoveExistingStaticsInArea(multi);
        }

     
            
            var (pivotOffsetX, pivotOffsetY) = CalculatePivotOffsetWithRotation(multi, _rotation);
            
            int placedTiles = 0;
            int skippedTiles = 0;
            
            foreach (var multiItem in multi.Tiles)
            {
                // Apply coordinate rotation
                var (rotatedX, rotatedY) = ApplyRotation(multiItem.OffsetX, multiItem.OffsetY, _rotation);
                
                // Get the art-rotated tile ID
                var rotatedTileId = GetRotatedTileId(multiItem.ItemId, _rotation);
                
                var finalX = _placementX - pivotOffsetX + rotatedX;
                var finalY = _placementY - pivotOffsetY + rotatedY;
                var finalZ = (sbyte)(_placementZ + multiItem.OffsetZ);
                
                if (finalX < 0 || finalY < 0)
                {
                    skippedTiles++;
                    continue;
                }
                
                var staticTile = new StaticTile(
                    rotatedTileId, // Use the art-rotated ID
                    (ushort)finalX,
                    (ushort)finalY,
                    finalZ,
                    0
                );
                
                CEDClient.Add(staticTile);
                placedTiles++;
            }
            
            CEDClient.EndUndoGroup();
            
            var rotationText = _rotation == 0 ? "" : $" (rotated {_rotation * 90}°)";
            Console.WriteLine($"Placed multi '{multi.Name}'{rotationText} with {placedTiles} tiles at ({_placementX}, {_placementY}, {_placementZ}) using {_pivotPoint} pivot");
            
            if (_showPreview)
            {
                UpdateGhostPreview();
            }
        }
        catch (Exception ex)
        {
            CEDClient.EndUndoGroup();
            Console.WriteLine($"Error placing multi: {ex.Message}");
        }
    }

 
    private (short offsetX, short offsetY) CalculatePivotOffsetWithRotation(MultiData multi, int rotation)
    {
        if (_pivotPoint == PivotPoint.None || multi.Tiles.Count == 0)
            return (0, 0);

        // Apply rotation to all tile positions first, then find bounds
        var rotatedPositions = multi.Tiles.Select(t => ApplyRotation(t.OffsetX, t.OffsetY, rotation)).ToList();

        // Find the bounds of the rotated multi
        var minX = rotatedPositions.Min(pos => pos.x);
        var maxX = rotatedPositions.Max(pos => pos.x);
        var minY = rotatedPositions.Min(pos => pos.y);
        var maxY = rotatedPositions.Max(pos => pos.y);

        return _pivotPoint switch
        {
            PivotPoint.Center => ((short)((minX + maxX) / 2), (short)((minY + maxY) / 2)),
            PivotPoint.TopLeft => (minX, minY),
            PivotPoint.TopRight => (maxX, minY),
            PivotPoint.BottomLeft => (minX, maxY),
            PivotPoint.BottomRight => (maxX, maxY),
            _ => (0, 0)
        };
    }

  
    private RectU16 CalculateMultiBounds(MultiData multi)
    {
        // Calculate the bounds based on the multi's components with rotation applied
        var (pivotOffsetX, pivotOffsetY) = CalculatePivotOffsetWithRotation(multi, _rotation);
        
        var placementX = _placementX;
        var placementY = _placementY;
        
        // Apply rotation to all tiles and find the actual bounds
        var rotatedPositions = multi.Tiles.Select(t => 
        {
            var (rotatedX, rotatedY) = ApplyRotation(t.OffsetX, t.OffsetY, _rotation);
            return (x: placementX - pivotOffsetX + rotatedX, y: placementY - pivotOffsetY + rotatedY);
        }).ToList();
        
        var minX = rotatedPositions.Min(pos => pos.x);
        var maxX = rotatedPositions.Max(pos => pos.x);
        var minY = rotatedPositions.Min(pos => pos.y);
        var maxY = rotatedPositions.Max(pos => pos.y);
        
        return new RectU16((ushort)minX, (ushort)minY, (ushort)maxX, (ushort)maxY);
    }


private void RemoveExistingStaticsInArea(MultiData multi)
{
    // // Get the bounds of the multi
    // var bounds = CalculateMultiBounds(multi);
    //     
    //
    // // Remove all static tiles within the bounds
    // for (ushort x = bounds.X1; x <= bounds.X2; x++)
    // {
    //     for (ushort y = bounds.Y1; y <= bounds.Y2; y++)
    //     {
    //         var staticTiles = CEDGame.MapManager.StaticTiles[x, y];
    //         if (staticTiles != null)
    //         {
    //             // Create a copy of the list to avoid modification during iteration
    //             var tilesToRemove = new List<StaticObject>(staticTiles);
    //             foreach (var staticTile in tilesToRemove)
    //             {
    //                 CEDClient.Remove(staticTile.StaticTile);
    //             }
    //         }
    //     }
    // }
}



    protected void OnSelectionChanged()
    {
        // Update ghost preview when selection changes
        UpdateGhostPreview();
    }
    

    
   private void DrawMultisList()
    {
        if (ImGui.BeginChild("Multis", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("MultisTable", 3) && CEDClient.Running)
            {
                unsafe
                {
                    var clipper = ImGui.ImGuiListClipper();
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 60 );

                    // Use filtered list instead of all multis
                    var ids = _filteredMultiesIds;
                    clipper.Begin(ids.Count,  60 + ImGui.GetStyle().ItemSpacing.Y);
                    while (clipper.Step())
                    {
                        for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            ushort multiIndex = ids[rowIndex];
                            var multiInfo = _multies[multiIndex];
                            var posY = ImGui.GetCursorPosY();
                            //DrawTileRow(tileIndex, tileInfo);
                            DrawMultiRow(multiIndex,  multiInfo);
                            ImGui.SetCursorPosY(posY);
                            if (ImGui.Selectable
                                (
                                    $"##multi{multiInfo.Id}",
                                    multiIndex == SelectedId,
                                    ImGuiSelectableFlags.SpanAllColumns,
                                    new Vector2(0, 60)
                                ))
                            {
                                SelectedId = multiIndex;
                                UpdateGhostPreview(); // Update preview when selection changes
                            }
                            //DrawTooltip(tileInfo);
                            // if (ImGui.BeginPopupContextItem())
                            // {
                            //     if (_tileSetIndex != 0 && ImGui.Button("Add to set"))
                            //     {
                            //         AddToTileSet((ushort)tileIndex);
                            //         ImGui.CloseCurrentPopup();
                            //     }
                            //     if (StaticMode)
                            //     {
                            //         if (ImGui.Button("Filter"))
                            //         {
                            //             CEDGame.MapManager.StaticFilterIds.Add(tileIndex);
                            //             ImGui.CloseCurrentPopup();
                            //         }
                            //     }
                            //     ImGui.EndPopup();
                            // }
                            // if (ImGui.BeginDragDropSource())
                            // {
                            //     ImGui.SetDragDropPayload
                            //     (
                            //         LandMode ? Land_DragDrop_Target_Type : Static_DragDrop_Target_Type,
                            //         &tileIndex,
                            //         sizeof(int)
                            //     );
                            //     ImGui.Text(tileInfo.Name);
                            //     CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds);
                            //     ImGui.EndDragDropSource();
                            // }
                        }
                    }
                    clipper.End();
                    
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }
  
    private void DrawMultiRow(int index, MultiData tileInfo)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None, TilesDimensions.Y);
        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY
                (ImGui.GetCursorPosY() + (TilesDimensions.Y - ImGui.GetFontSize()) / 2); //center vertically
            ImGui.Text($"0x{index:X4}");
        }

        if (ImGui.TableNextColumn())
        {
            
                // if (!CEDGame.UIManager.DrawImage(tileInfo.Texture, tileInfo.Bounds, TilesDimensions, LandMode) &&
                //     CEDGame.MapManager.DebugLogging)
                // {
                //     Console.WriteLine($"[TilesWindow] No texture found for tile 0x{index:X4}");
                // }
            
        }

        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (TilesDimensions.Y - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(tileInfo.Name);
        }
    }

}
