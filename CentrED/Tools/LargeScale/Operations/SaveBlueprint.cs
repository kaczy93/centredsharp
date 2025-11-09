using CentrED.Blueprints;
using CentrED.Blueprints.Writers;
using CentrED.Client;
using CentrED.Network;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class SaveBlueprint : LocalLargeScaleTool
{
    public override string Name => LangManager.Get(SAVE_BLUEPRINT);
    
    private string _name = "";
    private bool _withHue;
    private bool _overwrite;
    protected override bool DrawToolUI()
    {
        ImGui.InputText(LangManager.Get(NAME), ref _name, 64);
        ImGui.Checkbox(LangManager.Get(WITH_HUE), ref _withHue);
        ImGui.SetItemTooltip(LangManager.Get(SAVE_BLUEPRINT_WITH_HUE_TOOLTIP));
        ImGui.Checkbox(LangManager.Get(OVERWRITE), ref _overwrite);
        ImGui.SetItemTooltip(LangManager.Get(SAVE_BLUEPRINT_OVERWRITE_TOOLTIP));
        return true;
    }

    private string BlueprintPath => $"{BlueprintManager.BLUEPRINTS_DIR}/{_name}.csv";

    public override bool CanSubmit(RectU16 area)
    {
        if (string.IsNullOrEmpty(_name))
        {
            _submitStatus = LangManager.Get(EMPTY_NAME_ERROR);
            return false;
        }
        if(!_overwrite && File.Exists(BlueprintPath))
        {
            _submitStatus = LangManager.Get(FILE_ALREADY_EXISTS);
            return false;
        }
        return true;
    }

    private PointU16 _center;
    private List<BlueprintTile> _blueprintTiles = [];
    
    protected override void PreProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PreProcessArea(client, area);
        _blueprintTiles = new List<BlueprintTile>(area.Width * area.Height);
        _center = new PointU16((ushort)(area.X1 + area.Width / 2), (ushort)(area.Y1 + area.Height / 2));
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        var tiles = client.GetStaticTiles(x, y);
        foreach (var t in tiles)
        {
            _blueprintTiles.Add(new BlueprintTile(t.Id, (short)(t.X - _center.X), (short)(t.Y - _center.Y), t.Z, (ushort)(_withHue ? t.Hue : 0), true));
        }
    }

    protected override void PostProcessArea(CentrEDClient client, RectU16 area)
    {
        var minZ = _blueprintTiles.Min(t => t.Z);
        var tiles = _blueprintTiles.Select(t => t with { Z = (short)(t.Z - minZ) });
        using(var fs = File.Open(BlueprintPath, _overwrite ? FileMode.Create: FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using(var writer = new StreamWriter(fs)){
            CsvWriter.Write(tiles, writer);
        }
        //TODO: Load only changed blueprint
        Application.CEDGame.MapManager.BlueprintManager.Load();
    }
}