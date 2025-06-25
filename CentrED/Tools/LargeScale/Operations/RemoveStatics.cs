using CentrED.Client.Map;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class RemoveStatics : RemoteLargeScaleTool
{
    public override string Name => "Remove Statics";
    
    private string removeStatics_idsText = "";
    private int removeStatics_minZ = -128;
    private int removeStatics_maxZ = 127;

    public override bool DrawUI()
    {
        var changed = false;
        changed |= ImGui.InputText("ids", ref removeStatics_idsText, 1024);
        UIManager.Tooltip("Leave empty to remove all statics");
        changed |=  UIManager.DragInt("MinZ", ref removeStatics_minZ, 1, -128, 127);
        changed |= UIManager.DragInt("MaxZ", ref removeStatics_maxZ, 1, -128, 127);
        return !changed;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSODeleteStatics(removeStatics_idsText, (sbyte)removeStatics_minZ, (sbyte)removeStatics_maxZ);
    }
}