using System.Numerics;
using CentrED.Client;
using CentrED.IO.Models;
using CentrED.Network;
using CentrED.Tools;
using CentrED.Tools.LargeScale.Operations;
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

    private List<LargeScaleTool> _tools = [];
    private string[] _toolNames;

    private int _selectedToolIndex;
    private LargeScaleTool _selectedTool;
    
    private CentrEDClient _secondaryClient = new();
    private bool _secondaryClientConnectionTest;
    private string _secondaryClientUsername = "";
    private string _secondaryClientPassword = "";
    private bool _useMainClient;

    private int _mainClientActionsPerTick = 1000;
    private LargeScaleToolRunner? _runner;
    private Task? _backgroundTask;

    private string _submitMessage = "";
    private string _submitProgress = "";

    private ushort x1;
    private ushort y1;
    private ushort x2;
    private ushort y2;
    private int mode;

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
        
        DrawUI();
        
        ExecuteRunnerTicks();
    }

    private void DrawUI()
    {
        //Maybe move this to bottom?
        var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
        if (ImGui.Button(minimapWindow.Show ? "Close Minimap" : "Open Minimap"))
        {
            minimapWindow.Show = !minimapWindow.Show;
        }
        ImGui.Separator();
        ImGui.BeginDisabled(_runner != null || _backgroundTask is { IsCompleted: false });;
        ImGui.Text("Secondary client");
        ImGui.Checkbox("Use main client", ref _useMainClient);
        UIManager.Tooltip("Secondary client is highly recommended\n" +
                          "Use main client only if you cannot get second credentials\n" +
                          "'Use main client' will be slower and will make centred laggy during operation");
        if (_useMainClient)
        {
            ImGui.InputInt("Actions per tick", ref _mainClientActionsPerTick);
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            UIManager.Tooltip("Higher the value, faster the operation at the expense of application responsiveness\n" +
                              "Server side operations are not affected by this value");
        }
        else 
        {
            ImGui.PushItemWidth(160);
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
                if (_secondaryClientConnectionTest)
                {
                    ImGui.TextColored(UIManager.Green, "Connection established");
                }
                else
                {
                    ImGui.TextColored(UIManager.Red, _secondaryClient.Status);
                }
            }

        }
        
        ImGui.Separator();
        if (ImGui.BeginTable("##Table", 2, ImGuiTableFlags.BordersInner))
        {
            ImGui.TableSetupColumn("Tools", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableNextColumn();
            ImGui.Text("Tools:");
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.ListBox("##LargeScaleTools", ref _selectedToolIndex, _toolNames, _toolNames.Length))
            {
                _selectedTool = _tools[_selectedToolIndex];
            }
            ImGui.PopItemWidth();
            ImGui.TableNextColumn();
            ImGui.Text("Parameters:");
            _selectedTool.DrawUI();
            ImGui.EndTable();
        }
        
        ImGui.Separator();
        ImGui.Text("Area");
        ImGui.PushItemWidth(90);
        UIManager.InputUInt16("X1", ref x1);
        ImGui.SameLine();
        UIManager.InputUInt16("Y1", ref y1);
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos1"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x1 = tile.Tile.X;
                y1 = tile.Tile.Y;
            }
        }
        UIManager.InputUInt16("X2", ref x2);
        ImGui.SameLine();
        UIManager.InputUInt16("Y2", ref y2);
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos2"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x2 = tile.Tile.X;
                y2 = tile.Tile.Y;
            }
        }
        ImGui.PopItemWidth();
        ImGui.Separator();
        if (ImGui.Button("Submit"))
        {
            if (_useMainClient)
            { 
                var area = new AreaInfo(x1, y1, x2, y2);
                if (_selectedTool.CanSubmit(CEDClient, area, out _submitMessage))
                {
                    CEDGame.MapManager.DisableBlockLoading();
                    _runner = _selectedTool.Submit(CEDClient, area);
                }
            }
            else
            {
                _backgroundTask = Task.Run(ApplyAsync);
            }
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.Text(_submitMessage);
        ImGui.Text(_submitProgress);
    }

    private void ApplyAsync()
    {
        var client = _secondaryClient;
        client.Connect
        (
            CEDClient.Hostname,
            CEDClient.Port,
            _secondaryClientUsername,
            _secondaryClientPassword
        );
        var area = new AreaInfo(x1, y1, x2, y2);
        if (_selectedTool.CanSubmit(client, area, out _submitMessage))
        {

            var runner = _selectedTool.Submit(client, area);
            while (runner.Tick())
            {
                if (runner.Ticks % 10 == 0)
                {
                    client.Update();
                    _submitProgress = "Progress: {runner.Progress}%";
                }
            }
            _submitProgress = "Done";
        }
        client.Disconnect();
    }

    private void ExecuteRunnerTicks()
    {
        if (_useMainClient && _runner != null)
        {
            for (int i = 0; i < _mainClientActionsPerTick; i++)
            {
                if (!_runner.Tick())
                {
                    _runner = null;
                    CEDGame.MapManager.EnableBlockLoading();
                    _submitProgress = "Done";
                    return;
                }
            }
            _submitProgress = $"Progress: {_runner.Progress}%";
        }
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
                ImGui.GetColorU32(UIManager.Green)
            );
        }
    }
}