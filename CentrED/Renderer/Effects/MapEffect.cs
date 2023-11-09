using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer.Effects;

public class MapEffect : Effect
{
    private readonly EffectParameter _ambientLightColorParam;
    private readonly EffectParameter _worldViewProjParam;
    private readonly EffectParameter _lightWorldViewProjParam;
    private DirectionalLight _lightSource;

    private Matrix _worldViewProj = Matrix.Identity;
    private Matrix _lightWorldViewProj = Matrix.Identity;

    private Vector3 _ambientLightColor = Vector3.One;

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

    public MapEffect(GraphicsDevice device) : base
        (device, GetResource("CentrED.Renderer.Effects.Shaders.MapEffect.fxc"))
    {
        _ambientLightColorParam = Parameters["AmbientLightColor"];
        _worldViewProjParam = Parameters["WorldViewProj"];
        _lightWorldViewProjParam = Parameters["LightWorldViewProj"];

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
    }
}