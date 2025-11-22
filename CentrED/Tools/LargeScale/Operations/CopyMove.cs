using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class CopyMove : RemoteLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_COPY_MOVE);

    private int copyMove_type = 0;
    // Existing offsets (remain the internal representation)
    private int copyMove_offsetX = 0;
    private int copyMove_offsetY = 0;
    private bool copyMove_erase = false;

    // New: coordinate mode and unified inputs
    // 0 = Relative (offsets), 1 = Absolute (map coords)
    private int copyMove_coordMode = 0;
    private int copyMove_inputX = 0;
    private int copyMove_inputY = 0;
    
    // New: current area for conversions
    private RectU16 _currentArea;
    private bool _hasArea = false;
    
    private bool useAlternateSource = false;
    private string alternateMapPath = "";
    private string alternateStaIdxPath = "";
    private string alternateStaticsPath = "";
    private int alternateMapWidth = 7168;   // Default to Map0 dimensions
    private int alternateMapHeight = 4096;

    public void SetArea(RectU16 area)
    {
        _currentArea = area;
        _hasArea = true;
    }

    public override bool DrawUI()
    {
        var changed = false;
        
        // Copy/Move radio buttons - DISABLE Move when using alternate source
        if (useAlternateSource)
        {
            // Force Copy mode when using alternate source
            if (copyMove_type != (int)LSO.CopyMove.Copy)
            {
                copyMove_type = (int)LSO.CopyMove.Copy;
                changed = true;
            }
            
            // Draw Copy as enabled
            changed |= ImGui.RadioButton(LangManager.Get(COPY), ref copyMove_type, (int)LSO.CopyMove.Copy);
            
            // Draw Move as disabled
            ImGui.BeginDisabled();
            ImGui.SameLine();
            int disabledMove = (int)LSO.CopyMove.Move;
            ImGui.RadioButton(LangManager.Get(MOVE), ref disabledMove, (int)LSO.CopyMove.Move);
            ImGui.EndDisabled();
            
            // Show tooltip on disabled Move button
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Move is not supported when using a different source map.\nOnly Copy is available.");
            }
        }
        else
        {
            // Normal mode - both Copy and Move enabled
            changed |= ImGui.RadioButton(LangManager.Get(COPY), ref copyMove_type, (int)LSO.CopyMove.Copy);
            ImGui.SameLine();
            changed |= ImGui.RadioButton(LangManager.Get(MOVE), ref copyMove_type, (int)LSO.CopyMove.Move);
        }

        ImGui.Separator();
        
        // NEW: Checkbox for alternate source
        changed |= ImGui.Checkbox("Use Different Source Map", ref useAlternateSource);
        
        if (useAlternateSource)
        {
            ImGui.Text("Source Map Configuration:");
            ImGui.Separator();
            
            // File paths
            ImGui.InputText("Map File##alt", ref alternateMapPath, 256);
            ImGui.SameLine();
            if (ImGui.Button("Browse...##map"))
            {
                TinyFileDialogs.TryOpenFile
                        ("Select Map File", Environment.CurrentDirectory, ["*.mul"], null, false, out var result);
                if (!string.IsNullOrEmpty(result))
                {
                    alternateMapPath = result;
                    // Auto-populate related files
                    var basePath = alternateMapPath.Replace(".mul", "");
                    var mapNumber = basePath[basePath.Length - 1]; // Get map number
                    var directory = Path.GetDirectoryName(alternateMapPath);
                    var baseFileName = Path.GetFileNameWithoutExtension(alternateMapPath)
                        .Replace("Map", "").Replace("map", "");
                    
                    alternateStaIdxPath = Path.Combine(directory, $"Staidx{mapNumber}.mul");
                    alternateStaticsPath = Path.Combine(directory, $"Statics{mapNumber}.mul");
                    
                    // Set default dimensions based on common UO map sizes
                    switch(mapNumber)
                    {
                        case '0': // OLD Felucca/Trammel
                            alternateMapWidth = 6144;
                            alternateMapHeight = 4096;
                            break;
                        case '1': // Felucca/Trammel
                            alternateMapWidth = 7168;
                            alternateMapHeight = 4096;
                            break;
                        case '2': // Ilshenar
                            alternateMapWidth = 2304;
                            alternateMapHeight = 1600;
                            break;
                        case '3': // Malas
                            alternateMapWidth = 2560;
                            alternateMapHeight = 2048;
                            break;
                        case '4': // Tokuno
                            alternateMapWidth = 1448;
                            alternateMapHeight = 1448;
                            break;
                        case '5': // TerMur
                            alternateMapWidth = 1280;
                            alternateMapHeight = 4096;
                            break;
                        case '6': // Custom
                            alternateMapWidth = 1280;
                            alternateMapHeight = 4096;
                            break;
                        default:
                            // Keep current values
                            break;
                    }
                }
            }
            
            ImGui.InputText("StaIdx File##alt", ref alternateStaIdxPath, 256);
            ImGui.InputText("Statics File##alt", ref alternateStaticsPath, 256);
            
            ImGui.Separator();
            
            ImGui.Text("Map Dimensions (in tiles):");
            changed |= ImGuiEx.DragInt("Width##alt", ref alternateMapWidth, 1, 64, 8192);
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Width of the source map in tiles.\nMap0: 6144, Map1: 7168, Map2: 2304, etc...");
            }
            
            changed |= ImGuiEx.DragInt("Height##alt", ref alternateMapHeight, 1, 64, 8192);
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Height of the source map in tiles.\nMap0: 4096, Map1: 4096, Map2: 1600, etc...");
            }
            
            ImGui.Separator();
            
            // Validation warning
            if (!string.IsNullOrEmpty(alternateMapPath) && !File.Exists(alternateMapPath))
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "Warning: Map file not found!");
            }
            if (!string.IsNullOrEmpty(alternateStaIdxPath) && !File.Exists(alternateStaIdxPath))
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1), "Warning: StaIdx file not found!");
            }
            if (!string.IsNullOrEmpty(alternateStaticsPath) && !File.Exists(alternateStaticsPath))
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1), "Warning: Statics file not found!");
            }
        }
        
        ImGui.Separator();
        
        // DESTINATION COORDINATES SECTION
        // When using alternate source, force Absolute mode and disable Relative
        if (useAlternateSource)
        {
            // Force Absolute mode
            if (copyMove_coordMode != 1)
            {
                copyMove_coordMode = 1;
                changed = true;
            }
            
            // Draw Relative as disabled
            ImGui.BeginDisabled();
            int disabledRelative = 0;
            ImGui.RadioButton(LangManager.Get(COORD_MODE_RELATIVE), ref disabledRelative, 0);
            ImGui.EndDisabled();
            
            // Show tooltip on disabled Relative button
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Relative coordinates are not supported when using a different source map.\nOnly Absolute coordinates are available.");
            }
            
            ImGui.SameLine();
            // Draw Absolute as enabled
            changed |= ImGui.RadioButton(LangManager.Get(COORD_MODE_ABSOLUTE), ref copyMove_coordMode, 1);
        }
        else
        {
            // Normal mode - both Relative and Absolute enabled
            int prevMode = copyMove_coordMode;
            changed |= ImGui.RadioButton(LangManager.Get(COORD_MODE_RELATIVE), ref copyMove_coordMode, 0);
            ImGui.SameLine();
            changed |= ImGui.RadioButton(LangManager.Get(COORD_MODE_ABSOLUTE), ref copyMove_coordMode, 1);

            if (prevMode != copyMove_coordMode && _hasArea)
            {
                if (copyMove_coordMode == 1)
                {
                    // Relative -> Absolute
                    copyMove_inputX = _currentArea.X1 + copyMove_inputX;
                    copyMove_inputY = _currentArea.Y1 + copyMove_inputY;
                }
                else
                {
                    // Absolute -> Relative
                    copyMove_inputX = copyMove_inputX - _currentArea.X1;
                    copyMove_inputY = copyMove_inputY - _currentArea.Y1;
                }
            }
        }

        // Reuse same fields for X/Y with mode-specific ranges
        int minX = copyMove_coordMode == 1 ? 0 : -CEDClient.WidthInTiles;
        int maxX = copyMove_coordMode == 1 ? (int)CEDClient.WidthInTiles - 1 : (int)CEDClient.WidthInTiles;
        int minY = copyMove_coordMode == 1 ? 0 : -CEDClient.HeightInTiles;
        int maxY = copyMove_coordMode == 1 ? (int)CEDClient.HeightInTiles - 1 : (int)CEDClient.HeightInTiles;

        // Decide labels based on mode
        // 0 = Relative, 1 = Absolute
        string labelX = copyMove_coordMode == 1 
            ? LangManager.Get(COORDINATE_X)
            : LangManager.Get(OFFSET_X);
        string labelY = copyMove_coordMode == 1
            ? LangManager.Get(COORDINATE_Y)
            : LangManager.Get(OFFSET_Y);
    
        // Keep labels the same input fields regardless of mode
        changed |= ImGuiEx.DragInt(labelX, ref copyMove_inputX, 1, minX, maxX);
        changed |= ImGuiEx.DragInt(labelY, ref copyMove_inputY, 1, minY, maxY);

        changed |= ImGui.Checkbox(LangManager.Get(ERASE_OBJECTS_FROM_TARGET_AREA), ref copyMove_erase);

        // Keep internal offsets in sync with UI
        if (_hasArea && copyMove_coordMode == 1)
        {
            copyMove_offsetX = copyMove_inputX - _currentArea.X1;
            copyMove_offsetY = copyMove_inputY - _currentArea.Y1;
        }
        else
        {
            copyMove_offsetX = copyMove_inputX;
            copyMove_offsetY = copyMove_inputY;
        }

        return !changed;
    }

    public override bool CanSubmit(RectU16 area)
    {
        // Calculate offsets
        if (copyMove_coordMode == 1)
        {
            copyMove_offsetX = copyMove_inputX - area.X1;
            copyMove_offsetY = copyMove_inputY - area.Y1;
        }
        else
        {
            copyMove_offsetX = copyMove_inputX;
            copyMove_offsetY = copyMove_inputY;
        }

        // Validation for alternate source map dimensions
        if (useAlternateSource)
        {
            // Check if source area exists within alternate map bounds
            if (area.X1 >= alternateMapWidth || area.X2 >= alternateMapWidth)
            {
                _submitStatus = $"Source area exceeds alternate map width ({alternateMapWidth})";
                return false;
            }
            if (area.Y1 >= alternateMapHeight || area.Y2 >= alternateMapHeight)
            {
                _submitStatus = $"Source area exceeds alternate map height ({alternateMapHeight})";
                return false;
            }
            
            // Check if files exist
            if (!File.Exists(alternateMapPath))
            {
                _submitStatus = "Alternate map file not found";
                return false;
            }
            if (!File.Exists(alternateStaIdxPath))
            {
                _submitStatus = "Alternate StaIdx file not found";
                return false;
            }
            if (!File.Exists(alternateStaticsPath))
            {
                _submitStatus = "Alternate Statics file not found";
                return false;
            }
        }

        // Existing destination validation
        if (copyMove_offsetX < 0 && copyMove_offsetX + area.X1 < 0)
        {
            _submitStatus = LangManager.Get(INVALID_OFFSET_X);
            return false;
        }
        if (copyMove_offsetX > 0 && copyMove_offsetX + area.X2 > CEDClient.WidthInTiles)
        {
            _submitStatus = LangManager.Get(INVALID_OFFSET_X);
            return false;
        }
        if (copyMove_offsetY < 0 && copyMove_offsetY + area.Y1 < 0)
        {
            _submitStatus = LangManager.Get(INVALID_OFFSET_Y);
            return false;
        }
        if (copyMove_offsetY > 0 && copyMove_offsetY + area.Y2 > CEDClient.HeightInTiles)
        {
            _submitStatus = LangManager.Get(INVALID_OFFSET_Y);
            return false;
        }
        return true;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        if (useAlternateSource)
        {
            
            return new LSOCopyMove(
                (LSO.CopyMove)copyMove_type, 
                copyMove_erase, 
                copyMove_offsetX, 
                copyMove_offsetY,
                alternateMapPath,
                alternateStaIdxPath,
                alternateStaticsPath,
                alternateMapWidth,
                alternateMapHeight
            );
        }
        else
        {
            return new LSOCopyMove(
                (LSO.CopyMove)copyMove_type, 
                copyMove_erase, 
                copyMove_offsetX, 
                copyMove_offsetY
            );
        }
    }
}