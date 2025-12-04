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

    protected override void InternalDraw()
    {
        ImGui.Text(LangManager.Get(RESOLUTION_QUICK_SELECT));
        var mapManager = Application.CEDGame.MapManager;
        if (ImGui.Button("4K"))
        {
            mapManager.ExportWidth = 3840;
            mapManager.ExportHeight = 2160;
        }
        ImGui.SameLine();
        if (ImGui.Button("8K"))
        {
            mapManager.ExportWidth = 7680;
            mapManager.ExportHeight = 4320;
        }
        ImGui.SameLine();
        if (ImGui.Button("16K"))
        {
            mapManager.ExportWidth = 15360;
            mapManager.ExportHeight = 8640;
        }
        ImGui.InputInt(LangManager.Get(WIDTH), ref mapManager.ExportWidth);
        ImGui.InputInt(LangManager.Get(HEIGHT), ref mapManager.ExportHeight);
        ImGui.SliderFloat(LangManager.Get(ZOOM), ref mapManager.ExportZoom, 0.2f, 1.0f);
        ImGui.Separator();
        ImGui.InputText(LangManager.Get(FILE_PATH), ref mapManager.ExportPath, 1024);
        ImGuiEx.Tooltip(LangManager.Get(EXPORT_FILE_TOOLTIP));
        var path = mapManager.ExportPath;
        var validPath = path.EndsWith(".png") || path.EndsWith(".jpg");
        ImGui.BeginDisabled(!validPath);
        if (ImGui.Button(LangManager.Get(EXPORT)))
        {
            mapManager.Export = true;
        }
        if (!validPath)
        {
            ImGui.SameLine();
            ImGui.Text(LangManager.Get(UNKNOWN_FILE_FORMAT));
        }
        ImGui.EndDisabled();
    }
}