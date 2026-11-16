using ClassicUO.Renderer;
using ClassicUO.Renderer.Lights;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.CentrED;

namespace CentrED.Lights;

public class LightsManager
{
    private static LightsManager _instance;
    public static LightsManager Instance => _instance;
    
    private static readonly BlendState DarknessBlend = new()
    {
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.SourceColor,
        ColorBlendFunction = BlendFunction.Add,
    };

    private static readonly BlendState AltLightsBlend = new()
    {
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Add,
    };
    
    private static Color DefaultApplyBlendColor = Color.White;
    private static Color AltLightsApplyBlendColor = new(0.5f, 0.5f, 0.5f);
    
    public BlendState ApplyBlendState => AltLights.Value ? AltLightsBlend : DarknessBlend;
    public Color ApplyBlendColor => AltLights.Value ? AltLightsApplyBlendColor : DefaultApplyBlendColor;
    

    public static readonly ConfigValue<bool> ColoredLights = App.Workspace.Active.Init("lights.colored", true);
    public static readonly ConfigValue<bool> AltLights = App.Workspace.Active.Init("lights.alternative", false);
    public static readonly ConfigValue<bool> DarkNights = App.Workspace.Active.Init("lights.dark_nights", false);
    public static readonly ConfigValue<bool> ShowInvisibleLights = App.Workspace.Active.Init("lights.show_invisible", false);
    public readonly ushort VisibleLightId = 0x3EE8;
    public static readonly ConfigValue<bool> ClassicUONormals = App.Workspace.Active.Init("lights.classicuo_normals", false);
    public static readonly ConfigValue<int> GlobalLightLevel = App.Workspace.Active.Init("lights.global_level", 30);
    public bool MaxGlobalLight => GlobalLightLevel.Value == 30;

    private Color _globalLightLevelColor;
    public Color GlobalLightLevelColor => AltLights.Value ? Color.Black : _globalLightLevelColor;

    public void UpdateGlobalLight()
    {
        var val = (GlobalLightLevel.Value + 2) * 0.03125f;
        if (DarkNights.Value)
        {
            val -= 0.04f;
        }
        _globalLightLevelColor = new Color(val, val, val, 1f);
    }
    
    
    const int TEXTURE_WIDTH = 32;
    const int TEXTURE_HEIGHT = 63;
    
    private Light _lights;
    public readonly Texture2D LightColorsTexture;

    public static void Load(GraphicsDevice gd)
    {
        _instance = new LightsManager(gd);
    }
    
    private unsafe LightsManager(GraphicsDevice gd)
    {
        _lights = new Light(App.MapManager.UoFileManager.Lights, gd);
        LightColorsTexture = new Texture2D(gd, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * TEXTURE_HEIGHT);
        fixed (uint* ptr = buffer)
        {
            LightColors.CreateLightTextures(buffer, TEXTURE_HEIGHT);
            LightColorsTexture.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint));
        }
        LightColors.LoadLights();
        UpdateGlobalLight();
    }

    public SpriteInfo GetLight(uint id)
    {
        return _lights.GetLight(id);
    }
}