using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class LSOWindow : Window
{
    public override string Name => "Large Scale Operations";

    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    internal int x1;
    internal int y1;
    internal int x2;
    internal int y2;
    private int mode;

    private string landIdsText = "";
    // private ushort[] landIds;

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        var minimapWindow = CEDGame.UIManager.MinimapWindow;
        if (ImGui.Button(minimapWindow.Show ? "Close Minimap" : "Open Minimap"))
        {
            minimapWindow.Show = !minimapWindow.Show;
        }
        ImGui.Separator();
        ImGui.Text("Area");
        ImGui.PushItemWidth(90);
        ImGui.InputInt("X1", ref x1);
        ImGui.SameLine();
        ImGui.InputInt("Y1", ref y1);
        ImGui.SameLine();
        if (ImGui.Button("Current pos##pos1"))
        {
            var pos = CEDGame.MapManager.Position;
            x1 = pos.X;
            y1 = pos.Y;
        }
        ImGui.InputInt("X2", ref x2);
        ImGui.SameLine();
        ImGui.InputInt("Y2", ref y2);
        ImGui.SameLine();
        if (ImGui.Button("Current pos##pos2"))
        {
            
            var pos = CEDGame.MapManager.Position;
            x2 = pos.X;
            y2 = pos.Y;
        }
        ImGui.PopItemWidth();
        ImGui.Separator();
        ImGui.RadioButton("Copy/Move", ref mode, 0);
        ImGui.RadioButton("Elevate", ref mode, 1);
        ImGui.RadioButton("Land", ref mode, 2);
        ImGui.RadioButton("Add Statics", ref mode, 3);
        ImGui.RadioButton("Remove Statics", ref mode, 4);
        ImGui.Separator();
        ImGui.Text("Parameters");
        if (mode == 2)
        {
            ImGui.InputText("ids", ref landIdsText, 1024);
        }
        if (ImGui.Button("Submit"))
        {
            if (mode == 2)
            {
                CEDClient.Send(new LargeScaleOperationPacket([new AreaInfo((ushort)x1,(ushort)y1,(ushort)x2,(ushort)y2)], new LSODrawLand(landIdsText.Split(',').Select(ushort.Parse).ToArray())));
            }
        }
    }
}