using CentrED.Client.Map;
using CentrED.Network;
using CentrED.UI;
using CentrED.Utils;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class DrawLand : RemoteLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_DRAW_LAND);

    private string drawLand_idsText = "";
    private (ushort TileId, byte Chance)[] drawLand_tiles = [];

    public override bool DrawUI()
    {
        var changed = ImGui.InputText(LangManager.Get(IDS), ref drawLand_idsText, 1024);
        ImGuiEx.Tooltip(LangManager.Get(DRAW_LAND_IDS_TOOLTIP));
        return !changed;
    }

    public override bool CanSubmit(RectU16 area)
    {
        try
        {
            var entries = drawLand_idsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var tiles = new List<(ushort, byte)>();

            foreach (var entry in entries)
            {
                var parts = entry.Split(':');
                var tileId = UshortParser.Apply(parts[0].Trim());
                byte chance = 100;
                if (parts.Length > 1)
                {
                    chance = byte.Parse(parts[1].Trim());
                }
                tiles.Add((tileId, chance));
            }

            drawLand_tiles = tiles.ToArray();
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
        return new LSODrawLand(drawLand_tiles);
    }
}