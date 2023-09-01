using CentrED.Map;
using CentrED.Renderer;
using CentrED.Tools;
using ClassicUO.Assets;
using ClassicUO.IO;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI;

internal partial class UIManager
{
    private UIRenderer _uiRenderer;
    private GraphicsDevice _graphicsDevice;
    private readonly MapManager _mapManager;
    private readonly HuesManager _huesManager;
    
    // Input
    private int _scrollWheelValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _allKeys = Enum.GetValues<Keys>();

    private readonly TileDataLoader _tileDataLoader;
    private readonly ArtLoader _artLoader;

    private SelectTool _selectTool;
    private DrawTool _drawTool;
    private RemoveTool _removeTool;
    private MoveTool _moveTool;
    private ElevateTool _elevateTool;
    private HueTool _hueTool;

    private int[] _validLandIds;
    private int[] _validStaticIds;

    private int[] _matchedLandIds;
    private int[] _matchedStaticIds;

    private int[] _matchedHueIds;

    public UIManager(GraphicsDevice gd, MapManager mapManager)
    {
        _graphicsDevice = gd;
        _uiRenderer = new UIRenderer(_graphicsDevice);
        _mapManager = mapManager;
        _huesManager = HuesManager.Instance;

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();

        TextInputEXT.TextInput += c =>
        {
            if (c == '\t') return;

            ImGui.GetIO().AddInputCharacter(c);
        };

        _uiRenderer.RebuildFontAtlas();

        _selectTool = new SelectTool(this, _mapManager);
        _drawTool = new DrawTool(this, _mapManager);
        _removeTool = new RemoveTool(this, _mapManager);
        _moveTool = new MoveTool(this, _mapManager);
        _elevateTool = new ElevateTool(this, _mapManager);
        _hueTool = new HueTool(this, _mapManager);

        _tileDataLoader = TileDataLoader.Instance;
        _artLoader = ArtLoader.Instance;

        var landIds = new List<int>();
        for (int i = 0; i < _tileDataLoader.LandData.Length; i++) {
            if (!_artLoader.GetValidRefEntry(i).Equals(UOFileIndex.Invalid)) {
                landIds.Add(i);
            }
        }
        _validLandIds = landIds.ToArray();
        var staticIds = new List<int>();
        for (int i = 0; i < _tileDataLoader.StaticData.Length; i++) {
            if (!_artLoader.GetValidRefEntry(i + ArtLoader.MAX_LAND_DATA_INDEX_COUNT).Equals(UOFileIndex.Invalid)) {
                staticIds.Add(i);
            }
        }
        _validStaticIds = staticIds.ToArray();
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

    private double _framesPerSecond;
    
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
        //File
        DrawConnectWindow();
        DrawLocalServerWindow();
        //Tools
        DrawInfoWindow();
        DrawToolboxWindow();
        DrawTilesWindow();
        DrawHuesWindow();
        
        _mapManager.ActiveTool?.DrawWindow();
        //Help
        DrawDebugWindow();
        if (_debugShowTestWindow)
        {
            ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
            ImGui.ShowDemoWindow(ref _debugShowTestWindow);
        }
    }

    private void ToolButton(Tool tool) {
        if (ImGui.RadioButton(tool.Name, _mapManager.ActiveTool == tool)) {
            _mapManager.ActiveTool?.OnDeactivated(_mapManager.Selected);
            _mapManager.ActiveTool = tool;
            _mapManager.ActiveTool?.OnActivated(_mapManager.Selected);
        }
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