using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using CentrED.Utils;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;
using static CentrED.CentrED;

namespace CentrED.Tools.LargeScale.Operations;

public class InsertObjects : RemoteLargeScaleTool
{
    public override string Name => App.Lang.Get(LSO_INSERT_OBJECTS);
    
    private string addStatics_idsText = "";
    private uint[] addStatics_ids;
    private int addStatics_chance = 100;
    private int addStatics_type = 1;
    private int addStatics_fixedZ = 0;

    public override bool DrawUI()
    {
        var changed = false;
        changed |= ImGui.InputText(App.Lang.Get(IDS), ref addStatics_idsText, 1024);
        changed |= ImGui.DragInt(App.Lang.Get(CHANCE), ref addStatics_chance, 1, 0, 100);
        ImGui.Text(App.Lang.Get(MODE));
        changed |= ImGui.RadioButton(App.Lang.Get(TERRAIN), ref addStatics_type, (int)LSO.StaticsPlacement.Terrain);
        changed |= ImGui.RadioButton(App.Lang.Get(ON_TOP), ref addStatics_type, (int)LSO.StaticsPlacement.Top);
        changed |= ImGui.RadioButton(App.Lang.Get(FIXED_Z), ref addStatics_type, (int)LSO.StaticsPlacement.Fix);
        if (addStatics_type == (int)LSO.StaticsPlacement.Fix)
        {
            changed |= ImGuiEx.DragInt("Z", ref addStatics_fixedZ, 1, ZRange.MIN, ZRange.MAX);
        }
        return !changed;
    }
    
    public override bool CanSubmit(RectU16 area)
    {
        try
        {
            addStatics_ids = addStatics_idsText.Split(',').Select(s => UshortParser.Apply(s) + StaticId.LSO_OFFSET).ToArray();
        }
        catch (Exception e)
        {
            _submitStatus = string.Format(App.Lang.Get(INVALIDS_IDS_1INFO), e.Message);
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
            (short)addStatics_fixedZ
        );
    }
}