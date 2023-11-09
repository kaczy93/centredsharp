using System.Numerics;
using System.Text;
using CentrED.Server;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows; 

public class ServerWindow : Window {
    public override string Name => "Local Server";
    private string _configPath = Config.ServerConfigPath;
    private Vector4 _statusColor = UIManager.Red;
    private string _statusText = "Stopped"
;    private StreamReader? _logReader;
    private StringBuilder _log = new();

    public override void Draw() {
        if (!Show) return;

        ImGui.Begin(Name, ref _show );
        ImGui.InputText("Config File", ref _configPath, 512);
        ImGui.SameLine();
        if (ImGui.Button("...")) {
            ImGui.OpenPopup("open-file");
        }
        var isOpen = true;
        if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar)) {
            var picker = FilePicker.GetFilePicker(this, Environment.CurrentDirectory, ".xml");
            if (picker.Draw()) {
                _configPath = picker.SelectedFile;
                Config.ServerConfigPath = _configPath;
                FilePicker.RemoveFilePicker(this);
            }
            ImGui.EndPopup();
        }

        if (Application.CEDServer != null && Application.CEDServer.Running) {
            if (ImGui.Button("Stop")) {
                CEDClient.Disconnect();
                Application.CEDServer.Quit = true;
            }
        }
        else {
            if (ImGui.Button("Start")) {
                if (Application.CEDServer != null) {
                    Application.CEDServer.Dispose();
                }

                _log.Clear();
                Config.ServerConfigPath = _configPath;
                
                new Task(() => {
                    try {
                        _statusColor = UIManager.Blue;
                        _statusText = "Starting";
                        var logWriter =
                            new StreamWriter(File.Open("cedserver.log", FileMode.Create, FileAccess.Write,
                                FileShare.ReadWrite)) { AutoFlush = true };
                        _logReader = new StreamReader(File.Open("cedserver.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                        Application.CEDServer = new CEDServer(new[] { _configPath }, logWriter);
                        _statusColor = UIManager.Green;
                        _statusText = "Running";
                        
                        Application.CEDServer.Run();
                    }
                    catch (Exception e) {
                        Console.WriteLine("Server stopped");
                        Console.WriteLine(e);
                        _statusColor = UIManager.Red;
                        _statusText = "Stopped";
                    }
                }).Start();
            }
        }
        ImGui.SameLine();
        ImGui.TextColored(_statusColor, _statusText);
        
        ImGui.Separator();
        ImGui.Text("Server Log:");
        ImGui.BeginChild("ServerLogRegion");
        if (_logReader != null) {
            do {
                var line = _logReader.ReadLine();
                if (line == null) break;
                _log.AppendLine(line);
            } while (true);
        }
        ImGui.TextUnformatted(_log.ToString());
        ImGui.EndChild();

        ImGui.End();
    }
}