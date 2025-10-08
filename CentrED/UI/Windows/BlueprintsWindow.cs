using System.Numerics;
using Hexa.NET.ImGui;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class BlueprintsWindow : Window
{
    public override string Name => "Blueprints";

    private uint _selectedId;
    public uint SelectedId => _selectedId;
    
    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text("Not connected"u8);
            return;
        }
        
        var isTemplates = false;
        ImGuiEx.TwoWaySwitch("Multis", "Templates", ref isTemplates);
        
         if (ImGui.BeginChild("Multis", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("MultisTable", 2) && CEDClient.Running)
            {
                var textSize = ImGui.CalcTextSize("0x0000");
                var clipper = ImGui.ImGuiListClipper();
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, textSize.X);
                var ids = CEDGame.MapManager.BlueprintManager.ValidMultiIds;
                clipper.Begin(ids.Length, textSize.Y);
                while (clipper.Step())
                {
                    for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                    {
                        var index = ids[rowIndex];
                        var posY = ImGui.GetCursorPosY();
                        DrawMultiRow(index);
                        ImGui.SetCursorPosY(posY);
                        if (ImGui.Selectable
                            (
                                $"##multi{index}",
                                _selectedId == index,
                                ImGuiSelectableFlags.SpanAllColumns
                            ))
                        {
                            _selectedId = index;
                        }
                    }
                }
                clipper.End();
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }
    
    private void DrawMultiRow(uint index)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.None);
        if (ImGui.TableNextColumn())
        {
            ImGui.Text($"0x{index:X4}");
        }
        if (ImGui.TableNextColumn())
        {
            ImGui.TextUnformatted(CEDGame.MapManager.BlueprintManager.GetName(index));
        }   
    }
}