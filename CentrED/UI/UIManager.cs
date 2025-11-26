using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using CentrED.Renderer;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static SDL3.SDL;
using static CentrED.Application;
using static CentrED.LangEntry;
using Rectangle = System.Drawing.Rectangle;
using Vector2 = System.Numerics.Vector2;
using FNARectangle = Microsoft.Xna.Framework.Rectangle;

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
    private Keymap _keymap;
    
    // Event handling
    private SDL_EventFilter _EventFilter;
    private SDL_EventFilter _PrevEventFilter;

    public readonly bool HasViewports;

    internal Dictionary<Type, Window> AllWindows = new();
    internal List<Window> MainWindows = new();
    internal List<Window> ToolsWindows = new();
    internal List<Window> MenuWindows = new();

    internal DebugWindow DebugWindow;
    public bool ShowTestWindow;
    private ImFontPtr[] _Fonts;
    public string[] FontNames { get; }
    private int _FontIndex;
    private int _FontSize;
    public event Action? FontChanged;

    public void OnFontChanged()
    {
        FontChanged?.Invoke();
    }

    public int FontSize
    {
        get => _FontSize;
        set
        {
            _FontSize = value;
            Config.Instance.FontSize = _FontSize;
            ImGui.PopFont();
            ImGui.PushFont(_Fonts[_FontIndex], _FontSize);
            OnFontChanged();
        }
    }

    public int FontIndex
    {
        get => _FontIndex;
        set
        {
            _FontIndex = value;
            Config.Instance.FontName = FontNames[_FontIndex];
            ImGui.PopFont();
            ImGui.PushFont(_Fonts[_FontIndex], _FontSize);
            OnFontChanged();
        }
    }

    public unsafe UIManager(GraphicsDevice gd, GameWindow window, Keymap keymap)
    {
        _graphicsDevice = gd;
        _keymap = keymap;

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGuiImplSDL3.SetCurrentContext(context);
        var io = ImGui.GetIO();

        var glContext = SDL_GL_GetCurrentContext();
        if (glContext == IntPtr.Zero) 
        {
            ImGuiImplSDL3.InitForSDLGPU((SDLWindow*)window.Handle);
            //Viewports work for non-OpenGL
            io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        }
        else
        {
            ImGuiImplSDL3.InitForOpenGL((SDLWindow*)window.Handle, (void*)glContext);
        }
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
        
        HasViewports = io.BackendFlags.HasFlag(ImGuiBackendFlags.RendererHasViewports) && io.BackendFlags.HasFlag(ImGuiBackendFlags.PlatformHasViewports);

        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        if(Config.Instance.Viewports)
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigInputTrickleEventQueue = false;
        
        if (!File.Exists("imgui.ini") && File.Exists("imgui.ini.default"))
        {
            ImGui.LoadIniSettingsFromDisk("imgui.ini.default");
        }

        _uiRenderer = new UIRenderer(_graphicsDevice, HasViewports);
        
        AddWindow(Category.Main, new ConnectWindow());
        AddWindow(Category.Main, new ServerWindow());
        AddWindow(Category.Main, new OptionsWindow(_keymap));
        AddWindow(Category.Main, new ExportWindow());

        AddWindow(Category.Tools, new InfoWindow());
        AddWindow(Category.Tools, new ToolboxWindow());
        AddWindow(Category.Tools, new FilterWindow());
        AddWindow(Category.Tools, new TilesWindow());
        AddWindow(Category.Tools, new HuesWindow());
        AddWindow(Category.Tools, new BlueprintsWindow());
        AddWindow(Category.Tools, new LandBrushManagerWindow());
        AddWindow(Category.Tools, new LSOWindow());
        AddWindow(Category.Tools, new ChatWindow());
        AddWindow(Category.Tools, new HistoryWindow());
        AddWindow(Category.Tools, new ServerAdminWindow());
        
        AddWindow(Category.Menu, new MinimapWindow());
        DebugWindow = new DebugWindow();
        
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
        _FontSize = Config.Instance.FontSize;
    }
    
    private unsafe bool EventFilter(IntPtr userdata, SDL_Event* evt)
    {
        ImGuiImplSDL3.ProcessEvent((SDLEvent*)evt);
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
    private TileObject? contextMenuTile;

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

    public void NewFrame()
    {
        if(ImGui.GetMainViewport().PlatformRequestClose)
            CEDGame.Exit();
        Metrics.Start("NewFrameUI");
        ImGuiImplSDL3.NewFrame();
        Metrics.Stop("NewFrameUI");
    }

    public void Draw()
    {
        NewFrame();
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

    public void OpenContextMenu(TileObject? selected)
    {
        openContextMenu = true;
        contextMenuTile = selected;
    }

    private bool _resetLayout;

    protected virtual void DrawUI()
    {
        ShowCrashInfo();
        if (CEDGame.Closing)
            return;
        ImGui.PushFont(_Fonts[_FontIndex], Config.Instance.FontSize);
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
        var selected = contextMenuTile;
        if (selected != null && openContextMenu)
        {
            ImGui.OpenPopup("MainPopup");
            openContextMenu = false;
        }
        if (ImGui.BeginPopup("MainPopup"))
        {
            if (selected != null)
            {
                if (ImGui.Button(LangManager.Get(GRAB_TILE)))
                {
                    GetWindow<TilesWindow>().UpdateSelectedId(selected);
                    contextMenuTile = null;
                    ImGui.CloseCurrentPopup();
                }
                if (selected is StaticObject so)
                {
                    if (ImGui.Button(LangManager.Get(GRAB_HUE)))
                    {
                        GetWindow<HuesWindow>().UpdateSelectedHue(so);
                        contextMenuTile = null;
                        ImGui.CloseCurrentPopup();
                    }
                    if (ImGui.Button(LangManager.Get(FILTER_TILE)))
                    {
                        if (!CEDGame.MapManager.StaticFilterIds.Add(so.Tile.Id))
                            CEDGame.MapManager.StaticFilterIds.Remove(so.Tile.Id);
                        contextMenuTile = null;
                        ImGui.CloseCurrentPopup();
                    }
                }
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
                if (ImGui.MenuItem(LangManager.Get(QUIT)))
                    CEDGame.Exit();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu(LangManager.Get(EDIT)))
            {
                if (ImGui.MenuItem(LangManager.Get(UNDO), "Ctrl+Z", false, CEDClient.CanUndo))
                {
                    CEDClient.Undo();
                }
                if (ImGui.MenuItem(LangManager.Get(REDO), "Ctrl+Shift+Z", false, CEDClient.CanRedo))
                {
                    CEDClient.Redo();
                }
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu(LangManager.Get(VIEW)))
            {
                if (ImGui.MenuItem(LangManager.Get(RESET_ZOOM), "ESC"))
                {
                    CEDGame.MapManager.Camera.ResetCamera();
                }
                ImGui.MenuItem(LangManager.Get(WALKABLE_SURFACES), "Ctrl + W", ref CEDGame.MapManager.WalkableSurfaces);
                if (ImGui.BeginMenu(LangManager.Get(FLAT_VIEW)))
                {
                    if (ImGui.MenuItem(LangManager.Get(ENABLED), "Ctrl + F", ref CEDGame.MapManager.FlatView));
                    {
                        CEDGame.MapManager.UpdateAllTiles();
                    }
                    ImGui.MenuItem(LangManager.Get(SHOW_HEIGHT), "Ctrl + H", ref CEDGame.MapManager.FlatShowHeight);
                    ImGui.EndMenu();
                }
                ImGui.MenuItem(LangManager.Get(ANIMATE_OBJECTS), _keymap.GetShortcut(Keymap.ToggleAnimatedStatics), ref CEDGame.MapManager.AnimatedStatics);
                ImGui.MenuItem(LangManager.Get(TERRAIN_GRID), "Ctrl + G", ref CEDGame.MapManager.ShowGrid);
                ImGui.MenuItem(LangManager.Get(NODRAW_TILES), "", ref CEDGame.MapManager.ShowNoDraw);
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu(LangManager.Get(TOOLS)))
            {
                ToolsWindows.ForEach(w => w.DrawMenuItem());
                ImGui.EndMenu();
            }
            
            MenuWindows.ForEach(w => w.DrawMenuItem());
            if (ImGui.BeginMenu(LangManager.Get(HELP)))
            {
                if (ImGui.MenuItem(LangManager.Get(RESET_LAYOUT), false, File.Exists("imgui.ini.default")))
                {
                    _resetLayout = true;
                }
                if (ImGui.MenuItem(LangManager.Get(CLEAR_CACHE), "CTRL+R"))
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
                    string tileDisplay = mapManager.Selected switch
                    {
                        LandObject land => $"Land {land.Tile.Id.FormatId()} <{land.Tile.X},{land.Tile.Y},{land.Tile.Z}>",
                        StaticObject stat => $"Object {stat.Tile.Id.FormatId()} <{stat.Tile.X},{stat.Tile.Y},{stat.Tile.Z}> Hue:{((StaticTile)stat.Tile).Hue}",
                        _ => mapManager.Selected.Tile?.ToString() ?? "Unknown"
                    };
                    ImGui.Text(tileDisplay);
                }
                ImGui.SameLine();
                var tileStats = $"X: {mapManager.TilePosition.X} Y: {mapManager.TilePosition.Y} Zoom: {mapManager.Camera.Zoom:F1} | FPS: {ImGui.GetIO().Framerate:F1}";
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - ImGui.CalcTextSize(tileStats).X - ImGui.GetStyle().WindowPadding.X);
                ImGui.Text(tileStats);
            }
            ImGuiEx.EndStatusBar();
        }
    }

    internal bool DrawImage(Texture2D tex, FNARectangle bounds)
    {
        return DrawImage(tex, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), new Vector2(bounds.Width, bounds.Height));
    }
    
    internal bool DrawImage(Texture2D tex, FNARectangle bounds, Vector2 size, bool stretch = false)
    {
        return DrawImage(tex,  new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), size, stretch);
    }

    
    internal bool DrawImage(Texture2D tex, Rectangle bounds)
    {
        return DrawImage(tex, bounds, new Vector2(bounds.Width, bounds.Height));
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
                ImGui.Text(LangManager.Get(APP_CRASHED));
                ImGui.InputTextMultiline
                    (" ", ref _crashText, 1000, new Vector2(800, 150), ImGuiInputTextFlags.ReadOnly);
                if (ImGui.Button(LangManager.Get(COPY_TO_CLIPBOARD)))
                {
                    ImGui.SetClipboardText(_crashText);
                }
                ImGui.Separator();
                if (ImGui.Button(LangManager.Get(QUIT)))
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
            ImGui.Text(LangManager.Get(SERVER_IS_PERFORMING_OPERATION));
            ImGui.Text($"{LangManager.Get(STATE)}: {CEDClient.ServerState.ToString()}");
            ImGui.Text($"{LangManager.Get(REASON)}: {CEDClient.ServerStateReason}");
            if (CEDClient.ServerState == ServerState.Running)
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
    
    public T GetWindow<T>() where T : Window
    {
        return (T)AllWindows[typeof(T)];
    }
}
