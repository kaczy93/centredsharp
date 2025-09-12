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
    
    private static Color DefaultApplyBlendColor = Color.White;
    private static Color AltLightsApplyBlendColor = new(0.5f, 0.5f, 0.5f);
    
    public BlendState ApplyBlendState => Instance.AltLights ? AltLightsBlend : DarknessBlend;
    public Color ApplyBlendColor => Instance.AltLights ? AltLightsApplyBlendColor : DefaultApplyBlendColor;
    

    public bool ColoredLights = true;
    public bool AltLights = false;
    public bool DarkNights = false;
    public bool ShowInvisibleLights = false;
    public readonly ushort VisibleLightId = 0x3EE8;
    public bool ClassicUONormals = false;
    public int GlobalLightLevel = 30;
    public bool MaxGlobalLight => GlobalLightLevel == 30;

    private Color _globalLightLevelColor;
    public Color GlobalLightLevelColor => AltLights ? Color.Black : _globalLightLevelColor;

    public void UpdateGlobalLight()
    {
        var val = (GlobalLightLevel + 2) * 0.03125f;
        if (DarkNights)
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
        _lights = new Light(Application.CEDGame.MapManager.UoFileManager.Lights, gd);
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