using CentrED.Map;
using CentrED.Renderer;
using CentrED.Tools;
using CentrED.UI.Windows;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI;

public partial class UIManager {
    private CentrEDGame _game;
    internal UIRenderer _uiRenderer;
    internal GraphicsDevice _graphicsDevice;
    internal readonly MapManager _mapManager;
    
    // Input
    private int _scrollWheelValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _allKeys = Enum.GetValues<Keys>();

    internal InfoWindow _infoWindow;
    internal ToolboxWindow _toolboxWindow;
    private DebugWindow _debugWindow;
    
    private int[] _matchedLandIds;
    private int[] _matchedStaticIds;

    private int[] _matchedHueIds;

    internal List<Tool> tools = new();
    internal List<Window> mainWindows = new();
    internal List<Window> toolsWindows = new();

    public UIManager(CentrEDGame game, GraphicsDevice gd, MapManager mapManager) {
        _game = game;
        _graphicsDevice = gd;
        _uiRenderer = new UIRenderer(_graphicsDevice);
        _mapManager = mapManager;

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();

        TextInputEXT.TextInput += c =>
        {
            if (c == '\t') return;

            ImGui.GetIO().AddInputCharacter(c);
        };

        _uiRenderer.RebuildFontAtlas();
        
        mainWindows.Add(new ConnectWindow(this));
        mainWindows.Add(new ServerWindow(this));

        _infoWindow = new InfoWindow(this);
        _toolboxWindow = new ToolboxWindow(this);
        toolsWindows.Add(_infoWindow);
        toolsWindows.Add(_toolboxWindow);

        tools.Add(new SelectTool(this));
        tools.Add(new DrawTool(this));
        tools.Add(new RemoveTool(this));
        tools.Add(new MoveTool(this));
        tools.Add( new ElevateTool(this));
        tools.Add(new HueTool(this));

        _debugWindow = new DebugWindow(this);
        
        _mapManager.Client.Connected += OnConnect;
    }

    private void OnConnect() {
        FilterTiles();
        FilterHues();
    }

    public void Update(GameTime gameTime, bool isActive)
    {
        var io = ImGui.GetIO();

        io.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        io.AddMousePosEvent(mouse.X, mouse.Y);
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

        io.DisplaySize = new Vector2(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vector2(1f, 1f);
        
    }

    internal double _framesPerSecond;
    
    public void Draw(GameTime gameTime) {
        _framesPerSecond = 1 / gameTime.ElapsedGameTime.TotalSeconds;
        ImGui.NewFrame();
        DrawUI();
        ImGui.Render();

        unsafe { _uiRenderer.RenderDrawData(ImGui.GetDrawData()); }
    }

    public bool CapturingMouse => ImGui.GetIO().WantCaptureMouse;
    public bool CapturingKeyboard => ImGui.GetIO().WantCaptureKeyboard;

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
            Keys.LeftShift or Keys.RightShift => ImGuiKey.ModShift,
            Keys.LeftControl or Keys.RightControl => ImGuiKey.ModCtrl,
            Keys.LeftAlt or Keys.RightAlt => ImGuiKey.ModAlt,
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

    
    protected virtual void DrawUI() {
        DrawMainMenu();
        mainWindows.ForEach(w => w.Draw());
        DrawOptionsWindow();
        toolsWindows.ForEach(w => w.Draw());
        DrawTilesWindow();
        DrawHuesWindow();
        DrawMinimapWindow();
        _debugWindow.Draw();
    }
    
    private float _mainMenuHeight; 
    
    private void DrawMainMenu() {
        if (ImGui.BeginMainMenuBar()) {
            if (ImGui.BeginMenu("CentrED")) {
                mainWindows.ForEach(w => w.DrawMenuItem());
                ImGui.Separator();
                if (ImGui.MenuItem("Options")) _optionsShowWindow = true;
                ImGui.Separator();
                if (ImGui.MenuItem("Quit")) _game.Exit();
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Tools")) {
                toolsWindows.ForEach(w => w.DrawMenuItem());
                ImGui.MenuItem("Tiles", "", ref _tilesShowWindow);
                ImGui.MenuItem("Hues", "", ref HuesShowWindow);
                ImGui.MenuItem("Minimap", "", ref _minimapShowWindow);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help")) {
                //Credits
                //About
                ImGui.Separator();
                _debugWindow.DrawMenuItem();
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        _mainMenuHeight = ImGui.GetItemRectSize().Y;
    }
    
    internal void DrawImage(Texture2D tex, Rectangle bounds) {
        DrawImage(tex, bounds, new Vector2(bounds.Width, bounds.Height));
    }
    
    internal void DrawImage(Texture2D tex, Rectangle bounds, Vector2 size) {
        var texPtr = _uiRenderer.BindTexture(tex);
        var fWidth = (float)tex.Width;
        var fHeight = (float)tex.Height;
        var uv0 = new Vector2(bounds.X / fWidth, bounds.Y / fHeight);
        var uv1 = new Vector2(
            (bounds.X + bounds.Width) / fWidth, 
            (bounds.Y + bounds.Height) / fHeight
        );
        ImGui.Image(texPtr, size, uv0, uv1);
    }
}