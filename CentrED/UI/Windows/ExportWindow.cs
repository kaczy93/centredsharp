using CentrED.IO.Models;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ExportWindow : Window
{
    public override string Name => LangManager.Get(EXPORT_WINDOW) + "###Export";
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
        ImGui.Text(LangManager.Get(RESOLUTION_QUICK_SELECT));
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
        ImGui.InputInt(LangManager.Get(WIDTH), ref _width);
        ImGui.InputInt(LangManager.Get(HEIGHT), ref _height);
        ImGui.SliderFloat(LangManager.Get(ZOOM), ref _zoom, 0.2f, 1.0f);
        ImGui.Separator();
        ImGui.InputText(LangManager.Get(FILE_PATH), ref _path, 1024);
        ImGuiEx.Tooltip(LangManager.Get(EXPORT_FILE_TOOLTIP));
        var validPath = _path.EndsWith(".png") || _path.EndsWith(".jpg");
        ImGui.BeginDisabled(!validPath);
        if (ImGui.Button(LangManager.Get(EXPORT)))
        {
            Application.CEDGame.MapManager.ExportImage(_path, _width, _height, _zoom);
        }
        if (!validPath)
        {
            ImGui.SameLine();
            ImGui.Text(LangManager.Get(UNKNOWN_FILE_FORMAT));
        }
        ImGui.EndDisabled();
    }
}