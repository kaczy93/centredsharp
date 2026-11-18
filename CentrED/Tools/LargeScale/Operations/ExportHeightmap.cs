using CentrED.Client;
using CentrED.Network;
using Hexa.NET.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using static CentrED.LangEntry;
using static CentrED.CentrED;

namespace CentrED.Tools.LargeScale.Operations;

public class ExportHeightmap : LocalLargeScaleTool
{
    public override string Name => App.Lang.Get(LSO_EXPORT_HEIGHTMAP);

    private string _exportFilePath = "";
    private Image<L8>? _exportFile;
    
    private int xOffset;
    private int yOffset;

    protected override bool DrawToolUI()
    {
        ImGui.InputText(App.Lang.Get(FILE_PATH), ref _exportFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TrySaveFile
                    (App.Lang.Get(SELECT_FILE), Environment.CurrentDirectory, ["*.bmp"], null, out var newPath))
            {
                _exportFilePath = newPath;
                return false;
            }
        }
        return true;
    }

    public override bool CanSubmit(RectU16 area)
    {
        if (string.IsNullOrEmpty(_exportFilePath) || !_exportFilePath.EndsWith(".bmp"))
        {
            _submitStatus = App.Lang.Get(INVALID_FILE_FORMAT);
            return false;
        }
        try
        {
            using var file = File.OpenWrite(_exportFilePath);
        }
        catch (Exception e)
        {
            _submitStatus = string.Format(App.Lang.Get(OPEN_FILE_ERROR_1INFO), e.Message);
            return false;
        }
        return true;
    }

    protected override void PreProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PreProcessArea(client, area);
        _exportFile = new Image<L8>(area.Width, area.Height);
        xOffset = area.X1;
        yOffset = area.Y1;
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        var z = client.GetLandTile(x, y).Z;
        var value = (byte)(z - 128);
        _exportFile![x - xOffset, y - yOffset] = new L8(value);
    }

    protected override void PostProcessArea(CentrEDClient client, RectU16 area)
    {
        using var fileStream = File.OpenWrite(_exportFilePath);
        _exportFile!.Save(fileStream, new BmpEncoder()
        {
            BitsPerPixel = BmpBitsPerPixel.Pixel8
        });
        _exportFile.Dispose();
        _exportFile = null;
    }
}