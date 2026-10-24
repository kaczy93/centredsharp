using System.Net.Sockets;
using System.Numerics;
using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ConnectWindow() : Window(true)
{
    public override string Name => LangManager.Get(CONNECT_WINDOW) + "###Connect";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoResize;

    private const int TextInputLength = 255;
    private bool _showPassword;
    private bool _buttonDisabled;
    internal string Info = "Not Connected";
    internal Vector4 InfoColor = ImGuiColor.Red;
    
    protected override void InternalDraw()
    {
        var profileIndex = App.Profile.ProfileNames.IndexOf(App.Profile.ActiveName.Value);
        
        ImGui.SetWindowSize(Name, new Vector2(510, 250));
        if (ImGui.Combo(LangManager.Get(PROFILE), ref profileIndex, App.Profile.ProfileNames, App.Profile.ProfileNames.Length + 1))
        {
            App.Profile.ActiveName.Value = App.Profile.ProfileNames[profileIndex];
        }
        ImGui.SameLine();
        
        var profile = App.Profile.Active;
        
        ImGui.NewLine();
        ImGui.InputText(LangManager.Get(HOSTNAME), ref profile.Hostname.Value, TextInputLength);
        ImGui.InputInt(LangManager.Get(PORT), ref profile.Port.Value);
        ImGui.InputText(LangManager.Get(USERNAME), ref profile.Username.Value, TextInputLength);

        ImGui.InputText
        (
            LangManager.Get(PASSWORD),
            ref profile.Password.Value,
            TextInputLength,
            _showPassword ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password
        );
        ImGui.SameLine();
        if (ImGui.Button(_showPassword ? LangManager.Get(HIDE) : LangManager.Get(SHOW)))
        {
            _showPassword = !_showPassword;
        }
        ImGui.InputText(LangManager.Get(UO_DIRECTORY), ref profile.ClientPath.Value, TextInputLength);
        
        var hostname = profile.Hostname.Value;
        var port = profile.Port.Value;
        var username = profile.Username.Value;
        var password = profile.Password.Value;
        var clientPath = profile.ClientPath.Value;
        
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            var defaultPath = clientPath.Length == 0 ? Environment.CurrentDirectory : clientPath;
            if (TinyFileDialogs.TrySelectFolder(LangManager.Get(SELECT_DIRECTORY), defaultPath, out var newPath))
            {
                clientPath = newPath;
            }
        }
        ImGui.TextColored(InfoColor, Info);
        ImGui.BeginDisabled
        (
            hostname.Length == 0 || password.Length == 0 || username.Length == 0 || clientPath.Length == 0 || _buttonDisabled
        );
        if (App.Client.Running)
        {
            if (ImGui.Button(LangManager.Get(DISCONNECT)))
            {
                App.Client.Disconnect();
            }
        }
        else
        {
            if (ImGui.Button(LangManager.Get(CONNECT)) || ImGui.IsWindowFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
            {
                _buttonDisabled = true;
                new Task
                (
                    () =>
                    {
                        try
                        {
                            InfoColor = ImGuiColor.Blue;
                            Info = LangManager.Get(LOADING);
                            App.MapManager.Load(clientPath);
                            Info = LangManager.Get(CONNECTING);
                            App.Client.Connect(hostname, port, username, password);
                        }
                        catch (SocketException)
                        {
                            Info = LangManager.Get(UNABLE_TO_CONNECT);
                            InfoColor = ImGuiColor.Red;
                        }
                        catch (Exception e)
                        {
                            Info = string.Format(LangManager.Get(UNKNOWN_ERROR_1NAME), e.GetType().Name);
                            InfoColor = ImGuiColor.Red;
                            Console.WriteLine(e);
                        }
                        finally
                        {
                            _buttonDisabled = false;
                        }
                    }
                ).Start();
            }
        }
        if (App.Client.Status != "")
        {
            Info = App.Client.Status;
            InfoColor = App.Client.Running ? ImGuiColor.Green : ImGuiColor.Red;
        }
        ImGui.EndDisabled();
    }
}