using System.Numerics;
using CentrED.Client;
using CentrED.Network;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class HistoryWindow : Window
{
    public override string Name => "History";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;
    
    private int _lightLevel = 30;
    private Vector4 _virtualLayerFillColor = new(0.2f, 0.2f, 0.2f, 0.1f);
    private Vector4 _virtualLayerBorderColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private Vector4 _terrainGridFlatColor = new(0.5f, 0.5f, 0.0f, 0.5f);
    private Vector4 _terrainGridAngledColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    protected override void InternalDraw()
    {
        if (CEDClient.UndoStack.Count == 0)
        {
            ImGui.Text("History is empty.");
            return;
        }
        
        ImGui.Text($"Tasks in history: {CEDClient.UndoStack.Count}");
        
        // Create table
        ImGui.BeginTable("UndoTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit);

        // Set column widths
        ImGui.TableSetupColumn("Task", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthFixed, 350);

        // Headers
        ImGui.TableHeadersRow();

        foreach (var command in CEDClient.UndoStack)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            GetHistory(command[0], out var task, out var details);
            ImGui.Text(task);
            ImGui.TableNextColumn();
            ImGui.Text(details);
        }

        ImGui.EndTable();
        ImGui.End();
        
    }

    public void GetHistory(Packet p, out string command, out string details)
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