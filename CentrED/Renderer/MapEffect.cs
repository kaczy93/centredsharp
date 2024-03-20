using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer.Effects;

public class MapEffect : Effect
{
    public Matrix WorldViewProj
    {
        set => Parameters["WorldViewProj"].SetValue(value);
    }

    public int HueCount
    {
        set => Parameters["HueCount"].SetValue(value);
    }

    public Vector4 VirtualLayerFillColor
    {
        set => Parameters["VirtualLayerFillColor"].SetValue(value);
    }

    public Vector4 VirtualLayerBorderColor
    {
        set => Parameters["VirtualLayerBorderColor"].SetValue(value);
    }
    
    public Vector4 TerrainGridFlatColor
    {
        set => Parameters["TerrainGridFlatColor"].SetValue(value);
    }
    
    public Vector4 TerrainGridAngledColor
    {
        set => Parameters["TerrainGridAngledColor"].SetValue(value);
    }

    public float LightLevel
    {
        set => Parameters["LightLevel"].SetValue(value);
    }

    protected static byte[] GetResource(string name)
    {
        Stream? stream = typeof(MapEffect).Assembly.GetManifestResourceStream(name);

        if (stream == null)
        {
            return Array.Empty<byte>();
        }

        using (MemoryStream ms = new MemoryStream())
        {
            stream.CopyTo(ms);

            return ms.ToArray();
        }
    }

    public MapEffect(GraphicsDevice device) : this
        (device, GetResource("CentrED.Renderer.Shaders.MapEffect.fxc"))
    {
    }
    
    public MapEffect(GraphicsDevice device, byte[] effectCode) : base(device, effectCode)
    {
        Parameters["VirtualLayerFillColor"].SetValue(new Vector4(0.2f, 0.2f, 0.2f, 0.1f));
        Parameters["VirtualLayerBorderColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        Parameters["TerrainGridFlatColor"].SetValue(new Vector4(0.5f, 0.5f, 0.0f, 0.5f));
        Parameters["TerrainGridAngledColor"].SetValue(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        Parameters["LightLevel"].SetValue(1.0f);
    }
}