using System.Numerics;
using System.Text;
using CentrED.Server;
using Hexa.NET.ImGui;
using static CentrED.CentrED;

namespace CentrED.UI.Windows;

public class ServerWindow : Window
{
    private CEDServer? _server;
    
    public override string Name => "Local Server";
    public ConfigValue<string> ConfigPath = App.Config.Init("server.config_file" ,"cedserver.xml");
    private Vector4 _statusColor = ImGuiColor.Red;
    private string _statusText = "Stopped";
    private StreamReader? _logReader;
    private StringBuilder _log = new();
    private const int LOG_BUFFER_SIZE = 10000;
    private Server.Config.ConfigRoot? _config;
    
    protected override void InternalDraw()
    {
        ImGui.InputText("Config File", ref ConfigPath.Value, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile
                    ("Select Server Config", Environment.CurrentDirectory, ["*.xml"], null, false, out var newPath))
            {
                ConfigPath.Value = newPath;
            }
        }
        if (_server is { Running: true })
        {
            if (ImGui.Button("Stop"))
            {
                App.Client.Disconnect();
                _server.Quit = true;
                _statusColor = ImGuiColor.Red;
                _statusText = "Stopped";
            }
        }
        else
        {
            ImGui.BeginDisabled(_statusText == "Starting");
            if (ImGui.Button("Start"))
            {
                if (_server != null)
                {
                    _server.Dispose();
                }

                _log.Clear();

                _config = null;
                try
                {
                    _config = Server.Config.ConfigRoot.Read(ConfigPath.Value);
                }
                catch (Exception e)
                {
                    _log.Append(e);
                    _config = null;
                }
                
                if (_config != null)
                {
                    new Task
                    (() =>
                        {
                            try
                            {
                                _statusColor = ImGuiColor.Blue;
                                _statusText = "Starting";
                                var logWriter = new StreamWriter
                                    (File.Open("cedserver.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                                    {
                                        AutoFlush = true
                                    };
                                _logReader = new StreamReader
                                    (File.Open("cedserver.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                                _server = new CEDServer(_config!, logWriter);
                                _statusColor = ImGuiColor.Green;
                                _statusText = "Running";

                                _server.Run();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Server stopped");
                                Console.WriteLine(e);
                                _statusColor = ImGuiColor.Red;
                                _statusText = "Stopped";
                            }
                        }
                    ).Start();
                }
            }
            ImGui.EndDisabled();
        }
        ImGui.SameLine();
        ImGui.TextColored(_statusColor, ImGuiEx.EscapeFormat(_statusText));

        ImGui.Separator();
        ImGui.Text("Server Log:"u8);
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