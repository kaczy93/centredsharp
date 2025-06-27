using CentrED.Client;
using CentrED.Network;
using CentrED.UI;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CentrED.Tools.LargeScale.Operations;

public class ImportHeightmap : LocalLargeScaleTool
{
    public override string Name => "Import Heightmap";
    
    private string _importFilePath = "";
    private Image<L8>? _importFile;
    private bool _withStatics;
    
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
        ImGui.Checkbox("With Statics", ref _withStatics);
        ImGuiEx.Tooltip("If this is checked, statics will also be elevated");
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

    protected override void PostProcessArea(CentrEDClient client, AreaInfo area)
    {
        base.PostProcessArea(client, area);
        _importFile!.Dispose();
        _importFile = null;
    }
}