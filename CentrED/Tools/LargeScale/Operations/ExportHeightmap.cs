using CentrED.Client;
using CentrED.Network;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

namespace CentrED.Tools.LargeScale.Operations;

public class ExportHeightmap : LocalLargeScaleTool
{
    public override string Name => "Export Heightmap";

    private string _exportFilePath = "";
    private Image<L8>? _exportFile;
    
    private int xOffset;
    private int yOffset;

    protected override bool DrawToolUI()
    {
        ImGui.InputText("File", ref _exportFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TrySaveFile
                    ("Select file", Environment.CurrentDirectory, ["*.bmp"], null, out var newPath))
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
            _submitStatus = "Selected file must be a bmp file";
            return false;
        }
        try
        {
            using var file = File.OpenWrite(_exportFilePath);
        }
        catch (Exception e)
        {
            _submitStatus = "Unable to open file: " + e.Message;
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