using ClassicUO.Renderer;
using ClassicUO.Renderer.Lights;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.CentrED;

namespace CentrED.Lights;

public class LightsManager
{
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
    

    public readonly ConfigValue<bool> ColoredLights = App.Workspace.Active.Init("lights.colored", true);
    public readonly ConfigValue<bool> AltLights = App.Workspace.Active.Init("lights.alternative", false);
    public readonly ConfigValue<bool> DarkNights = App.Workspace.Active.Init("lights.dark_nights", false);
    public readonly ConfigValue<bool> ShowInvisibleLights = App.Workspace.Active.Init("lights.show_invisible", false);
    public readonly ushort VisibleLightId = 0x3EE8;
    public readonly ConfigValue<bool> ClassicUONormals = App.Workspace.Active.Init("lights.classicuo_normals", false);
    public readonly ConfigValue<int> GlobalLightLevel = App.Workspace.Active.Init("lights.global_level", 30);
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
    
    private Light? _lights;
    public Texture2D? LightColorsTexture { get; private set; }
    public bool Loaded => _lights != null;

    public LightsManager()
    {
        UpdateGlobalLight();
    }

    public unsafe void Load(GraphicsDevice gd)
    {
        _lights = new Light(App.MapManager.UoFileManager.Lights, gd);
        LightColorsTexture?.Dispose();
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
        return _lights!.GetLight(id);
    }
}