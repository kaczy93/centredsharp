using CentrED.Client;
using CentrED.Network;
using CentrED.UI;
using Hexa.NET.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class ImportHeightmap : LocalLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_IMPORT_HEIGHTMAP);
    
    private string _importFilePath = "";
    private Image<L8>? _importFile;
    private bool _withStatics;
    
    private int xOffset;
    private int yOffset;
    protected override bool DrawToolUI()
    {
        ImGui.InputText(LangManager.Get(FILE_PATH), ref _importFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile
                    (LangManager.Get(SELECT_FILE), Environment.CurrentDirectory, ["*.bmp"], null, false, out var newPath))
            {
                _importFilePath = newPath;
                return false;
            }
        }
        ImGui.Checkbox(LangManager.Get(WITH_OBJECTS), ref _withStatics);
        ImGuiEx.Tooltip(LangManager.Get(WITH_OBJECTS_TOOLTIP));
        return true;
    }

    public override bool CanSubmit(RectU16 area)
    {
        try
        {
            using var fileStream = File.OpenRead(_importFilePath);
            try
            {
                _importFile = Image.Load<L8>(fileStream);
            }
            catch (Exception e)
            {
                _submitStatus = string.Format(LangManager.Get(LOAD_IMAGE_ERROR_1INFO), e.Message);
                return false;
            }
        }
        catch (Exception e)
        {
            _submitStatus = string.Format(LangManager.Get(OPEN_FILE_ERROR_1INFO), e.Message);
            return false;       
        }
        if (_importFile.Width != area.Width || _importFile.Height != area.Height)
        {
            _submitStatus = LangManager.Get(FILE_SIZE_MISMATCH_AREA);
            return false;
        }
        return true;
    }

    protected override void PreProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PreProcessArea(client, area);
        xOffset = area.X1;
        yOffset = area.Y1;
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        var value = _importFile![x - xOffset, y - yOffset].PackedValue;
        var newZ = (sbyte)(value + 128);
        var landTile = client.GetLandTile(x, y);
        var zDelta = (sbyte)(newZ - landTile.Z);
        if (_withStatics)
        {
            foreach (var staticTile in client.GetStaticTiles(x, y))
            {
                staticTile.Z += zDelta;
            }
        }
        client.GetLandTile(x, y).Z += zDelta;
    }

    protected override void PostProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PostProcessArea(client, area);
        _importFile!.Dispose();
        _importFile = null;
    }
}