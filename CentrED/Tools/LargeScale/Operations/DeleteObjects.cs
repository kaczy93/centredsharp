using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using CentrED.Utils;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class DeleteObjects : RemoteLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_DELETE_OBJECTS);
    
    private string removeStatics_idsText = "";
    private ushort[] removeStatics_ids;
    private int removeStatics_minZ = -128;
    private int removeStatics_maxZ = 127;

    public override bool DrawUI()
    {
        var changed = false;
        changed |= ImGui.InputText(LangManager.Get(IDS), ref removeStatics_idsText, 1024);
        ImGuiEx.Tooltip(LangManager.Get(DELETE_OBJECTS_IDS_TOOLTIP));
        changed |= ImGuiEx.DragInt("Min Z", ref removeStatics_minZ, 1, -128, 127);
        changed |= ImGuiEx.DragInt("Max Z", ref removeStatics_maxZ, 1, -128, 127);
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
            removeStatics_ids = removeStatics_idsText.Split(',').Select(s => (ushort)(UshortParser.Apply(s) + 0x4000)).ToArray();
        }
        catch (Exception e)
        {
            _submitStatus = string.Format(LangManager.Get(INVALIDS_IDS_1INFO), e.Message);
            return false;
        }
        return true;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSODeleteStatics(removeStatics_ids, (sbyte)removeStatics_minZ, (sbyte)removeStatics_maxZ);
    }
}