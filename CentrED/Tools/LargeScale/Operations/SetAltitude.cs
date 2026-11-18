using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;
using static CentrED.CentrED;

namespace CentrED.Tools.LargeScale.Operations;

public class SetAltitude : RemoteLargeScaleTool
{
    public override string Name => App.Lang.Get(LSO_SET_ALTITUDE);
    
    private int setAltitude_type = 1;
    private int setAltitude_minZ = ZRange.Min;
    private int setAltitude_maxZ = ZRange.Max;
    private int setAltitude_relativeZ = 0;

    public override bool DrawUI()
    {
        var changed = false;
        ImGui.Text(App.Lang.Get(MODE));
        changed |= ImGui.RadioButton(App.Lang.Get(TERRAIN), ref setAltitude_type, (int)LSO.SetAltitude.Terrain);
        ImGuiEx.Tooltip(App.Lang.Get(SET_ALTITUDE_TERRAIN_TOOLTIP));
        ImGui.SameLine();
        changed |= ImGui.RadioButton(App.Lang.Get(RELATIVE), ref setAltitude_type, (int)LSO.SetAltitude.Relative);
        ImGuiEx.Tooltip(App.Lang.Get(SET_ALTITUDE_RELATIVE_TOOLTIP));
        if (setAltitude_type == (int)LSO.SetAltitude.Terrain)
        {
            changed |= ImGuiEx.DragInt("Min Z", ref setAltitude_minZ, 1, ZRange.Min, ZRange.Max);
            changed |= ImGuiEx.DragInt("Max Z", ref setAltitude_maxZ, 1, ZRange.Min, ZRange.Max);
        }
        else
        {
            changed |= ImGuiEx.DragInt($"{App.Lang.Get(RELATIVE)} Z", ref setAltitude_relativeZ, 1, ZRange.Min, ZRange.Max);
        }
        return !changed;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return setAltitude_type switch
        {
            (int)LSO.SetAltitude.Terrain => new LSOSetAltitude((short)setAltitude_minZ, (short)setAltitude_maxZ),
            (int)LSO.SetAltitude.Relative => new LSOSetAltitude((short)setAltitude_relativeZ),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}