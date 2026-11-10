using System.Text.Json;
using Microsoft.Xna.Framework.Input;

namespace CentrED;

public class WindowState
{
    public bool IsOpen { get; set; }
}

public class ImageOverlaySettings
{
    public string ImagePath = "";
    public bool Enabled;
    public bool DrawAboveTerrain;
    public int WorldX;
    public int WorldY;
    public float Scale = 1.0f;
    public float Opacity = 1.0f;
    public float Screen = 0.0f;
}

public class ConfigRoot
{
    public string ActiveProfile = "";
    public string ActiveSettingsProfile = "Default";
    public string ServerConfigPath = "cedserver.xml";
    public bool PreferTexMaps;
    public bool ObjectBrightHighlight;
    public bool LegacyMouseScroll;
    public bool Viewports;
    public string GraphicsDriver = "Auto"; //Auto,SDL_GPU,D3D11,OpenGL
    public Dictionary<string, WindowState> Layout = new();
    public Dictionary<string, (Keys[], Keys[])> Keymap = new();
    public int FontSize = 13;
    public string FontName = "ProggyClean.ttf";
    public string Language = "English";
    public UI.NumberDisplayFormat NumberFormat = UI.NumberDisplayFormat.HEX;
    public ImageOverlaySettings ImageOverlay = new();
}

public static class ConfigLegacy
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        IncludeFields = true,
        WriteIndented = true,
    };
    
    public static ConfigRoot? Instance;
    private static string _configFilePath = "settings.json";

    public static void Initialize()
    {
        if (!File.Exists(_configFilePath))
        {
            return;
        }

        var jsonText = File.ReadAllText(_configFilePath);
        Instance = JsonSerializer.Deserialize<ConfigRoot>(jsonText, SerializerOptions);
    }

    public static void Purge()
    {
        File.Delete(_configFilePath);
    }
}