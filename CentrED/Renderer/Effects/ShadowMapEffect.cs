using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Renderer.Effects;

public class ShadowMapEffect : Effect
{
    private readonly EffectParameter _worldViewProjParam;

    private Matrix _worldViewProj = Matrix.Identity;

    public Matrix WorldViewProj {
        get { return _worldViewProj; }
        set { _worldViewProj = value; }
    }

    protected static byte[] GetResource(string name)
    {
        Stream? stream = typeof(ShadowMapEffect).Assembly.GetManifestResourceStream(name);

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

    public ShadowMapEffect(GraphicsDevice device)
        : base(device, GetResource("CentrED.Renderer.Effects.Shaders.ShadowMapEffect.fxc"))
    {
        _worldViewProjParam = Parameters["WorldViewProj"];
    }

    protected override void OnApply()
    {
        _worldViewProjParam.SetValue(_worldViewProj);
    }
}
