using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED;

public class HuesManager {
    private static HuesManager _instance;
    public static HuesManager Instance => _instance;

    public const int TEXTURE_WIDTH = 32;
    public readonly Texture2D Texture;
    public static readonly SamplerState SamplerState = SamplerState.PointClamp;
    public readonly int HuesCount;
    public readonly string[] Names;
    public readonly ushort[][] Colors;

    private unsafe HuesManager(GraphicsDevice gd) {
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

    public static void Initialize(GraphicsDevice gd) {
        _instance = new HuesManager(gd);
    }

    private enum HueMode {
        NONE = 0,
        HUED = 1,
        PARTIAL = 2
    }

    public Vector3 GetHueVector(StaticTile tile, float alpha = 1) {
        return GetHueVector(tile.Id, tile.Hue, alpha);
    }
    
    public Vector3 GetHueVector(ushort id, ushort hue, float alpha = 1) {
        var partial = TileDataLoader.Instance.StaticData[id].IsPartialHue;
        return GetHueVector(hue, partial, alpha);
    }

    public Vector3 GetHueVector(ushort hue, bool partial, float alpha = 1) {
        HueMode mode;

        if ((hue & 0x8000) != 0) {
            partial = true;
            hue &= 0x7FFF;
        }

        if (hue != 0) {
            // hue -= 1;
            mode = partial ? HueMode.PARTIAL : HueMode.HUED;
        }
        else {
            mode = HueMode.NONE;
        }

        return new Vector3(hue, (int)mode, alpha);
    }
}