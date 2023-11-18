using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer.Effects;

public class MapEffect : Effect
{
    private readonly EffectParameter _ambientLightColorParam;
    private readonly EffectParameter _worldViewProjParam;
    private readonly EffectParameter _lightWorldViewProjParam;
    private readonly EffectParameter _virtualLayerFillColorParam;
    private readonly EffectParameter _virtualLayerBorderColorParam;
    private DirectionalLight _lightSource;

    private Matrix _worldViewProj = Matrix.Identity;
    private Matrix _lightWorldViewProj = Matrix.Identity;

    private Vector3 _ambientLightColor = Vector3.One;

    private Vector4 _virtualLayerFillColor = new(0.2f, 0.2f, 0.2f, 0.1f);
    private Vector4 _virtualLayerBorderColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    public Matrix WorldViewProj
    {
        get { return _worldViewProj; }
        set { _worldViewProj = value; }
    }

    public Matrix LightWorldViewProj
    {
        get { return _lightWorldViewProj; }
        set { _lightWorldViewProj = value; }
    }

    public Vector3 AmbientLightColor
    {
        get { return _ambientLightColor; }

        set { _ambientLightColor = value; }
    }

    public Vector4 VirtualLayerFillColor
    {
        set => _virtualLayerFillColor = value;
    }

    public Vector4 VirtualLayerBorderColor
    {
        set => _virtualLayerBorderColor = value;
    }

    public DirectionalLight LightSource
    {
        get { return _lightSource; }
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
        (device, GetResource("CentrED.Renderer.Effects.Shaders.MapEffect.fxc"))
    {
    }
    
    public MapEffect(GraphicsDevice device, byte[] effectCode) : base(device, effectCode)
    {
        _ambientLightColorParam = Parameters["AmbientLightColor"];
        _worldViewProjParam = Parameters["WorldViewProj"];
        _lightWorldViewProjParam = Parameters["LightWorldViewProj"];
        _virtualLayerFillColorParam = Parameters["VirtualLayerFillColor"];
        _virtualLayerBorderColorParam = Parameters["VirtualLayerBorderColor"];

        _lightSource = new DirectionalLight
        (
            Parameters["DirectionalLightDirection"],
            Parameters["DirectionalLightDiffuseColor"],
            Parameters["DirectionalLightSpecularColor"],
            null
        );
    }

    protected override void OnApply()
    {
        _worldViewProjParam.SetValue(_worldViewProj);
        _lightWorldViewProjParam.SetValue(_lightWorldViewProj);
        _ambientLightColorParam.SetValue(new Vector4(_ambientLightColor, 1));
        _virtualLayerFillColorParam.SetValue(_virtualLayerFillColor);
        _virtualLayerBorderColorParam.SetValue(_virtualLayerBorderColor);
    }
}