using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using CentrED.Utils;
using Hexa.NET.ImGui;

namespace CentrED.Tools.LargeScale.Operations;

public class InsertStatics : RemoteLargeScaleTool
{
    public override string Name => "Insert Statics";
    
    private string addStatics_idsText = "";
    private ushort[] addStatics_ids;
    private int addStatics_chance = 100;
    private int addStatics_type = 1;
    private int addStatics_fixedZ = 0;

    public override bool DrawUI()
    {
        var changed = false;
        changed |= ImGui.InputText("ids", ref addStatics_idsText, 1024);
        changed |= ImGui.DragInt("Chance", ref addStatics_chance, 1, 0, 100);
        ImGui.Text("Placement type"u8);
        changed |= ImGui.RadioButton("Terrain", ref addStatics_type, (int)LSO.StaticsPlacement.Terrain);
        changed |= ImGui.RadioButton("On Top", ref addStatics_type, (int)LSO.StaticsPlacement.Top);
        changed |= ImGui.RadioButton("Fixed Z", ref addStatics_type, (int)LSO.StaticsPlacement.Fix);
        if (addStatics_type == (int)LSO.StaticsPlacement.Fix)
        {
            changed |= ImGuiEx.DragInt("Z", ref addStatics_fixedZ, 1, -128, 127);
        }
        return !changed;
    }
    
    public override bool CanSubmit(RectU16 area)
    {
        try
        {
            addStatics_ids = addStatics_idsText.Split(',').Select(s => (ushort)(UshortParser.Apply(s) + 0x4000)).ToArray();
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
        return new LSOAddStatics
        (
            addStatics_ids,
            (byte)addStatics_chance,
            (LSO.StaticsPlacement)addStatics_type,
            (sbyte)addStatics_fixedZ
        );
    }
}