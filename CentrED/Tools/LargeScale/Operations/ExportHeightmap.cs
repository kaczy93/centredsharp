using CentrED.Client;
using CentrED.Network;
using ImGuiNET;

namespace CentrED.Tools.LargeScale.Operations;

public class ExportHeightmap : LocalLargeScaleTool
{
    public override string Name => "Heightmap";

    private string _exportFilePath = "";

    public override void DrawUI()
    {
        ImGui.Text("Selected area will be exported to a heightmap file");
        ImGui.InputText("File", ref _exportFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TrySaveFile
                    ("Select file", Environment.CurrentDirectory, ["*.bmp"], null, out var newPath))
            {
                _exportFilePath = newPath;
            }
        }
    }

    public override bool CanSubmit(CentrEDClient client, AreaInfo area, out string message)
    {
        message = ""; //TODO
        return !string.IsNullOrEmpty(_exportFilePath) && _exportFilePath.EndsWith(".bmp");
    }
    
    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        throw new NotImplementedException();
    }
    
    
    private void ExportHeightMap()
    {
        // var client = Application.CEDClient;
        // var imageWidth = client.Width * 8;
        // var imageHeight = client.Height * 8;
        // client.ResizeCache(client.Width * client.Height + 1);
        // Application.CEDGame.MapManager.DisableBlockLoading();
        // client.LoadBlocks(new AreaInfo(0, 0, (ushort)(imageWidth - 1), (ushort)(imageHeight - 1)));
        // Application.CEDGame.MapManager.EnableBlockLoading();
        // using Image<L8> image = new(imageWidth, imageHeight);
        // image.ProcessPixelRows
        // (accessor =>
        //     {
        //         for (int y = 0; y < accessor.Height; y++)
        //         {
        //             Span<L8> pixelRow = accessor.GetRowSpan(y);
        //             // pixelRow.Length has the same value as accessor.Width,
        //             // but using pixelRow.Length allows the JIT to optimize away bounds checks:
        //             for (int x = 0; x < pixelRow.Length; x++)
        //             {
        //                 // Get a reference to the pixel at position x
        //                 ref L8 pixel = ref pixelRow[x];
        //                 var tile = client.GetLandTile(x, y);
        //                 pixel.PackedValue = (byte)(tile.Z);
        //             }
        //         }
        //     }
        // );
        // using (var fileStream = File.OpenWrite(_heightMapPath))
        // {
        //     image.Save(fileStream, new BmpEncoder()
        //     {
        //         BitsPerPixel = BmpBitsPerPixel.Pixel8
        //     });
        // }
    }
}