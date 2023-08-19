using CentrED.Map;
using CentrED.Renderer;
using CentrED.Tools;
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

        io.DisplaySize = new Vector2(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vector2(1f, 1f);

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

    private bool show_test_window = false;
    private int tileX, tileY;

    protected virtual void DrawUI()
    {
        {
            if (ImGui.BeginMainMenuBar()) {
                if (ImGui.BeginMenu("CentrED")) {
                    if (ImGui.MenuItem("Connect", !_mapManager.Client.Running)) _showConnectWindow = true;
                    if (ImGui.MenuItem("Local Server")) _showLocalServerWindow = true;
                    if (ImGui.MenuItem("Disconnect", _mapManager.Client.Running)) _mapManager.Client.Disconnect();
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
            
            if(_showConnectWindow) DrawConnectWindow();
            if(_showLocalServerWindow) DrawLocalServerWindow();


            ImGui.Text($"Camera focus tile {_mapManager.Camera.LookAt / _mapManager.TILE_SIZE}");
            ImGui.Separator();
            
            ImGui.Checkbox("DrawLand", ref MapManager.IsDrawLand);
            ImGui.Checkbox("DrawStatics", ref MapManager.IsDrawStatic);
            ImGui.Checkbox("DrawShadows", ref MapManager.IsDrawShadows);
            ImGui.SliderInt("Min Z render", ref _mapManager.MIN_Z, -127, 127);
            ImGui.SliderInt("Max Z render", ref _mapManager.MAX_Z, -127, 127);
            ImGui.SliderFloat("Zoom", ref _mapManager.Camera.Zoom, 0.2f, 10.0f);
            ImGui.Separator();
            ImGui.InputInt("Camera x", ref tileX);
            ImGui.InputInt("Camera y", ref tileY);
            if (ImGui.Button("Update pos")) {
                _mapManager.Camera.Position.X = tileX * _mapManager.TILE_SIZE;
                _mapManager.Camera.Position.Y = tileY * _mapManager.TILE_SIZE;
            }
            ImGui.Separator();
            if(ImGui.Button("Flush")) _mapManager.Client.Flush();
            if(ImGui.Button("Render 4K")) _mapManager.DrawHighRes();
            ImGui.BeginGroup();
            if (ImGui.RadioButton("Hue", HueTool.Instance.Active)) {
                if(_mapManager.ActiveTool != null)
                    _mapManager.ActiveTool.Active = false;
                _mapManager.ActiveTool = HueTool.Instance;
                _mapManager.ActiveTool.Active = true;
            }

            if (ImGui.RadioButton("Elevate", ElevateTool.Instance.Active)) {
                if(_mapManager.ActiveTool != null)
                    _mapManager.ActiveTool.Active = false;
                _mapManager.ActiveTool = ElevateTool.Instance;
                _mapManager.ActiveTool.Active = true;
            }
            if(_mapManager.ActiveTool != null){
                _mapManager.ActiveTool.DrawWindow();
            }
            ImGui.EndGroup();
            if (ImGui.Button("Test Window")) show_test_window = !show_test_window;
        }
        
        if (show_test_window)
        {
            ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
            ImGui.ShowDemoWindow(ref show_test_window);
        }
    }
}