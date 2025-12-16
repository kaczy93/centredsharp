using CentrED.IO.Models;
using CentrED.Map;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ImageOverlayWindow : Window
{
    public override string Name => LangManager.Get(IMAGE_OVERLAY_WINDOW) + "###ImageOverlay";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    private string _imagePath = "";
    private int[] _position = new int[2];
    private float _scale = 1.0f;
    private float _opacity = 1.0f;
    private float _screen = 0.0f;
    private bool _settingsLoaded = false;

    private ImageOverlaySettings Settings => Config.Instance.ImageOverlay;

    private void LoadSettings()
    {
        if (_settingsLoaded)
            return;

        _settingsLoaded = true;
        var overlay = CEDGame.MapManager.ImageOverlay;

        _imagePath = Settings.ImagePath;
        overlay.Enabled = Settings.Enabled;
        overlay.DrawAboveTerrain = Settings.DrawAboveTerrain;
        overlay.WorldX = Settings.WorldX;
        overlay.WorldY = Settings.WorldY;
        overlay.Scale = Settings.Scale;
        overlay.Opacity = Settings.Opacity;
        overlay.Screen = Settings.Screen;

        if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
        {
            try
            {
                overlay.LoadImage(CEDGame.GraphicsDevice, _imagePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to auto-load overlay image: {ex.Message}");
            }
        }
    }

    private void SaveSettings()
    {
        var overlay = CEDGame.MapManager.ImageOverlay;

        Settings.ImagePath = _imagePath;
        Settings.Enabled = overlay.Enabled;
        Settings.DrawAboveTerrain = overlay.DrawAboveTerrain;
        Settings.WorldX = overlay.WorldX;
        Settings.WorldY = overlay.WorldY;
        Settings.Scale = overlay.Scale;
        Settings.Opacity = overlay.Opacity;
        Settings.Screen = overlay.Screen;
    }

    protected override void InternalDraw()
    {
        var mapManager = CEDGame.MapManager;
        var overlay = mapManager.ImageOverlay;

        LoadSettings();

        if (ImGui.InputText(LangManager.Get(FILE_PATH), ref _imagePath, 512))
        {
            SaveSettings();
        }
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile(
                LangManager.Get(SELECT_FILE),
                Environment.CurrentDirectory,
                ["*.png", "*.jpg", "*.jpeg", "*.bmp"],
                "Image files",
                false,
                out var newPath))
            {
                _imagePath = newPath;
                SaveSettings();
            }
        }

        var hasTexture = overlay.Texture != null;
        ImGui.BeginDisabled(string.IsNullOrEmpty(_imagePath));
        if (ImGui.Button(LangManager.Get(IMAGE_OVERLAY_LOAD)))
        {
            try
            {
                overlay.LoadImage(CEDGame.GraphicsDevice, _imagePath);
                _position[0] = overlay.WorldX;
                _position[1] = overlay.WorldY;
                SaveSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image: {ex.Message}");
            }
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(!hasTexture);
        if (ImGui.Button(LangManager.Get(IMAGE_OVERLAY_UNLOAD)))
        {
            overlay.UnloadImage();
            SaveSettings();
        }
        ImGui.EndDisabled();

        ImGui.Separator();

        if (hasTexture)
        {
            ImGui.Text($"{LangManager.Get(IMAGE_OVERLAY_SIZE)}: {overlay.WidthInTiles:F1} x {overlay.HeightInTiles:F1}");
            ImGui.Text($"Image: {overlay.ImageWidth} x {overlay.ImageHeight} px");
        }
        else
        {
            ImGui.TextDisabled("No image loaded");
        }

        ImGui.Separator();

        var enabled = overlay.Enabled;
        if (ImGui.Checkbox(LangManager.Get(ENABLED), ref enabled))
        {
            overlay.Enabled = enabled;
            SaveSettings();
        }

        var drawAbove = overlay.DrawAboveTerrain;
        if (ImGui.Checkbox(LangManager.Get(IMAGE_OVERLAY_DRAW_ABOVE), ref drawAbove))
        {
            overlay.DrawAboveTerrain = drawAbove;
            SaveSettings();
        }

        _position[0] = overlay.WorldX;
        _position[1] = overlay.WorldY;
        if (ImGui.InputInt2(LangManager.Get(IMAGE_OVERLAY_POSITION), ref _position[0]))
        {
            overlay.WorldX = _position[0];
            overlay.WorldY = _position[1];
            SaveSettings();
        }

        _scale = overlay.Scale;
        if (ImGui.SliderFloat(LangManager.Get(IMAGE_OVERLAY_SCALE), ref _scale, 0.1f, 10.0f, "%.2f"))
        {
            overlay.Scale = _scale;
            SaveSettings();
        }

        _opacity = overlay.Opacity;
        if (ImGui.SliderFloat(LangManager.Get(IMAGE_OVERLAY_OPACITY), ref _opacity, 0.0f, 1.0f, "%.2f"))
        {
            overlay.Opacity = _opacity;
            SaveSettings();
        }

        _screen = overlay.Screen;
        if (ImGui.SliderFloat(LangManager.Get(IMAGE_OVERLAY_SCREEN), ref _screen, 0.0f, 1.0f, "%.2f"))
        {
            overlay.Screen = _screen;
            SaveSettings();
        }

        if (ImGui.Button("Set to View Center"))
        {
            var tilePos = mapManager.TilePosition;
            overlay.WorldX = tilePos.X;
            overlay.WorldY = tilePos.Y;
            SaveSettings();
        }
    }
}
