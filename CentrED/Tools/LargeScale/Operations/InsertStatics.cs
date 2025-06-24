using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class InsertStatics : RemoteLargeScaleTool
{
    public override string Name => "Insert Statics";
    
    private string addStatics_idsText = "";
    private int addStatics_chance = 100;
    private int addStatics_type = 1;
    private int addStatics_fixedZ = 0;

    public override void DrawUI()
    {
        ImGui.InputText("ids", ref addStatics_idsText, 1024);
        ImGui.DragInt("Chance", ref addStatics_chance, 1, 0, 100);
        ImGui.Text("Placement type");
        ImGui.RadioButton("Terrain", ref addStatics_type, (int)LSO.StaticsPlacement.Terrain);
        ImGui.RadioButton("On Top", ref addStatics_type, (int)LSO.StaticsPlacement.Top);
        ImGui.RadioButton("Fixed Z", ref addStatics_type, (int)LSO.StaticsPlacement.Fix);
        if (addStatics_type == (int)LSO.StaticsPlacement.Fix)
        {
            UIManager.DragInt("Z", ref addStatics_fixedZ, 1, -128, 127);
        }
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSOAddStatics
        (
            addStatics_idsText.Split(',').Select(s => (ushort)(int.Parse(s) + 0x4000)).ToArray(),
            (byte)addStatics_chance,
            (LSO.StaticsPlacement)addStatics_type,
            (sbyte)addStatics_fixedZ
        );
    }
}