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

    public void SetArea(RectU16 area)
    {
        _currentArea = area;
        _hasArea = true;
    }

    public override bool DrawUI()
    {
        var changed = false;

        // Copy / Move
        changed |= ImGui.RadioButton(LangManager.Get(COPY), ref copyMove_type, (int)LSO.CopyMove.Copy);
        ImGui.SameLine();
        changed |= ImGui.RadioButton(LangManager.Get(MOVE), ref copyMove_type, (int)LSO.CopyMove.Move);

        ImGui.Separator();

        // Relative / Absolute toggle with auto-conversion
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
        // Ensure offsets computed from current inputs/mode/area
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

        // Existing bounds checks
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
        // Still submit offsets; absolute entries were converted above
        return new LSOCopyMove((LSO.CopyMove)copyMove_type, copyMove_erase, copyMove_offsetX, copyMove_offsetY);
    }
}