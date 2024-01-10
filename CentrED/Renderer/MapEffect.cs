using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer.Effects;

public class MapEffect : Effect
{
    private readonly EffectParameter _worldViewProjParam;
    private readonly EffectParameter _hueCountParam;
    private readonly EffectParameter _virtualLayerFillColorParam;
    private readonly EffectParameter _virtualLayerBorderColorParam;

    private Matrix _worldViewProj = Matrix.Identity;
    private int _hueCount = 0;

    private Vector4 _virtualLayerFillColor = new(0.2f, 0.2f, 0.2f, 0.1f);
    private Vector4 _virtualLayerBorderColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    public Matrix WorldViewProj
    {
        get { return _worldViewProj; }
        set { _worldViewProj = value; }
    }

    public int HueCount
    {
        set => _hueCount = value;
    }

    public Vector4 VirtualLayerFillColor
    {
        set => _virtualLayerFillColor = value;
    }

    public Vector4 VirtualLayerBorderColor
    {
        set => _virtualLayerBorderColor = value;
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
        _worldViewProjParam = Parameters["WorldViewProj"];
        _hueCountParam = Parameters["HueCount"];
        _virtualLayerFillColorParam = Parameters["VirtualLayerFillColor"];
        _virtualLayerBorderColorParam = Parameters["VirtualLayerBorderColor"];
    }

    protected override void OnApply()
    {
        _worldViewProjParam.SetValue(_worldViewProj);
        _hueCountParam.SetValue(_hueCount);
        _virtualLayerFillColorParam.SetValue(_virtualLayerFillColor);
        _virtualLayerBorderColorParam.SetValue(_virtualLayerBorderColor);
    }
}