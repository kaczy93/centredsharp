using System.Numerics;
using System.Text;
using CentrED.Server;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class ServerWindow : Window
{
    public override string Name => "Local Server";
    private string _configPath = Config.Instance.ServerConfigPath;
    private Vector4 _statusColor = UIManager.Red;
    private string _statusText = "Stopped";
    private StreamReader? _logReader;
    private StringBuilder _log = new();
    private const int LOG_BUFFER_SIZE = 10000;
    private Server.Config.ConfigRoot? _config;

    public ServerWindow()
    {
        TryReadConfigFile();
    }

    private bool TryReadConfigFile()
    {
        try
        {
            try
            {
                _config = Server.Config.ConfigRoot.Read(_configPath);
                Config.Instance.ServerConfigPath = _configPath;
                _log.Clear();
                _log.Append("Config file valid.");
            }
            catch (InvalidOperationException e)
            {
                _log.Clear();
                _log.Append(e);
                throw;
            }
        }
        catch (Exception)
        {
            _config = null;
            return false;
        }
        return true;
    }

    protected override void InternalDraw()
    {
        if (ImGui.InputText("Config File", ref _configPath, 512))
        {
            TryReadConfigFile();
        }
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
                TryReadConfigFile();
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
            ImGui.BeginDisabled(_statusText == "Starting" || _config == null);
            if (ImGui.Button("Start"))
            {
                if (Application.CEDServer != null)
                {
                    Application.CEDServer.Dispose();
                }

                _log.Clear();
                Config.Instance.ServerConfigPath = _configPath;

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
                            Application.CEDServer = new CEDServer(_config!, logWriter);
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
        if (ImGui.BeginChild("ServerLogRegion"))
        {
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
        }
        ImGui.EndChild();
    }
}