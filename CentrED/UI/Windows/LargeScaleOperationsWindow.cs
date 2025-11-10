using System.Numerics;
using CentrED.Network;
using CentrED.Tools;
using CentrED.Tools.LargeScale.Operations;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class LSOWindow : Window
{
    public override string Name => LangManager.Get(LARGE_SCALE_OPRATIONS_WINDOW) + "###LargeScaleOperations";

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
        _tools.Add(new InsertObjects());
        _tools.Add(new DeleteObjects());
        _tools.Add(new SetAltitude());
        _tools.Add(new ExportHeightmap());
        _tools.Add(new ImportHeightmap());
        _tools.Add(new SaveBlueprint());
        
        _toolNames = _tools.Select(t => t.Name).ToArray();
        _selectedTool = _tools[_selectedToolIndex];
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(NOT_CONNECTED));
            return;
        }
        
        var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
        if (ImGui.Button(LangManager.Get(minimapWindow.Show ? CLOSE_MINIMAP : OPEN_MINIMAP)))
        {
            minimapWindow.Show = !minimapWindow.Show;
        }
        ImGui.Separator();
        
        ImGui.Text(LangManager.Get(AREA));
        ImGui.PushItemWidth(90);
        if(ImGuiEx.InputUInt16("X1", ref x1, 0, (ushort)(CEDClient.WidthInTiles - 1))) 
            canSubmit = false;
        ImGui.SameLine();
        if(ImGuiEx.InputUInt16("Y1", ref y1, 0, (ushort)(CEDClient.HeightInTiles - 1))) 
            canSubmit = false;
        ImGui.SameLine();
        if (ImGui.Button(LangManager.Get(SELECTED_TILE) + "##pos1"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x1 = tile.Tile.X;
                y1 = tile.Tile.Y;
                canSubmit = false;
            }
        }
        if (ImGuiEx.InputUInt16("X2", ref x2, 0, (ushort)(CEDClient.WidthInTiles - 1)))
            canSubmit = false;
        ImGui.SameLine();
        if (ImGuiEx.InputUInt16("Y2", ref y2, 0, (ushort)(CEDClient.HeightInTiles - 1)))
            canSubmit = false;
        ImGui.SameLine();
        if (ImGui.Button(LangManager.Get(SELECTED_TILE) + "##pos2"))
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
            ImGui.TableSetupColumn(LangManager.Get(TOOLS), ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableNextColumn();
            ImGui.Text(LangManager.Get(TOOLS));
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.ListBox("##LargeScaleTools", ref _selectedToolIndex, _toolNames, _toolNames.Length))
            {
                _selectedTool = _tools[_selectedToolIndex];
                _selectedTool.OnSelected();
                canSubmit = false;
            }
            ImGui.PopItemWidth();
            ImGui.TableNextColumn();
            ImGui.Text(LangManager.Get(PARAMETERS));
            // Provide area context to CopyMove so it can convert absolute/relative
            var areaForUi = new RectU16(x1, y1, x2, y2);
            if (_selectedTool is CopyMove cm)
            {
                cm.SetArea(areaForUi);
            }
            canSubmit &= _selectedTool.DrawUI();
            ImGui.EndTable();
        }
        ImGui.Separator();

        if (ImGui.Button(LangManager.Get(VALIDATE)))
        {
            var area = new RectU16(x1, y1, x2, y2);
            canSubmit = _selectedTool.CanSubmit(area);
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!canSubmit);
        if (ImGui.Button(LangManager.Get(SUBMIT)))
        {
            var area = new RectU16(x1, y1, x2, y2);
            _selectedTool.Submit(area);
            canSubmit = false;
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