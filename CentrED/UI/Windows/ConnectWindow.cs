using System.Net.Sockets;
using System.Numerics;
using CentrED.Utils;
using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ConnectWindow : Window
{
    public override string Name => LangManager.Get(CONNECT_WINDOW) + "###Connect";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoResize;

    private const int TextInputLength = 255;
    private readonly ConfigValue<string> _hostname = App.Profile.Active.Init("hostname", "");
    private readonly ConfigValue<int> _port = App.Profile.Active.Init("port", 2597);
    private readonly ConfigValue<string> _username = App.Profile.Active.Init("username", "");
    private readonly ConfigValue<string> _passwordEncrypted = App.Profile.Active.Init("password", "");
    private string _password;
    private readonly ConfigValue<string> _clientPath = App.Profile.Active.Init("client_path", "");
    private bool _showPassword;
    private bool _buttonDisabled;
    internal string Info = "Not Connected";
    internal Vector4 InfoColor = ImGuiColor.Red;
    private string _profileName = "";

    public ConnectWindow() : base(true)
    {
        _password = PasswordCrypter.Decrypt(_passwordEncrypted.Value);
    }

    protected override void InternalDraw()
    {
        var profileIndex = App.Profile.Names.IndexOf(App.Profile.ActiveName.Value);
        
        ImGui.SetWindowSize(Name, new Vector2(510, 250));
        if (ImGui.Combo("###" + LangManager.Get(PROFILE), ref profileIndex, App.Profile.Names, App.Profile.Names.Length))
        {
            if (profileIndex < App.Profile.Names.Length)
            {
                App.Profile.ChangeActive(profileIndex);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("+"))
        {
            _profileName = "";
            ImGui.OpenPopup("NewProfile");
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(App.Profile.Names.Length <= 1);
        if(ImGuiEx.ConfirmButton("-", $"Are you sure you want to delete profile {App.Profile.ActiveName.Value}?"))
        {
            App.Profile.Remove();
        }
        ImGui.EndDisabled();

        if (ImGuiEx.TextInputPopup("NewProfile", ref _profileName))
        {
            App.Profile.Add(_profileName);
        }
        
        ImGui.NewLine();
        ImGui.InputText(LangManager.Get(HOSTNAME), ref _hostname.Value, TextInputLength);
        ImGui.InputInt(LangManager.Get(PORT), ref _port.Value);
        ImGui.InputText(LangManager.Get(USERNAME), ref _username.Value, TextInputLength);

        if (ImGui.InputText
            (
                LangManager.Get(PASSWORD),
                ref _password,
                TextInputLength,
                _showPassword ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password
            ))
        {
            _passwordEncrypted.Value = PasswordCrypter.Encrypt(_password);
        }
        ImGui.SameLine();
        if (ImGui.Button(_showPassword ? LangManager.Get(HIDE) : LangManager.Get(SHOW)))
        {
            _showPassword = !_showPassword;
        }
        ImGui.InputText(LangManager.Get(UO_DIRECTORY), ref _clientPath.Value, TextInputLength);
        
        var hostname = _hostname.Value;
        var port = _port.Value;
        var username = _username.Value;
        var clientPath = _clientPath.Value;
        
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            var defaultPath = clientPath.Length == 0 ? Environment.CurrentDirectory : clientPath;
            if (TinyFileDialogs.TrySelectFolder(LangManager.Get(SELECT_DIRECTORY), defaultPath, out var newPath))
            {
                _clientPath.Value = newPath;
            }
        }
        ImGui.TextColored(InfoColor, Info);
        ImGui.BeginDisabled
        (
            hostname.Length == 0 || _password.Length == 0 || username.Length == 0 || clientPath.Length == 0 || _buttonDisabled
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
                            App.Client.Connect(hostname, port, username, _password);
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