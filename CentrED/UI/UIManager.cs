using CentrED.Map;
using CentrED.Renderer;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CentrED.UI;

internal class UIManager
{
    private UIRenderer _uiRenderer;
    private GraphicsDevice _graphicsDevice;
    private readonly MapManager _mapManager;
    
    // Input
    private int _scrollWheelValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _allKeys = Enum.GetValues<Keys>();

    public UIManager(GraphicsDevice gd, MapManager mapManager)
    {
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
    }

    public void Update(GameTime gameTime)
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

        io.DisplaySize = new System.Numerics.Vector2(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);

        ImGui.NewFrame();

        DrawUI();
    }

    public void Draw()
    {
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

    private float f = 0.0f; 

    private bool show_test_window = false;
    private bool show_another_window = false;
    private System.Numerics.Vector3 clear_color = new System.Numerics.Vector3(114f / 255f, 144f / 255f, 154f / 255f);
    private byte[] _textBuffer = new byte[100];

    protected virtual void DrawUI()
    {
        {
            ImGui.SliderFloat("z scale", ref _mapManager.TILE_Z_SCALE, 3f, 5f, string.Empty);
            ImGui.Text($"TILE_Z_SCALE: {_mapManager.TILE_Z_SCALE}");
            ImGui.Separator();
            ImGui.Text($"Camera focus pos {_mapManager.Camera.LookAt}");
            ImGui.Text($"Camera focus tile {_mapManager.Camera.LookAt / _mapManager.TILE_SIZE}");
            ImGui.Separator();
            ImGui.SliderFloat("mouse z", ref f, 0f, 1f, string.Empty);
            ImGui.Text($"Depth: {f}");
            var mouse = Mouse.GetState();
            var mouseVec = new Vector3(mouse.X, mouse.Y, f);
            var unp = _graphicsDevice.Viewport.Unproject(mouseVec, _mapManager.Camera.proj, _mapManager.Camera.view, _mapManager.Camera.world);
            ImGui.Text($"Mouse pos {unp}");
            ImGui.Text($"Mouse tile {unp.X /  _mapManager.TILE_SIZE} {unp.Y /  _mapManager.TILE_SIZE} {unp.Z / 6}");
            ImGui.Separator();
            ImGui.Checkbox("DrawLand", ref MapManager.IsDrawLand);
            ImGui.Checkbox("DrawStatics", ref MapManager.IsDrawStatic);
            ImGui.Checkbox("DrawShadows", ref MapManager.IsDrawShadows);
            ImGui.SliderInt("Min Z render", ref _mapManager.MIN_Z, -127, 127);
            ImGui.SliderInt("Max Z render", ref _mapManager.MAX_Z, -127, 127);
            ImGui.Text($"Zoom: {_mapManager.Camera.Zoom}");
            ImGui.SliderFloat("Camera x", ref _mapManager.Camera.LookAt.X, 0, 100);
            ImGui.SliderFloat("Camera y", ref _mapManager.Camera.LookAt.Y, 0, 100);
        }

        if (show_another_window)
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(200, 100), ImGuiCond.FirstUseEver);
            ImGui.Begin("Another Window", ref show_another_window);
            ImGui.Text("Hello");
            ImGui.End();
        }

        if (show_test_window)
        {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(650, 20), ImGuiCond.FirstUseEver);
            ImGui.ShowDemoWindow(ref show_test_window);
        }
    }
}