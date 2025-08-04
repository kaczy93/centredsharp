using System.Runtime.InteropServices;
using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using CentrED.Renderer;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static SDL3.SDL;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI;

public class UIManager
{
    public enum Category
    {
        Menu, 
        Main,
        Tools,
    }

    internal UIRenderer _uiRenderer;
    private GraphicsDevice _graphicsDevice;
    private GameWindow _GameWindow;
    
    private uint _MainWindowID;
    // Event handling
    private SDL_EventFilter _EventFilter;
    private SDL_EventFilter _PrevEventFilter;
    private uint _MouseWindowId;

    private static readonly string[] backendsWithGlobalMouseState = ["windows", "cocoa", "x11", "DIVE", "VMAN"];
    private readonly bool _HasCaptureAndGlobalMouse;
    private int _PrevScrollWheelValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _AllKeys = Enum.GetValues<Keys>();

    internal Dictionary<Type, Window> AllWindows = new();
    internal List<Window> MainWindows = new();
    internal List<Window> ToolsWindows = new();
    internal List<Window> MenuWindows = new();

    internal DebugWindow DebugWindow;
    public bool ShowTestWindow;
    private ImFontPtr[] _Fonts;
    public string[] FontNames { get; }
    private int _FontIndex;

    public int FontIndex
    {
        get => _FontIndex;
        set
        {
            _FontIndex = value;
            ImGui.PopFont();
            ImGui.PushFont(_Fonts[_FontIndex], Config.Instance.FontSize);
        }
    }

    public unsafe UIManager(GraphicsDevice gd, GameWindow window)
    {
        _graphicsDevice = gd;
        _GameWindow = window;

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        var sdl_backend = SDL_GetCurrentVideoDriver();
       
        _HasCaptureAndGlobalMouse = backendsWithGlobalMouseState.Contains(sdl_backend);
        
        io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
        if(_HasCaptureAndGlobalMouse)
            io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        
        ImGuiViewportPtr mainViewport = ImGui.GetMainViewport();
        mainViewport.PlatformHandle = (void*)window.Handle;

        SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
        SDL_SetHint(SDL_HINT_MOUSE_AUTO_CAPTURE, "0");
        SDL_SetHint("SDL_BORDERLESS_WINDOWED_STYLE", "0");

        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        if(Config.Instance.Viewports)
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigInputTrickleEventQueue = false;
        
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
        TextInputEXT.StartTextInput();
        
        _uiRenderer = new UIRenderer(_graphicsDevice, window);
        
        AddWindow(Category.Main, new ConnectWindow());
        AddWindow(Category.Main, new ServerWindow());
        AddWindow(Category.Main, new OptionsWindow());
        AddWindow(Category.Main, new ExportWindow());

        AddWindow(Category.Tools, new InfoWindow());
        AddWindow(Category.Tools, new ToolboxWindow());
        AddWindow(Category.Tools, new TilesWindow());
        AddWindow(Category.Tools, new LandBrushManagerWindow());
        AddWindow(Category.Tools, new HuesWindow());
        AddWindow(Category.Tools, new FilterWindow());
        AddWindow(Category.Tools, new HistoryWindow());
        AddWindow(Category.Tools, new LSOWindow());
        AddWindow(Category.Tools, new ChatWindow());
        AddWindow(Category.Tools, new ServerAdminWindow());
        
        AddWindow(Category.Menu, new MinimapWindow());
        DebugWindow = new DebugWindow();
        
        _MainWindowID = SDL_GetWindowID(window.Handle);
        // Use a filter to get SDL events for your extra window
        IntPtr prevUserData;
        SDL_GetEventFilter(
            out _PrevEventFilter,
            out prevUserData
        );
        _EventFilter = EventFilter;
        SDL_SetEventFilter(
            _EventFilter,
            prevUserData
        );
        
        LoadFonts();
        FontNames = _Fonts.Select(x => x.GetDebugNameS()).ToArray();
        var fontIndex = Array.IndexOf(FontNames, Config.Instance.FontName);
        if (fontIndex != -1)
        {
            _FontIndex = fontIndex;
        }
    }
    
    private unsafe bool EventFilter(IntPtr userdata, SDL_Event* evt)
    {
        var io = ImGui.GetIO();
        var eventType = (SDL_EventType)evt->type;
        switch (eventType)
        {
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
            {
                if (GetViewportById(evt->window.windowID) == null)
                    return false;
                var mouseX = evt->motion.x;
                var mouseY = evt->motion.y;
                if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
                {
                    SDL_GetWindowPosition(SDL_GetWindowFromID(evt->window.windowID), out var windowX, out var windowY);
                    mouseX += windowX;
                    mouseY += windowY;
                }
                io.AddMousePosEvent(mouseX, mouseY);
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
            {
                if (GetViewportById(evt->window.windowID) == null)
                    return false;
                float wheelX = -evt->wheel.x;
                float wheelY = evt->wheel.y;
                io.AddMouseWheelEvent(wheelX, wheelY);
                break;
            }
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
            {
                if (GetViewportById(evt->window.windowID) == null)
                    return false;
                var mouseButton = -1;
                if (evt->button.button == 1) { mouseButton = 0; }
                if (evt->button.button == 3) { mouseButton = 1; }
                if (evt->button.button == 2) { mouseButton = 2; }
                if (evt->button.button == 4) { mouseButton = 3; }
                if (evt->button.button == 5) { mouseButton = 4; }
                io.AddMouseButtonEvent(mouseButton, eventType == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN);
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
            {
                if (GetViewportById(evt->window.windowID) == null)
                    return false;
                _MouseWindowId = evt->window.windowID;
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
            {
                if (GetViewportById(evt->window.windowID) == null)
                    return false;
                _MouseWindowId = 0;
                //Should we defer leave until next frame?
                io.AddMousePosEvent(float.MinValue, float.MinValue);
                break;
            }
            
            case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
            {
                // This trigger back buffer resize and can cause troubles for windows that are managed by ImGui
                if (evt->window.windowID != _MainWindowID)
                    return false;
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
            {
                //This event messes with Mouse.INTERNAL_WindowWidth and Mouse.INTERNAL_WindowHeight
                //Maybe we could not filter it if FNA would start handling events that targets only main GameWindow
                if (evt->window.windowID != _MainWindowID)
                    return false;
                break;
            }
            case SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
            {
                if (evt->window.windowID == _MainWindowID)
                {
                    CEDGame.Exit();
                    return false;
                }
                break;
            }
        }
        if (_PrevEventFilter != null)
        {
            return _PrevEventFilter(userdata, evt);
        }
        return true;
    }

    private unsafe void LoadFonts()
    {
        var io = ImGui.GetIO();
        var fontFiles = Directory.GetFiles(".", "*.ttf");
        _Fonts = new ImFontPtr[fontFiles.Length + 1];
        _Fonts[0] = io.Fonts.AddFontDefault();
        var fontIndex = 1;
        foreach (var fontFile in fontFiles)
        {
            _Fonts[fontIndex] = io.Fonts.AddFontFromFileTTF(fontFile);
            fontIndex++;
        }
    }

    private unsafe ImGuiViewport* GetViewportById(uint windowId)
    {
        return ImGui.FindViewportByPlatformHandle((void*)SDL_GetWindowFromID(windowId));
    }

    public void AddWindow(Category category, Window window)
    {
        AllWindows.Add(window.GetType(), window);
        switch (category)
        {
            case Category.Main: 
                MainWindows.Add(window);
                break;
            case Category.Tools:
                ToolsWindows.Add(window);
                break;
            case Category.Menu:
                MenuWindows.Add(window); 
                break;
        }
    }
    
    public bool CapturingMouse => ImGui.GetIO().WantCaptureMouse;
    public bool CapturingKeyboard => ImGui.GetIO().WantCaptureKeyboard;
    
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
            >= Keys.D0 and <= Keys.D9 => ImGuiKey.Key0 + (key - Keys.D0),
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

    public Vector2 MaxWindowSize()
    {
        int x = 0;
        int y = 0;
        var platformIO = ImGui.GetPlatformIO();
        for (int i = 0; i < platformIO.Viewports.Size; i++)
        {
            ImGuiViewportPtr vp = platformIO.Viewports[i];
            x = Math.Max(x, (int)vp.Size.X);
            y = Math.Max(y, (int)vp.Size.Y);
        }
        return new Vector2(x, y);
    }

    public unsafe void NewFrame(GameTime gameTime, bool isActive)
    {
        Metrics.Start("NewFrameUI");
        var io = ImGui.GetIO();
        io.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _uiRenderer.NewFrame();

        var canCapture = !RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && ImGui.GetDragDropPayload() != ImGuiPayloadPtr.Null;
        if (canCapture)
            io.BackendFlags |= ImGuiBackendFlags.HasMouseHoveredViewport;
        else
            io.BackendFlags &= ~ImGuiBackendFlags.HasMouseHoveredViewport;

        if (_HasCaptureAndGlobalMouse)
        {
            var want_capture = false;
            for(var button = 0; button < (int)ImGuiMouseButton.Count && !want_capture; button++)
                if(ImGui.IsMouseDragging((ImGuiMouseButton)button, 1.0f))
                    want_capture = true;
            SDL_CaptureMouse(want_capture);
        }
        
        SDL_GetGlobalMouseState(out var mouseX, out var mouseY);
        if (!io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            SDL_GetWindowPosition(_GameWindow.Handle, out var windowX, out var windowY);
            mouseX -= windowX;
            mouseY -= windowY;
        }
        io.AddMousePosEvent(mouseX, mouseY);
        if (io.BackendFlags.HasFlag(ImGuiBackendFlags.HasMouseHoveredViewport))
        {
            var mouseViewportId = 0u;
            var mouseViewport = GetViewportById(_MouseWindowId);
            if (mouseViewport != null)
                mouseViewportId = mouseViewport->ID;
            io.AddMouseViewportEvent(mouseViewportId);
        }
        
        if (isActive)
        {
            //Maybe we can someday handle keyboard events from SDL, as we handle mouse input
            var keyboard = Keyboard.GetState();
            foreach (var key in _AllKeys)
            {
                if (TryMapKeys(key, out ImGuiKey imguikey))
                {
                    io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
                }
            }
        }
        Metrics.Stop("NewFrameUI");
    }

    public void Draw(GameTime gameTime, bool isActive)
    {
        NewFrame(gameTime, isActive);
        Metrics.Start("DrawUI");
        ImGui.NewFrame();
        DrawUI();
        ImGui.Render();
        _uiRenderer.RenderMainWindow();
        Metrics.Stop("DrawUI");
    }

    public void DrawExtraWindows()
    {
        if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            Metrics.Start("DrawUIWindows");
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
            Metrics.Stop("DrawUIWindows");
        }
    }

    public void OpenContextMenu()
    {
        openContextMenu = true;
    }

    private bool _resetLayout;

    protected virtual void DrawUI()
    {
        ImGui.PushFont(_Fonts[_FontIndex], Config.Instance.FontSize);
        ShowCrashInfo();
        if (CEDGame.Closing)
            return;
        ServerStatePopup();
        
        if (_resetLayout)
        {
            ImGui.LoadIniSettingsFromDisk("imgui.ini.default");
            Config.Instance.Layout = new Dictionary<string, WindowState>();
            _resetLayout = false;
        }
        ImGui.DockSpaceOverViewport(ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode);
        DrawContextMenu();
        DrawMainMenu();
        DrawStatusBar();
        foreach (var window in AllWindows.Values)
        { 
            window.Draw();   
        }
        DebugWindow.Draw();
        if (ShowTestWindow)
        {
            ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
            ImGui.ShowDemoWindow(ref ShowTestWindow);
        }
        ImGui.PopFont();
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
                    GetWindow<TilesWindow>().UpdateSelectedId(selected);
                    ImGui.CloseCurrentPopup();
                }
                if (selected is StaticObject so)
                {
                    if (ImGui.Button("Grab Hue"))
                    {
                        GetWindow<HuesWindow>().UpdateSelectedHue(so);
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
                ImGui.Text("Nothing to see here"u8);
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
                    ImGui.MenuItem("Show Height", "Ctrl + H", ref CEDGame.MapManager.FlatShowHeight);
                    ImGui.EndMenu();
                }
                ImGui.MenuItem("Animated Statics", Keymap.GetShortcut(Keymap.ToggleAnimatedStatics), ref CEDGame.MapManager.AnimatedStatics);
                ImGui.MenuItem("Show Grid", "Ctrl + G", ref CEDGame.MapManager.ShowGrid);
                ImGui.MenuItem("Show NoDraw tiles", "", ref CEDGame.MapManager.ShowNoDraw);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Tools"))
            {
                ToolsWindows.ForEach(w => w.DrawMenuItem());
                ImGui.EndMenu();
            }
            
            MenuWindows.ForEach(w => w.DrawMenuItem());
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

    private void DrawStatusBar()
    {
        if (ImGuiEx.BeginStatusBar())
        {
            var connectWindow = CEDGame.UIManager.GetWindow<ConnectWindow>();
            ImGui.TextColored(connectWindow.InfoColor, connectWindow.Info);
            if(CEDClient.Running)
            {
                ImGui.SameLine();
                ImGui.Text($"{ProfileManager.ActiveProfile.Name} ({CEDClient.AccessLevel})");
                ImGui.SameLine();
                ImGui.TextDisabled("|");
                ImGui.SameLine();
                var mapManager = CEDGame.MapManager;
                if (mapManager.Selected != null)
                {
                    ImGui.Text(mapManager.Selected.Tile.ToString());
                }
                ImGui.SameLine();
                var tileStats = $"X: {mapManager.TilePosition.X} Y: {mapManager.TilePosition.Y} Zoom: {mapManager.Camera.Zoom:F1} | FPS: {ImGui.GetIO().Framerate:F1}";
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize(tileStats).X - ImGui.GetStyle().WindowPadding.X);
                ImGui.Text(tileStats);
            }
            ImGuiEx.EndStatusBar();
        }
    }

    internal void DrawImage(Texture2D tex, Rectangle bounds)
    {
        DrawImage(tex, bounds, new Vector2(bounds.Width, bounds.Height));
    }

    internal unsafe bool DrawImage(Texture2D tex, Rectangle bounds, Vector2 size, bool stretch = false)
    {
        if (tex == null)
        {
            ImGui.Dummy(size);
            return false;
        }
        var texPtr = _uiRenderer.BindTexture(tex);
        var oldPos = ImGui.GetCursorPos();
        var offsetX = (size.X - bounds.Width) / 2;
        var offsetY = (size.Y - bounds.Height) / 2;
        if (!stretch)
        {
            ImGui.Dummy(size);
            ImGui.SetCursorPosX(oldPos.X + Math.Max(0, offsetX));
            ImGui.SetCursorPosY(oldPos.Y + Math.Max(0, offsetY));
        }
        var fWidth = (float)tex.Width;
        var fHeight = (float)tex.Height;
        var targetSize = stretch ? size : new Vector2(Math.Min(bounds.Width, size.X), Math.Min(bounds.Height, size.Y));
        var uvOffsetX = stretch ? 0 : Math.Min(0, offsetX);
        var uvOffsetY = stretch ? 0 : Math.Min(0, offsetY);
        var uv0 = new Vector2((bounds.X - uvOffsetX) / fWidth, (bounds.Y - uvOffsetY) / fHeight);
        var uv1 = new Vector2((bounds.X + bounds.Width + uvOffsetX) / fWidth, (bounds.Y + bounds.Height + uvOffsetY) / fHeight);
        ImGui.Image(new ImTextureRef(null, texPtr), targetSize, uv0, uv1);
        return true;
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
                ImGui.Text("Application crashed"u8);
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

    private bool _showServerStatePopup;
    public void ServerStatePopup()
    {
        if (!CEDClient.Running)
        {
            _showServerStatePopup = false;
            return;
        }
        if (CEDClient.ServerState != ServerState.Running)
        {
            ImGui.OpenPopup("ServerState");
            _showServerStatePopup = true;
        }
        if (ImGui.BeginPopupModal
            (
                "ServerState",
                ref _showServerStatePopup,
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
            ))
        {
            ImGui.Text("Server is performing operation"u8);
            ImGui.Text($"State: {CEDClient.ServerState.ToString()}");
            ImGui.Text($"Reason: {CEDClient.ServerStateReason}");
            if (CEDClient.ServerState == ServerState.Running)
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
    
    public T? GetWindow<T>() where T : Window
    {
        if(AllWindows.TryGetValue(typeof(T), out var window))
        {
            return (T)window;
        }
        return null;
    }
}