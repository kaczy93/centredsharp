using CentrED.Map;
using CentrED.UI.Windows;
using CentrED.Utils;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.LangEntry;

namespace CentrED.Tools;

public class HueTool : BaseTool
{
    private readonly HuesWindow _huesWindow;
    
    public HueTool()
    {
        _huesWindow = UIManager.GetWindow<HuesWindow>();
    }
    
    public override string Name => LangManager.Get(HUE_TOOL);
    public override Keys Shortcut => Keys.F6;

    private enum HueSource
    {
        HUE,
        HUE_SET
    }
    
    private int _hueSource;

    internal override void Draw()
    {
        ImGui.Text(LangManager.Get(SOURCE));
        ImGui.RadioButton(LangManager.Get(HUES), ref _hueSource, (int)HueSource.HUE);
        ImGui.RadioButton(LangManager.Get(HUE_SET), ref _hueSource, (int)HueSource.HUE_SET);
        if (_huesWindow.ActiveHueSetValues.Count <= 0)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(LangManager.Get(EMPTY));
        }
        ImGui.Separator();
        base.Draw();
    }

    public override void OnActivated(TileObject? o)
    {
        UIManager.GetWindow<HuesWindow>().Show = true;
    }

    public ushort ActiveHue => (HueSource)_hueSource switch
    {
        HueSource.HUE => _huesWindow.SelectedIds.GetRandom() ?? 0,
        HueSource.HUE_SET => _huesWindow.ActiveHueSetValues.GetRandom() ?? 0,
        _ => 0
    };

    protected override void GhostApply(TileObject? o)
    {
        if (o is StaticObject so)
        {
            so.GhostHue = ActiveHue;
        }
    }

    protected override void GhostClear(TileObject? o)
    {
        if (o is StaticObject)
        {
            o.Reset();
        }
    }

    protected override void InternalApply(TileObject? o)
    {
        if (o is StaticObject so && so.GhostHue != -1)
            so.StaticTile.Hue = (ushort)so.GhostHue;
    }
}