using System.Numerics;
using CentrED.Client;
using CentrED.Network;
using Hexa.NET.ImGui;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class HistoryWindow : Window
{
    public override string Name => "History";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
    
    protected override void InternalDraw()
    {
        if (CEDClient.UndoStack.Count == 0)
        {
            ImGui.Text("History is empty."u8);
            return;
        }
        
        ImGui.Text($"Tasks in history: {CEDClient.UndoStack.Count}");
        
        // Create table
        if (ImGui.BeginTable("UndoTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            // Set column widths
            ImGui.TableSetupColumn("Task", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthFixed, 350);

            // Headers
            ImGui.TableHeadersRow();

            var cnt = 0;
            foreach (var command in CEDClient.UndoStack)
            {
                cnt++;
                ImGui.TableNextRow();
                
                if (command.Length == 1)
                {
                    ImGui.TableNextColumn();
                    GetHistory(command[0], out var task, out var details);
                    ImGui.Text(task);
                    ImGui.TableNextColumn();
                    ImGui.Text(details);
                }
                else
                {
                    ImGui.TableNextColumn();
                    ImGui.Text("Task collection"u8);
                    ImGui.TableNextColumn();
                    
                    foreach (var packet in command)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        GetHistory(packet, out var task, out var details);
                        ImGui.Text($"\t {task}");
                        ImGui.TableNextColumn();
                        ImGui.Text(details);
                    }
                }

                if (cnt >= 25)
                {
                    break;
                }
            }
            
            ImGui.EndTable();
        }
        
        if (ImGui.Button("Clear"))
        {
            ImGui.OpenPopup("Clear History");
        }
        
        var open = true;
        if (ImGui.BeginPopupModal("Clear History", ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Are you sure you want to clear the history?\nThis operation cannot be undone.\n\n"u8);
            ImGui.Spacing();

            if (ImGui.Button("Yes", new Vector2(120, 0)))
            {
                CEDClient.UndoStack.Clear();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("No", new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static void GetHistory(Packet p, out string command, out string details)
    {
        details = "Error parsing packet"; // should never be shown

        switch (p)
        {
            case DrawMapPacket dmp:
            {
                command = "Drew Map";
                details = $"{dmp.TileId} at {dmp.X}, {dmp.Y}, {dmp.Z}";
                break;
            }
            case DeleteStaticPacket dsp:
            {
                command = "Added Static";
                details = $"{dsp.TileId} at {dsp.X}, {dsp.Y}, {dsp.Z}";
                break;
            }
            case InsertStaticPacket isp:
            {
                command = "Removed Static";
                details = $"{isp.TileId} at {isp.X}, {isp.Y}, {isp.Z}";
                break;
            }
            case MoveStaticPacket msp:
            {
                command = "Moved Static";
                details = $"{msp.TileId} from {msp.NewX}, {msp.NewY}, {msp.Z} to {msp.X}, {msp.Y}, {msp.Z}";
                break;
            }
            case ElevateStaticPacket esp:
            {
                command = "Elevated Static";
                details = $"{esp.TileId} at {esp.X}, {esp.Y}, {esp.NewZ} to {esp.X}, {esp.Y}, {esp.Z}";
                break;
            }
            case HueStaticPacket hsp:
            {
                command = "Hued Static";
                details = $"{hsp.TileId} at {hsp.X}, {hsp.Y}, {hsp.Z} from hue {hsp.NewHue} to hue {hsp.Hue}";
                break;
            }
            default:
            {
                command = "Unknown";
                break;
            }
        }
    }
}
