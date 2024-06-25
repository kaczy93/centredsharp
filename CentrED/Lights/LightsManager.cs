using ClassicUO.Renderer;
using ClassicUO.Renderer.Lights;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Lights;

public class LightsManager
{
    private Light _lights;
    private static LightsManager _instance;
    public static LightsManager Instance => _instance;
    
    const int TEXTURE_WIDTH = 32;
    const int TEXTURE_HEIGHT = 63;
    
    public readonly Texture2D Texture;

    public static void Load(GraphicsDevice gd)
    {
        _instance = new LightsManager(gd);
    }
    
    private unsafe LightsManager(GraphicsDevice gd)
    {
        _lights = new Light(gd);
        Texture = new Texture2D(gd, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * TEXTURE_HEIGHT);
        fixed (uint* ptr = buffer)
        {
            LightColors.CreateLightTextures(buffer, TEXTURE_HEIGHT);
            Texture.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint));
        }
        LightColors.LoadLights();
    }

    public SpriteInfo GetLight(uint id)
    {
        return _lights.GetLight(id);
    }
}