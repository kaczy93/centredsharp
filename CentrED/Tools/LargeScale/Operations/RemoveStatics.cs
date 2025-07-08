using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using CentrED.Utils;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class RemoveStatics : RemoteLargeScaleTool
{
    public override string Name => "Remove Statics";
    
    private string removeStatics_idsText = "";
    private ushort[] removeStatics_ids;
    private int removeStatics_minZ = -128;
    private int removeStatics_maxZ = 127;

    public override bool DrawUI()
    {
        var changed = false;
        changed |= ImGui.InputText("ids", ref removeStatics_idsText, 1024);
        ImGuiEx.Tooltip("Leave empty to remove all statics");
        changed |= ImGuiEx.DragInt("MinZ", ref removeStatics_minZ, 1, -128, 127);
        changed |= ImGuiEx.DragInt("MaxZ", ref removeStatics_maxZ, 1, -128, 127);
        return !changed;
    }
    public override bool CanSubmit(RectU16 area)
    {
        if (string.IsNullOrWhiteSpace(removeStatics_idsText))
        {
            removeStatics_ids = [];
            return true;
        }
        try
        {
            removeStatics_ids = removeStatics_idsText.Split(',').Select(UshortParser.Apply).ToArray();
        }
        catch (Exception e)
        {
            _submitStatus = "Invalid ids: " + e.Message;
            return false;
        }
        return true;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSODeleteStatics(removeStatics_ids, (sbyte)removeStatics_minZ, (sbyte)removeStatics_maxZ);
    }
}