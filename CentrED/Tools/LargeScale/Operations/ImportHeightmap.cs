using CentrED.Client;
using CentrED.Network;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static CentrED.Application;

namespace CentrED.Tools.LargeScale.Operations;

public class ImportHeightmap : LocalLargeScaleTool
{
    public override string Name => "Import Heightmap";
    
    private string _importFilePath = "";
    private Image<L8>? _importFile;
    
    private int xOffset;
    private int yOffset;
    protected override bool DrawToolUI()
    {
        ImGui.InputText("File", ref _importFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile
                    ("Select file", Environment.CurrentDirectory, ["*.bmp"], null, false, out var newPath))
            {
                _importFilePath = newPath;
                return false;
            }
        }
        return true;
    }

    public override bool CanSubmit(AreaInfo area)
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
                _submitStatus = "Unable to load image: " + e.Message;
                return false;
            }
        }
        catch (Exception e)
        {
            _submitStatus = "Unable to open file: " + e.Message;
            return false;       
        }
        if (_importFile.Width != area.Width || _importFile.Height != area.Height)
        {
            _submitStatus = "The file must be the same size as selected area";
            return false;
        }
        return true;
    }

    protected override void PreProcessArea(CentrEDClient client, AreaInfo area)
    {
        base.PreProcessArea(client, area);
        xOffset = area.Left;
        yOffset = area.Top;
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        var value = _importFile![x - xOffset, y - yOffset].PackedValue;
        var newZ = (sbyte)(value + 128);
        client.GetLandTile(x, y).Z = newZ;
    }

    protected override void PostProcessArea(CentrEDClient client, AreaInfo area)
    {
        base.PostProcessArea(client, area);
        _importFile!.Dispose();
        _importFile = null;
    }
}