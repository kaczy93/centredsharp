using CentrED.IO.Models;
using Hexa.NET.ImGui;

namespace CentrED.UI.Windows;

public class ExportWindow : Window
{
    public override string Name => "Export";
    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    private float _zoom = 1.0f;
    private int _width = 1920;
    private int _height = 1080;
    private string _path = "render.png";
    protected override void InternalDraw()
    {
        ImGui.Text("Resolution Quick Select"u8);
        if (ImGui.Button("4K"))
        {
            _width = 3840;
            _height = 2160;
        }
        ImGui.SameLine();
        if (ImGui.Button("8K"))
        {
            _width = 7680;
            _height = 4320;
        }
        ImGui.SameLine();
        if (ImGui.Button("16K"))
        {
            _width = 15360;
            _height = 8640;
        }
        ImGui.InputInt("Width", ref _width);
        ImGui.InputInt("Height", ref _height);
        ImGui.SliderFloat("Zoom", ref _zoom, 0.2f, 1.0f);
        ImGui.Separator();
        ImGui.InputText("File path", ref _path, 1024);
        ImGuiEx.Tooltip("It accepts only .png and .jpg files");
        var validPath = _path.EndsWith(".png") || _path.EndsWith(".jpg");
        if(!validPath)
            ImGui.BeginDisabled();
        var label = _path.EndsWith(".png") ? "Export as PNG" : _path.EndsWith(".jpg") ? "Export as JPG" : "Invalid File Foramt";
        if (ImGui.Button(label))
        {
            Application.CEDGame.MapManager.ExportImage(_path, _width, _height, _zoom);
        }
        if(!validPath)
            ImGui.EndDisabled();
    }
}