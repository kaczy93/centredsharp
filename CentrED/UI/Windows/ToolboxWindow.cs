using CentrED.IO.Models;
using CentrED.Tools;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ToolboxWindow : Window
{
    public override string Name => LangManager.Get(TOOLBOX_WINDOW) + "###Toolbox";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    protected override void InternalDraw()
    {
        App.MapManager.Tools.ForEach(ToolButton);
        ImGui.Separator();
        ImGui.Text(LangManager.Get(PARAMETERS));
        if (ImGui.BeginChild("ToolOptionsContainer", new System.Numerics.Vector2(-1, -1), ImGuiChildFlags.Borders))
        {
            var tool = App.MapManager.ActiveTool;
            tool.Draw();
            tool.DrawFloatingWindow();
        }
        ImGui.EndChild();
    }

    private void ToolButton(Tool tool)
    {
        if (ImGui.RadioButton(tool.Name, App.MapManager.ActiveTool == tool))
        {
            App.MapManager.ActiveTool = tool;
        }
        if (tool.Shortcut != Keys.None)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(tool.Shortcut.ToString());
        }
    }
}