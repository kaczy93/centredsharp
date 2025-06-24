using CentrED.Client;
using CentrED.Network;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CentrED.Tools.LargeScale.Operations;

public class ImportHeightmap : LocalLargeScaleTool
{
    public override string Name => "Import Heightmap";
    
    private string _importFilePath = "";
    private Image<L8>? _importFile;
    private string _importStatus = "";
    public override void DrawUI()
    {
        ImGui.InputText("File", ref _importFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile
                    ("Select file", Environment.CurrentDirectory, ["*.bmp"], null, false, out var newPath))
            {
                _importFilePath = newPath;
            }
        }
        if (ImGui.Button("Load"))
        {
            using (var fileStream = File.OpenRead(_importFilePath))
            {
                try
                {
                    _importFile = Image.Load<L8>(fileStream);
                }
                catch (Exception e)
                {
                    _importStatus = "Unable to load image: " + e.Message;
                }
            }
        }
        ImGui.Text(_importStatus);
    }

    public override bool CanSubmit(CentrEDClient client, AreaInfo area, out string message)
    {
        if (_importFile == null)
        {
            message = "You must load a file first";
            return false;
        }
        if (_importFile.Width != client.WidthInTiles || _importFile.Height != client.HeightInTiles)
        {
            message = "The file must be the same size as the map";
            return false;
        }
        message = "";
        return true;
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        client.GetLandTile(x, y).Z = (sbyte)_importFile![x, y].PackedValue;
    }
}