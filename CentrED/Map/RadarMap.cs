using CentrED.Client;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CentrED.Map; 

public  class RadarMap {
    private static RadarMap _instance;
    public static RadarMap Instance => _instance;
    
    private Texture2D _texture = null!;
    public Texture2D Texture => _texture;
    
    private RadarMap(GraphicsDevice gd) {
        CentrED.Client.Connected += () => {
            _texture = new Texture2D(gd, CentrED.Client.Width, CentrED.Client.Height );
            CentrED.Client.Send(new RequestRadarMapPacket());
        };
        
        CentrED.Client.RadarData += RadarData;
        CentrED.Client.RadarUpdate += RadarUpdate;
    }

    public static void Initialize(GraphicsDevice gd) {
        _instance = new RadarMap(gd);
    }

    private unsafe void RadarData(ushort[] data) {
        var width = CentrED.Client.Width;
        var height = CentrED.Client.Height;
        uint[] buffer = System.Buffers.ArrayPool<uint>.Shared.Rent(data.Length);
        for (ushort x = 0; x < width; x++) {
            for (ushort y = 0; y < height; y++) {
                buffer[y * width + x] = HuesHelper.Color16To32(data[x * height + y]) | 0xFF_00_00_00;
            }
        }

        fixed (uint* ptr = buffer) {
            _texture.SetDataPointerEXT(0, null, (IntPtr)ptr, data.Length * sizeof(uint));
        }
    }

    private void RadarUpdate(ushort x, ushort y, ushort color) {
        _texture.SetData(0, new Rectangle(x, y,1,1), new []{HuesHelper.Color16To32(color) | 0xFF_00_00_00},0,1);
    }
}