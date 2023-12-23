using System.Numerics;
using System.Text;
using CentrED.Server;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class ServerWindow : Window
{
    public override string Name => "Local Server";
    private string _configPath = Config.ServerConfigPath;
    private Vector4 _statusColor = UIManager.Red;
    private string _statusText = "Stopped";
    private StreamReader? _logReader;
    private StringBuilder _log = new();
    private const int LOG_BUFFER_SIZE = 10000;

    protected override void InternalDraw()
    {
        ImGui.InputText("Config File", ref _configPath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            ImGui.OpenPopup("open-file");
        }
        var isOpen = true;
        if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar))
        {
            var picker = FilePicker.GetFilePicker(this, Environment.CurrentDirectory, ".xml");
            if (picker.Draw())
            {
                _configPath = picker.SelectedFile;
                Config.ServerConfigPath = _configPath;
                FilePicker.RemoveFilePicker(this);
            }
            ImGui.EndPopup();
        }

        if (Application.CEDServer is { Running: true })
        {
            if (ImGui.Button("Stop"))
            {
                CEDClient.Disconnect();
                Application.CEDServer.Quit = true;
                _statusColor = UIManager.Red;
                _statusText = "Stopped";
            }
        }
        else
        {
            ImGui.BeginDisabled(_statusText == "Starting");
            if (ImGui.Button("Start"))
            {
                if (Application.CEDServer != null)
                {
                    Application.CEDServer.Dispose();
                }

                _log.Clear();
                Config.ServerConfigPath = _configPath;

                new Task
                (
                    () =>
                    {
                        try
                        {
                            _statusColor = UIManager.Blue;
                            _statusText = "Starting";
                            var logWriter = new StreamWriter
                                (File.Open("cedserver.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                {
                                    AutoFlush = true
                                };
                            _logReader = new StreamReader
                                (File.Open("cedserver.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                            Application.CEDServer = new CEDServer(new[] { _configPath }, logWriter);
                            _statusColor = UIManager.Green;
                            _statusText = "Running";

                            Application.CEDServer.Run();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Server stopped");
                            Console.WriteLine(e);
                            _statusColor = UIManager.Red;
                            _statusText = "Stopped";
                        }
                    }
                ).Start();
            }
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        ImGui.TextColored(_statusColor, _statusText);

        ImGui.Separator();
        ImGui.Text("Server Log:");
        ImGui.BeginChild("ServerLogRegion");
        if (_logReader != null)
        {
            do
            {
                var line = _logReader.ReadLine();
                if (line == null)
                    break;
                _log.AppendLine(line);
                ImGui.SetScrollY(ImGui.GetScrollMaxY());
            } while (true);
        }
        if (_log.Length > LOG_BUFFER_SIZE)
        {
            _log.Remove(0, _log.Length - LOG_BUFFER_SIZE);
        }
        ImGui.TextUnformatted(_log.ToString());
        ImGui.EndChild();
    }
}