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
    private int copyMove_offsetX = 0;
    private int copyMove_offsetY = 0;
    private bool copyMove_erase = false;

    public override bool DrawUI()
    {
        var changed = false;
        changed |= ImGui.RadioButton(LangManager.Get(COPY), ref copyMove_type, (int)LSO.CopyMove.Copy);
        ImGui.SameLine();
        changed |= ImGui.RadioButton(LangManager.Get(MOVE), ref copyMove_type, (int)LSO.CopyMove.Move);
        changed |= ImGuiEx.DragInt(LangManager.Get(OFFSET_X), ref copyMove_offsetX, 1, -CEDClient.WidthInTiles, CEDClient.WidthInTiles);
        changed |= ImGuiEx.DragInt(LangManager.Get(OFFSET_Y), ref copyMove_offsetY, 1, -CEDClient.HeightInTiles, CEDClient.HeightInTiles);
        changed |= ImGui.Checkbox(LangManager.Get(ERASE_OBJECTS_FROM_TARGET_AREA), ref copyMove_erase);
        return !changed;
    }

    public override bool CanSubmit(RectU16 area)
    {
        if (copyMove_offsetX < 0 && copyMove_offsetX + area.X1 < 0)
        {
            _submitStatus = LangManager.Get(INVALID_OFFSET_X);
            return false;
        }
        if(copyMove_offsetX > 0 && copyMove_offsetX + area.X2 > CEDClient.WidthInTiles)
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
        return new LSOCopyMove((LSO.CopyMove)copyMove_type, copyMove_erase, copyMove_offsetX, copyMove_offsetY);
    }
}