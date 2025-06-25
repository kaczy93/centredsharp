using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class SetAltitude : RemoteLargeScaleTool
{
    public override string Name => "Set Altitude";
    
    private int setAltitude_type = 1;
    private int setAltitude_minZ = -128;
    private int setAltitude_maxZ = 127;
    private int setAltitude_relativeZ = 0;

    public override bool DrawUI()
    {
        var changed = false;
        ImGui.Text("Operation Type");
        changed |= ImGui.RadioButton("Terrain", ref setAltitude_type, (int)LSO.SetAltitude.Terrain);
        UIManager.Tooltip("Set terrain altitude\n" +
                          "Terrain altitude will be changed to a random value between minZ and maxZ\n" +
                          "Statics will be elevated according to the terrain change");
        ImGui.SameLine();
        changed |= ImGui.RadioButton("Relative", ref setAltitude_type, (int)LSO.SetAltitude.Relative);
        UIManager.Tooltip("Relative altitude change\n" + 
                          "Terrain and statics altitude will be changed by the specified amount");
        if (setAltitude_type == (int)LSO.SetAltitude.Terrain)
        {
            changed |= UIManager.DragInt("MinZ", ref setAltitude_minZ, 1, -128, 127);
            changed |= UIManager.DragInt("MaxZ", ref setAltitude_maxZ, 1, -128, 127);
        }
        else
        {
            changed |= UIManager.DragInt("RelatizeZ", ref setAltitude_relativeZ, 1, -128, 127);
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