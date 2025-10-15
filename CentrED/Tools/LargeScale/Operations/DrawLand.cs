using CentrED.Client.Map;
using CentrED.Network;
using CentrED.Utils;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class DrawLand : RemoteLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_DRAW_LAND);
    
    private string drawLand_idsText = "";
    private ushort[] drawLand_ids;

    public override bool DrawUI()
    {
        var changed = ImGui.InputText(LangManager.Get(IDS), ref drawLand_idsText, 1024);
        return !changed;
    }
    
    public override bool CanSubmit(RectU16 area)
    {
        try
        {
            drawLand_ids = drawLand_idsText.Split(',').Select(UshortParser.Apply).ToArray();
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
        return new LSODrawLand(drawLand_ids);
    }
}