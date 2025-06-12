using System.Net.Mime;
using CentrED.Client;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using StbImageSharp;
using ColorComponents = StbImageSharp.ColorComponents;

namespace CentrED.UI.Windows;

public class HeightmapWindow(GraphicsDevice gd) : Window
{
    private string _heightMapPath = "";
    private ImageResult _heightMap;

    private string taskStatus = "";
    public override string Name => "Heightmap";

    protected override void InternalDraw()
    {
        ImGui.InputText("File", ref _heightMapPath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile
                    ("Select file", Environment.CurrentDirectory, ["*.bmp"], null, false, out var newPath))
            {
                _heightMapPath = newPath;
            }
        }
        if (ImGui.Button("Load"))
        {
            using (var fileStream = File.OpenRead(_heightMapPath))
            {
                // var imageInfo = ImageInfo.FromStream(fileStream);
                _heightMap = ImageResult.FromStream(fileStream, ColorComponents.Grey);
            }
        }
        ImGui.BeginDisabled(!_heightMapPath.ToLower().EndsWith(".bmp"));
        if (ImGui.Button("Export"))
        {
            ExportHeightMap();
        }
        ImGui.EndDisabled();
        ImGui.BeginDisabled(_heightMap == null);
        if (ImGui.Button("Import"))
        {
            Task.Run(ImportHeightMap);
        }
        ImGui.EndDisabled();
        
        ImGui.Text(taskStatus);
        ImGui.Text($"Enqueued: {Application.ClientPacketQueue.Count}");
    }

    private unsafe sbyte ToSByte(byte value)
    {
        return (sbyte)(*(sbyte*)&value - 128);
    }

    private unsafe byte ToByte(sbyte value)
    {
        return (byte)(*(byte*)&value + 128);
    }

    private void ExportHeightMap()
    {
        var client = Application.CEDClient;
        var imageWidth = client.Width * 8;
        var imageHeight = client.Height * 8;
        client.ResizeCache(client.Width * client.Height + 1);
        Application.CEDGame.MapManager.DisableBlockLoading();
        client.LoadBlocks(new AreaInfo(0, 0, (ushort)(imageWidth - 1), (ushort)(imageHeight - 1)));
        Application.CEDGame.MapManager.EnableBlockLoading();
        using Image<L8> image = new(imageWidth, imageHeight);
        image.ProcessPixelRows
        (accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<L8> pixelRow = accessor.GetRowSpan(y);
                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref L8 pixel = ref pixelRow[x];
                        var tile = client.GetLandTile(x, y);
                        pixel.PackedValue = ToByte(tile.Z);
                    }
                }
            }
        );
        using (var fileStream = File.OpenWrite(_heightMapPath))
        {
            image.Save(fileStream, new BmpEncoder()
            {
                BitsPerPixel = BmpBitsPerPixel.Pixel8
            });
        }
    }

    private void ImportHeightMap()
    {
        for (ushort x = 0; x < _heightMap.Width; x++)
        {
            for (ushort y = 0; y < _heightMap.Height; y++)
            {
                var pixel = _heightMap.Data[y * _heightMap.Width + x];
                var newZ = ToSByte(pixel);
                var tileId = Random.Shared.Next(0x245, 0x249);
                Application.ClientPacketQueue.Enqueue(new DrawMapPacket(x, y, newZ, (ushort)tileId));
                taskStatus = $"{x},{y}";
            }
        }
        taskStatus = "Done";
    }
}