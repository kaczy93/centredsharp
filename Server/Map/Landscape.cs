//Server/ULandscape.pas

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Shared;
using Shared.MulProvider;

namespace Server; 

//TLandscape
public class Landscape {
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct StaticInfo(ushort X, ushort Y, sbyte Z, ushort TileId, ushort Hue);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct AreaInfo(ushort Left, ushort Top, ushort Right, ushort Bottom) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ushort x, ushort y) {
            return x >= Left && x <= Right && 
                   y >= Top && y <= Bottom;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct WorldPoint(ushort X, ushort Y);
    
    public static int GetId(ushort x, ushort y) {
        return (x & 0x7FFF) << 15 | y & 0x7FFF;
    }
    
    public Landscape(string map, string statics, string staidx, string tileData, string radarcol, ushort width,
        ushort height, bool valid) {
        
    }

    public Landscape(Stream map, Stream statics, Stream staidx, Stream tileData, Stream radarcol, ushort width,
        ushort height, bool valid) {
        
    }

    public ushort Width { get; }
    public ushort Height { get; }
    public ushort CellWidth { get; }
    public ushort CellHeight { get; }
    private Stream _map;
    private Stream _statics;
    private Stream _staidx;
    private Stream _tileData;
    public TileDataProvider TileDataProvider { get; }
    private bool _ownsStreams;
    private RadarMap _radarMap;
    private BlockCache _blockCache;
    private ArrayList[] _blockSubscriptions;

    private void OnRemovedCachedObject(Block block) {
        
    }

    public MapCell GetMapCell(ushort x, ushort y) {
        
    }

    public List<StaticItem> GetStaticList(ushort x, ushort y) {
        
    }

    public ArrayList GetBlockSubscriptions(ushort x, ushort y) {
        
    }

    private void OnDrawMapPacket(BinaryReader buffer, NetState ns) {
        
    }

    private void OnInsertStaticPacket(BinaryReader buffer, NetState ns) {
        
    }

    private void OnDeleteStaticPacket(BinaryReader buffer, NetState ns) {
        
    }

    private void OnElevateStaticPacket(BinaryReader buffer, NetState ns) {
        
    }

    private void OnMoveStaticPacket(BinaryReader buffer, NetState ns) {
        
    }

    private void OnHueStaticPacket(BinaryReader buffer, NetState ns) {
        
    }

    private void OnLargeScaleCommandPacket(BinaryReader buffer, NetState ns) {
        
    }
    
    public ArrayList BlockSubscriptions(ushort coordX, ushort coordY) {
        throw new NotImplementedException();
    }
    
    
    public MapBlock? MapBlock(ushort x, ushort y) {
        throw new NotImplementedException();
    }

    public StaticBlock? StaticBlock(ushort x, ushort y) {
        throw new NotImplementedException();
    }
    
    public Block? LoadBlock(ushort x, ushort y) {
        
    }

    public void UpdateRadar(ushort x, ushort y) {
        
    }

    public sbyte EffectiveAltitute(MapCell tile) {
        
    }

    public sbyte LandAlt(ushort x, ushort y, sbyte defaultValue) {
        
    }

    public void Flush() {
        throw new NotImplementedException();
    }

    public void SaveBlock(WorldBlock worldBlock) {
        
    }

    public bool Validate() {
        
    }
}