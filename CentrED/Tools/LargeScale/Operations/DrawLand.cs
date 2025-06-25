using CentrED.Client.Map;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class DrawLand : RemoteLargeScaleTool
{
    public override string Name => "Draw Land";
    
    private string drawLand_idsText = "";

    public override bool DrawUI()
    {
        var changed = ImGui.InputText("ids", ref drawLand_idsText, 1024);
        return !changed;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return new LSODrawLand(drawLand_idsText.Split(',').Select(ushort.Parse).ToArray());
    }
}