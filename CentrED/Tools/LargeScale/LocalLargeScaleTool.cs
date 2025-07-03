using CentrED.Client;
using CentrED.Network;
using CentrED.UI;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.Tools;

public abstract class LocalLargeScaleTool : LargeScaleTool
{
    private static CentrEDClient _secondaryClient = new();
    private static bool _secondaryClientConnectionTest;
    private static string _secondaryClientConnectionTestStatus = "";
    private static string _secondaryClientUsername = "";
    private static string _secondaryClientPassword = "";
    private static bool _useMainClient;

    private static Task? _submitTask;
    protected static string _submitStatus = "";
    public override string SubmitStatus => _submitStatus;

    public override bool IsRunning => _submitTask is { IsCompleted: false };

    public override void OnSelected()
    {
        _submitStatus = "";
    }

    public override bool DrawUI()
    {
        ImGui.Checkbox("Use main client", ref _useMainClient);
        if (_useMainClient)
        {
            ImGui.Text("Application will freeze for the entire operation duration");
            ImGui.Text("Performance will be worse than with second account");
        }
        else
        {
            ImGui.PushItemWidth(160);
            ImGui.Text("Secondary credentials:");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            ImGuiEx.Tooltip("You need second centred account");
            ImGui.InputText("Username", ref _secondaryClientUsername, 64);
            ImGui.InputText("Password", ref _secondaryClientPassword, 64, ImGuiInputTextFlags.Password);
            ImGui.PopItemWidth();
            if (ImGui.Button("Test connection"))
            {
                try
                {
                    _secondaryClient.Connect
                        (CEDClient.Hostname, CEDClient.Port, _secondaryClientUsername, _secondaryClientPassword);
                    _secondaryClientConnectionTest = _secondaryClient.Running;
                    _secondaryClientConnectionTestStatus = _secondaryClient.Status;
                    _secondaryClient.Disconnect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            ImGui.SameLine();
            if (_secondaryClient.Hostname != "")
            {
                ImGui.TextColored(_secondaryClientConnectionTest ? ImGuiColor.Green : ImGuiColor.Red, _secondaryClientConnectionTestStatus);
            }
        }
        ImGui.Separator();
        return DrawToolUI();
    }

    protected abstract bool DrawToolUI();

    public sealed override void Submit(RectU16 area)
    {
        if (_useMainClient)
        {
            CEDGame.MapManager.DisableBlockLoading();
            PreProcessArea(CEDClient, area);
            ProcessArea(CEDClient, area);   
            PostProcessArea(CEDClient, area);
            CEDGame.MapManager.EnableBlockLoading();
            _submitStatus = "Done";
        }
        else
        {
            _submitTask = Task.Run
            (() =>
                {
                    _secondaryClient.Connect(CEDClient.Hostname, CEDClient.Port, _secondaryClientUsername, _secondaryClientPassword);
                    PreProcessArea(_secondaryClient, area);
                    ProcessArea(_secondaryClient, area);
                    PostProcessArea(_secondaryClient, area);
                    _secondaryClient.Disconnect();
                    _submitStatus = "Done";
                }
            );
        }
    }

    protected virtual void PreProcessArea(CentrEDClient client, RectU16 area)
    {
        client.LoadBlocks(area);
    }

    private void ProcessArea(CentrEDClient client, RectU16 area)
    {
        double totalTiles = area.Width * area.Height;
        var processedTiles = 0;
        foreach (var (x,y) in new TileRange(area))
        {
            ProcessTile(client, x, y);
            processedTiles++;
            if (processedTiles % 10 == 0)
            {
                var progress = processedTiles / totalTiles * 100;
                _submitStatus = $"{progress:F0}%";
                client.Update();
            }
        }
    }

    protected abstract void ProcessTile(CentrEDClient client, ushort x, ushort y);

    protected virtual void PostProcessArea(CentrEDClient client, RectU16 area)
    {
        
    }
}