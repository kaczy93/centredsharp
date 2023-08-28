using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED; 

public class HuesManager {
    
    public const int TEXTURE_HEIGHT = 3000;
    public const int TEXTURE_WIDTH = 32;
    public readonly Texture2D Texture;
    public static readonly SamplerState SamplerState = SamplerState.PointClamp;

    public unsafe HuesManager(GraphicsDevice gd) {
        Texture = new Texture2D(gd, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * TEXTURE_HEIGHT);

        fixed (uint* ptr = buffer) {
            HuesLoader.Instance.CreateShaderColors(buffer);
            Texture.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * TEXTURE_HEIGHT * sizeof(uint));
        }
        System.Buffers.ArrayPool<uint>.Shared.Return(buffer);
    }
}