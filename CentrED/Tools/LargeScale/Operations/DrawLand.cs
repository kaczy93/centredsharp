using System.Globalization;
using CentrED.Client.Map;
using CentrED.Network;
using CentrED.Utils;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class DrawLand : RemoteLargeScaleTool
{
    public override string Name => "Draw Land";
    
    private string drawLand_idsText = "";
    private ushort[] drawLand_ids;

    public override bool DrawUI()
    {
        var changed = ImGui.InputText("ids", ref drawLand_idsText, 1024);
        return !changed;
    }
    
    public override bool CanSubmit(AreaInfo area)
    {
        try
        {
            drawLand_ids = drawLand_idsText.Split(',').Select(UshortParser.Apply).ToArray();
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
        return new LSODrawLand(drawLand_ids);
    }
}