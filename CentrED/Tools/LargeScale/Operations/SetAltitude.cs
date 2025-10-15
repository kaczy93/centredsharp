using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class SetAltitude : RemoteLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_SET_ALTITUDE);
    
    private int setAltitude_type = 1;
    private int setAltitude_minZ = -128;
    private int setAltitude_maxZ = 127;
    private int setAltitude_relativeZ = 0;

    public override bool DrawUI()
    {
        var changed = false;
        ImGui.Text(LangManager.Get(MODE));
        changed |= ImGui.RadioButton(LangManager.Get(TERRAIN), ref setAltitude_type, (int)LSO.SetAltitude.Terrain);
        ImGuiEx.Tooltip(LangManager.Get(SET_ALTITUDE_TERRAIN_TOOLTIP));
        ImGui.SameLine();
        changed |= ImGui.RadioButton(LangManager.Get(RELATIVE), ref setAltitude_type, (int)LSO.SetAltitude.Relative);
        ImGuiEx.Tooltip(LangManager.Get(SET_ALTITUDE_RELATIVE_TOOLTIP));
        if (setAltitude_type == (int)LSO.SetAltitude.Terrain)
        {
            changed |= ImGuiEx.DragInt("Min Z", ref setAltitude_minZ, 1, -128, 127);
            changed |= ImGuiEx.DragInt("Max Z", ref setAltitude_maxZ, 1, -128, 127);
        }
        else
        {
            changed |= ImGuiEx.DragInt($"{LangManager.Get(RELATIVE)} Z", ref setAltitude_relativeZ, 1, -128, 127);
        }
        return !changed;
    }

    protected override ILargeScaleOperation SubmitLSO()
    {
        return setAltitude_type switch
        {
            (int)LSO.SetAltitude.Terrain => new LSOSetAltitude((sbyte)setAltitude_minZ, (sbyte)setAltitude_maxZ),
            (int)LSO.SetAltitude.Relative => new LSOSetAltitude((sbyte)setAltitude_relativeZ),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}