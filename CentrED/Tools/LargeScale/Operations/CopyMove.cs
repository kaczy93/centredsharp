using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.Tools.LargeScale.Operations;

public class CopyMove : RemoteLargeScaleTool
{
    public override string Name => "Copy/Move";
    
    private int copyMove_type = 0;
    private int copyMove_offsetX = 0;
    private int copyMove_offsetY = 0;
    private bool copyMove_erase = false;

    public override bool DrawUI()
    {
        var changed = false;
        ImGui.Text("Operation Type");
        changed |= ImGui.RadioButton("Copy", ref copyMove_type, (int)LSO.CopyMove.Copy);
        ImGui.SameLine();
        changed |= ImGui.RadioButton("Move", ref copyMove_type, (int)LSO.CopyMove.Move);
        changed |= ImGuiEx.DragInt("Offset X", ref copyMove_offsetX, 1, -CEDClient.WidthInTiles, CEDClient.WidthInTiles);
        changed |= ImGuiEx.DragInt("Offset Y", ref copyMove_offsetY, 1, -CEDClient.HeightInTiles, CEDClient.HeightInTiles);
        changed |= ImGui.Checkbox("Erase statics from target area", ref copyMove_erase);
        return !changed;
    }

    public override bool CanSubmit(RectU16 area)
    {
        if (copyMove_offsetX < 0 && copyMove_offsetX + area.X1 < 0)
        {
            _submitStatus = "Invalid OffsetX";
            return false;
        }
        if(copyMove_offsetX > 0 && copyMove_offsetX + area.X2 > CEDClient.WidthInTiles)
        {
            _submitStatus = "Invalid OffsetX";
            return false;
        }
        if (copyMove_offsetY < 0 && copyMove_offsetY + area.Y1 < 0)
        {
            _submitStatus = "Invalid OffsetY";
            return false;
        }
        if (copyMove_offsetY > 0 && copyMove_offsetY + area.Y2 > CEDClient.HeightInTiles)
        {
            _submitStatus = "Invalid OffsetY";
            return false;
        }
        return true;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSOCopyMove((LSO.CopyMove)copyMove_type, copyMove_erase, copyMove_offsetX, copyMove_offsetY);
    }
}