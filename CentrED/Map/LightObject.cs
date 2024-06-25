using CentrED.Lights;
using Microsoft.Xna.Framework;

namespace CentrED.Map;

public class LightObject : MapObject
{
    public LightObject(byte lightId, int lightColor, bool isHued)
    {
        var spriteInfo = LightsManager.Instance.GetLight(lightId);
        Texture = spriteInfo.Texture;
        TextureBounds = spriteInfo.UV;

        Vector3 hue = Vector3.Zero;
        hue.X = lightColor;
        hue.Y = hue.X > 1.0f
            ?isHued
                ? ShaderHueTranslator.SHADER_HUED
                : ShaderHueTranslator.SHADER_LIGHTS
            : ShaderHueTranslator.SHADER_NONE;
        
        for (var i = 0; i < Vertices.Length; i++)
        {
            Vertices[i].Hue.X =
        }
    }
}