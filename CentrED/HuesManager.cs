using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED; 

public class HuesManager {
    
    public const int TEXTURE_WIDTH = 32;
    public readonly Texture2D Texture;
    public static readonly SamplerState SamplerState = SamplerState.PointClamp;
    public readonly int HuesCount;
    public readonly string[] Names;
    public readonly ushort[][] Colors;
    

    public unsafe HuesManager(GraphicsDevice gd) {
        var huesLoader = HuesLoader.Instance;
        HuesCount = huesLoader.HuesCount;
        Texture = new Texture2D(gd, TEXTURE_WIDTH, HuesCount);
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(TEXTURE_WIDTH * HuesCount);

        fixed (uint* ptr = buffer) {
            huesLoader.CreateShaderColors(buffer);
            Texture.SetDataPointerEXT(0, null, (IntPtr)ptr, TEXTURE_WIDTH * HuesCount * sizeof(uint));
        }
        System.Buffers.ArrayPool<uint>.Shared.Return(buffer);
        var i = 0;
        Colors = new ushort[HuesCount][];
        Names = new string[HuesCount];
        foreach (var huesGroup in huesLoader.HuesRange) {
            foreach (var hueEntry in huesGroup.Entries) {
                Colors[i] = hueEntry.ColorTable;
                Names[i] = new string(hueEntry.Name);
                i++;
            }
        }
    }
}