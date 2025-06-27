using System.Numerics;
using CentrED.Network;
using CentrED.Tools;
using CentrED.Tools.LargeScale.Operations;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class LSOWindow : Window
{
    public override string Name => "Large Scale Operations";

    private List<LargeScaleTool> _tools = [];
    private string[] _toolNames;

    private int _selectedToolIndex;
    private LargeScaleTool _selectedTool;

    private bool canSubmit;
    
    private ushort x1;
    private ushort y1;
    private ushort x2;
    private ushort y2;

    public LSOWindow()
    {
        _tools.Add(new CopyMove());
        _tools.Add(new DrawLand());
        _tools.Add(new InsertStatics());
        _tools.Add(new RemoveStatics());
        _tools.Add(new SetAltitude());
        _tools.Add(new ExportHeightmap());
        _tools.Add(new ImportHeightmap());
        
        _toolNames = _tools.Select(t => t.Name).ToArray();
        _selectedTool = _tools[_selectedToolIndex];
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text("Not connected");
            return;
        }
        
        var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
        if (ImGui.Button(minimapWindow.Show ? "Close Minimap" : "Open Minimap"))
        {
            minimapWindow.Show = !minimapWindow.Show;
        }
        ImGui.Separator();
        
        ImGui.Text("Area");
        ImGui.PushItemWidth(90);
        if(ImGuiEx.InputUInt16("X1", ref x1)) 
            canSubmit = false;
        ImGui.SameLine();
        if(ImGuiEx.InputUInt16("Y1", ref y1)) 
            canSubmit = false;
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos1"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x1 = tile.Tile.X;
                y1 = tile.Tile.Y;
                canSubmit = false;
            }
        }
        if (ImGuiEx.InputUInt16("X2", ref x2))
            canSubmit = false;
        ImGui.SameLine();
        if (ImGuiEx.InputUInt16("Y2", ref y2))
            canSubmit = false;
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos2"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x2 = tile.Tile.X;
                y2 = tile.Tile.Y;
                canSubmit = false;
            }
        }
        ImGui.PopItemWidth();
        ImGui.Separator();
        
        ImGui.BeginDisabled(_selectedTool.IsRunning);
        if (ImGui.BeginTable("##Table", 2, ImGuiTableFlags.BordersInner))
        {
            ImGui.TableSetupColumn("Tools", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableNextColumn();
            ImGui.Text("Tools:");
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.ListBox("##LargeScaleTools", ref _selectedToolIndex, _toolNames, _toolNames.Length))
            {
                _selectedTool = _tools[_selectedToolIndex];
                _selectedTool.OnSelected();
                canSubmit = false;
            }
            ImGui.PopItemWidth();
            ImGui.TableNextColumn();
            ImGui.Text("Parameters:");
            canSubmit &= _selectedTool.DrawUI();
            ImGui.EndTable();
        }
        ImGui.Separator();

        if (ImGui.Button("Validate"))
        {
            var area = new AreaInfo(x1, y1, x2, y2);
            canSubmit = _selectedTool.CanSubmit(area);
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!canSubmit);
        if (ImGui.Button("Submit"))
        {
            var area = new AreaInfo(x1, y1, x2, y2);
            _selectedTool.Submit(area);
        }
        ImGui.EndDisabled();//canSubmit
        ImGui.EndDisabled();//IsRunning
        ImGui.SameLine();
        ImGui.Text(_selectedTool.SubmitStatus);
    }

    public void DrawArea(Vector2 currentPos)
    {
        if (!Show)
            return;
        if (x1 != 0 || y1 != 0 || x2 != 0 || y2 != 0)
        {
            ImGui.GetWindowDrawList().AddRect(
                currentPos + new Vector2(x1 / 8, y1 / 8), 
                currentPos + new Vector2(x2 / 8, y2 / 8), 
                ImGui.GetColorU32(ImGuiColor.Green)
            );
        }
    }
}