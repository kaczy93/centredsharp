using ClassicUO.Renderer;
using ClassicUO.Renderer.Lights;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    
    public static BlendState BlendState => Instance.AltLights ? AltLightsBlend : DarknessBlend;

    private static Color DarknessColor = new(0, 0, 1);
    private static Color AltLightsColor = new(0, 0, 0.5f);

    public static Color LightsColor => Instance.AltLights ? AltLightsColor : DarknessColor;
    
    public bool AltLights = false;

    private byte _globalLightLevel;

    public byte GlobalLightLevel
    {
        get => _globalLightLevel;
        set { 
            _globalLightLevel = value;
            var val = (_globalLightLevel + 2) / 32f;
            GlobalLightLevelColor = new Color(val, val, val, 1f);
        }
    }

    public Color GlobalLightLevelColor { get; private set; }
    
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
        _lights = new Light(gd);
        LightColorsTexture = new Texture2D(gd, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * TEXTURE_HEIGHT);
        fixed (uint* ptr = buffer)
        {
            LightColors.CreateLightTextures(buffer, TEXTURE_HEIGHT);
            LightColorsTexture.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint));
        }
        LightColors.LoadLights();
    }

    public SpriteInfo GetLight(uint id)
    {
        return _lights.GetLight(id);
    }
}