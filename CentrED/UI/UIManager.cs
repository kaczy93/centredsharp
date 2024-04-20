using CentrED.IO.Models;
using CentrED.Map;
using CentrED.Renderer;
using CentrED.UI.Windows;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI;

public class UIManager
{
    public static Vector4 Red = new(1, 0, 0, 1);
    public static Vector4 Green = new(0, 1, 0, 1);
    public static Vector4 Blue = new(0, 0, 1, 1);

    internal UIRenderer _uiRenderer;
    internal GraphicsDevice _graphicsDevice;

    private int _scrollWheelValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _allKeys = Enum.GetValues<Keys>();

    internal InfoWindow InfoWindow;
    internal ToolboxWindow ToolboxWindow;
    internal TilesWindow TilesWindow;
    internal LandBrushWindow LandBrushWindow;
    internal HuesWindow HuesWindow;
    internal FilterWindow FilterWindow;
    internal DebugWindow DebugWindow;

    internal List<Window> MainWindows = new();
    internal List<Window> ToolsWindows = new();

    public UIManager(GraphicsDevice gd)
    {
        _graphicsDevice = gd;
        _uiRenderer = new UIRenderer(_graphicsDevice);

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.GetIO().ConfigInputTrickleEventQueue = false;
        if (!File.Exists("imgui.ini") && File.Exists("imgui.ini.default"))
        {
            ImGui.LoadIniSettingsFromDisk("imgui.ini.default");
        }

        TextInputEXT.TextInput += c =>
        {
            if (c == '\t')
                return;

            ImGui.GetIO().AddInputCharacter(c);
        };

        _uiRenderer.RebuildFontAtlas();

        MainWindows.Add(new ConnectWindow());
        MainWindows.Add(new ServerWindow());
        MainWindows.Add(new OptionsWindow());
        // MainWindows.Add(new ExportWindow());

        InfoWindow = new InfoWindow();
        ToolboxWindow = new ToolboxWindow();
        TilesWindow = new TilesWindow();
        LandBrushWindow = new LandBrushWindow();
        HuesWindow = new HuesWindow();
        FilterWindow = new FilterWindow();
        ToolsWindows.Add(InfoWindow);
        ToolsWindows.Add(ToolboxWindow);
        ToolsWindows.Add(TilesWindow);
        ToolsWindows.Add(LandBrushWindow);
        ToolsWindows.Add(HuesWindow);
        ToolsWindows.Add(FilterWindow);
        ToolsWindows.Add(new MinimapWindow());
        ToolsWindows.Add(new HistoryWindow());

        DebugWindow = new DebugWindow();
    }
    
    public bool CapturingMouse => ImGui.GetIO().WantCaptureMouse;
    public bool CapturingKeyboard => ImGui.GetIO().WantCaptureKeyboard;
    
    internal double FramesPerSecond;
    private bool openContextMenu;

    private bool TryMapKeys(Keys key, out ImGuiKey imguikey)
    {
        //Special case not handed in the switch...
        //If the actual key we put in is "None", return none and true. 
        //otherwise, return none and false.
        if (key == Keys.None)
        {
            imguikey = ImGuiKey.None;
            return true;
        }

        imguikey = key switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
            Keys.Multiply => ImGuiKey.KeypadMultiply,
            Keys.Add => ImGuiKey.KeypadAdd,
            Keys.Subtract => ImGuiKey.KeypadSubtract,
            Keys.Decimal => ImGuiKey.KeypadDecimal,
            Keys.Divide => ImGuiKey.KeypadDivide,
            >= Keys.F1 and <= Keys.F12 => ImGuiKey.F1 + (key - Keys.F1),
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.Scroll => ImGuiKey.ScrollLock,
            Keys.LeftShift => ImGuiKey.ModShift,
            Keys.RightShift => ImGuiKey.RightShift,
            Keys.LeftControl => ImGuiKey.ModCtrl,
            Keys.RightControl => ImGuiKey.RightCtrl,
            Keys.LeftAlt => ImGuiKey.ModAlt,
            Keys.RightAlt => ImGuiKey.LeftAlt,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            Keys.OemPlus => ImGuiKey.Equal,
            Keys.OemComma => ImGuiKey.Comma,
            Keys.OemMinus => ImGuiKey.Minus,
            Keys.OemPeriod => ImGuiKey.Period,
            Keys.OemQuestion => ImGuiKey.Slash,
            Keys.OemTilde => ImGuiKey.GraveAccent,
            Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            Keys.OemCloseBrackets => ImGuiKey.RightBracket,
            Keys.OemPipe => ImGuiKey.Backslash,
            Keys.OemQuotes => ImGuiKey.Apostrophe,
            _ => ImGuiKey.None,
        };

        return imguikey != ImGuiKey.None;
    }

    public void Update(GameTime gameTime, bool isActive)
    {
        Metrics.Start("UpdateUI");
        var io = ImGui.GetIO();

        io.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        io.AddMousePosEvent(mouse.X, mouse.Y);
        if (isActive)
        {
            io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
            io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

            io.AddMouseWheelEvent(0, (mouse.ScrollWheelValue - _scrollWheelValue) / WHEEL_DELTA);
            _scrollWheelValue = mouse.ScrollWheelValue;

            foreach (var key in _allKeys)
            {
                if (TryMapKeys(key, out ImGuiKey imguikey))
                {
                    io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
                }
            }
        }

        io.DisplaySize = new Vector2
        (
            _graphicsDevice.PresentationParameters.BackBufferWidth,
            _graphicsDevice.PresentationParameters.BackBufferHeight
        );
        io.DisplayFramebufferScale = new Vector2(1f, 1f);
        Metrics.Stop("UpdateUI");
    }

    public void Draw(GameTime gameTime)
    {
        Metrics.Start("DrawUI");
        FramesPerSecond = 1 / gameTime.ElapsedGameTime.TotalSeconds;
        _graphicsDevice.SetRenderTarget(null);
        ImGui.NewFrame();
        DrawUI();

        ImGui.Render();
        _uiRenderer.RenderDrawData(ImGui.GetDrawData());
        Metrics.Stop("DrawUI");
    }

    public void OpenContextMenu()
    {
        openContextMenu = true;
    }

    private bool _resetLayout;

    protected virtual void DrawUI()
    {
        ShowCrashInfo();
        if (CEDGame.Closing)
            return;
        if (_resetLayout)
        {
            ImGui.LoadIniSettingsFromDisk("imgui.ini.default");
            Config.Instance.Layout = new Dictionary<string, WindowState>();
            _resetLayout = false;
        }
        ImGui.DockSpaceOverViewport
        (
            ImGui.GetMainViewport(),
            ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode
        );
        DrawContextMenu();
        DrawMainMenu();
        MainWindows.ForEach(w => w.Draw());
        ToolsWindows.ForEach(w => w.Draw());
        DebugWindow.Draw();
    }

    private void DrawContextMenu()
    {
        if (openContextMenu)
        {
            ImGui.OpenPopup("MainPopup");
            openContextMenu = false;
        }
        if (ImGui.BeginPopup("MainPopup"))
        {
            var selected = CEDGame.MapManager.Selected;
            if (selected != null)
            {
                if (ImGui.Button("Grab TileId"))
                {
                    TilesWindow.UpdateSelectedId(selected);
                    ImGui.CloseCurrentPopup();
                }
                if (selected is StaticObject so)
                {
                    if (ImGui.Button("Grab Hue"))
                    {
                        HuesWindow.UpdateSelectedHue(so.StaticTile.Hue);
                        ImGui.CloseCurrentPopup();
                    }
                    if (ImGui.Button("Filter TileId"))
                    {
                        if (!CEDGame.MapManager.StaticFilterIds.Add(so.Tile.Id))
                            CEDGame.MapManager.StaticFilterIds.Remove(so.Tile.Id);
                        ImGui.CloseCurrentPopup();
                    }
                }
            }
            else
            {
                ImGui.Text("Nothing to see here");
            }
            ImGui.EndPopup();
        }
    }

    private void DrawMainMenu()
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("CentrED"))
            {
                MainWindows.ForEach(w => w.DrawMenuItem());
                ImGui.Separator();
                if (ImGui.MenuItem("Quit"))
                    CEDGame.Exit();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo", "Ctrl+Z"))
                {
                    CEDClient.Undo();
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Reset Zoom", "ESC"))
                {
                    CEDGame.MapManager.Camera.ResetZoom();
                }
                ImGui.MenuItem("Walkable Surfaces", "Ctrl + W", ref CEDGame.MapManager.WalkableSurfaces);
                if (ImGui.BeginMenu("Flat View"))
                {
                    if (ImGui.MenuItem("Enabled", "Ctrl + F", ref CEDGame.MapManager.FlatView));
                    {
                        CEDGame.MapManager.UpdateAllTiles();
                    }
                    if (ImGui.MenuItem("Flat statics", "Ctrl + S", ref CEDGame.MapManager.FlatStatics))
                    {
                        CEDGame.MapManager.UpdateAllTiles();
                    }
                    ImGui.EndMenu();
                }
                ImGui.MenuItem("Animated Statics", "Ctrl + A", ref CEDGame.MapManager.AnimatedStatics);
                ImGui.MenuItem("Show Grid", "Ctrl + G", ref CEDGame.MapManager.ShowGrid);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Tools"))
            {
                ToolsWindows.ForEach(w => w.DrawMenuItem());
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("Reset layout", File.Exists("imgui.ini.default")))
                {
                    _resetLayout = true;
                }
                if (ImGui.MenuItem("Clear cache", "CTRL+R"))
                {
                    CEDGame.MapManager.Reset();
                }
                //Credits
                //About
                ImGui.Separator();
                DebugWindow.DrawMenuItem();
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    internal void DrawImage(Texture2D tex, Rectangle bounds)
    {
        DrawImage(tex, bounds, new Vector2(bounds.Width, bounds.Height));
    }

    internal void DrawImage(Texture2D tex, Rectangle bounds, Vector2 size)
    {
        var texPtr = _uiRenderer.BindTexture(tex);
        var fWidth = (float)tex.Width;
        var fHeight = (float)tex.Height;
        var uv0 = new Vector2(bounds.X / fWidth, bounds.Y / fHeight);
        var uv1 = new Vector2((bounds.X + bounds.Width) / fWidth, (bounds.Y + bounds.Height) / fHeight);
        ImGui.Image(texPtr, size, uv0, uv1);
    }

    public static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }

    public static bool TwoWaySwitch(string leftLabel, string rightLabel, ref bool value)
    {
        ImGui.Text(leftLabel);
        ImGui.SameLine();
        var pos = ImGui.GetCursorPos();
        var wpos = ImGui.GetCursorScreenPos();
        if (value)
            wpos.X += 40;
        var result = ImGui.Button(" ", new Vector2(80, 18)); //Just empty label makes button non functional
        if (result)
        {
            value = !value;
        }
        ImGui.SetCursorPos(pos);
        ImGui.GetWindowDrawList().AddRectFilled
            (wpos, wpos + new Vector2(40, 18), ImGui.GetColorU32(new Vector4(.8f, .8f, 1, 0.5f)));
        ImGui.SameLine();
        ImGui.Text(rightLabel);
        return result;
    }

    private bool _showCrashPopup;
    private string _crashText = "";

    public void ReportCrash(Exception exception)
    {
        _showCrashPopup = true;
        _crashText = exception.ToString();
        CEDGame.Closing = true;
    }

    public void ShowCrashInfo()
    {
        if (CEDGame.Closing)
        {
            ImGui.Begin("Crash");
            ImGui.OpenPopup("CrashWindow");
            if (ImGui.BeginPopupModal
                (
                    "CrashWindow",
                    ref _showCrashPopup,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text("Application crashed");
                ImGui.InputTextMultiline
                    (" ", ref _crashText, 1000, new Vector2(800, 150), ImGuiInputTextFlags.ReadOnly);
                if (ImGui.Button("Copy to clipboard"))
                {
                    ImGui.SetClipboardText(_crashText);
                }
                ImGui.Separator();
                if (ImGui.Button("Exit"))
                {
                    File.WriteAllText("Crash.log", _crashText);
                    CEDGame.Exit();
                }
                ImGui.EndPopup();
            }
            ImGui.End();
        }
    }

    public static void DragInt(ReadOnlySpan<char> label, ref int value, float v_speed, int v_min, int v_max)
    {
        ImGui.PushItemWidth(50);
        ImGui.DragInt($"##{label}", ref value, v_speed, v_min, v_max);
        ImGui.PopItemWidth();
        Tooltip("Drag Left/Right");
        ImGui.SameLine(0, 0);
        ImGui.PushButtonRepeat(true);
        if (ImGui.ArrowButton($"{label}down", ImGuiDir.Down))
        {
            value--;
        }
        ImGui.SameLine(0, 0);
        if (ImGui.ArrowButton($"{label}up", ImGuiDir.Up))
        {
            value++;
        }
        ImGui.PopButtonRepeat();
        ImGui.SameLine();
        ImGui.Text(label);
        value = Math.Clamp(value, v_min, v_max);
    }
}