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

    public override void DrawUI()
    {
        ImGui.Text("Operation Type");
        ImGui.RadioButton("Copy", ref copyMove_type, (int)LSO.CopyMove.Copy);
        ImGui.SameLine();
        ImGui.RadioButton("Move", ref copyMove_type, (int)LSO.CopyMove.Move);
        UIManager.DragInt("Offset X", ref copyMove_offsetX, 1, -CEDClient.Width * 8, CEDClient.Width * 8);
        UIManager.DragInt("Offset Y", ref copyMove_offsetY, 1, -CEDClient.Height * 8, CEDClient.Height * 8);
        ImGui.Checkbox("Erase statics from target area", ref copyMove_erase);
    }
    
    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSOCopyMove((LSO.CopyMove)copyMove_type, copyMove_erase, copyMove_offsetX, copyMove_offsetY);
    }
}