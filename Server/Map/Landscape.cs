//Server/ULandscape.pas

using System.Collections;
using System.Runtime.Caching;
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
    
    public static string GetId(ushort x, ushort y) {
        return (x & 0x7FFF) << 15 | y & 0x7FFF;
    }
    
    public Landscape(string mapPath, string staticsPath, string staidxPath, string tileDataPath, string radarcolPath, ushort width,
        ushort height, ref bool valid) {
        Console.Write($"[{DateTime.Now}] Loading Map");
        _map = File.Open(mapPath, FileMode.Open, FileAccess.ReadWrite);
        Console.Write(", Statics");
        _statics = File.Open(staticsPath, FileMode.Open, FileAccess.ReadWrite);
        Console.Write(", StaIdx");
        _staidx = File.Open(staidxPath, FileMode.Open, FileAccess.ReadWrite);
        Console.WriteLine(", Tiledata");
        _tileData = File.Open(tileDataPath, FileMode.Open, FileAccess.ReadWrite);
        Width = width;
        Height = height;
        CellWidth = (ushort)(Width * 8);
        CellHeight = (ushort)(Height * 8);
        _ownsStreams = false;
        valid = Validate();
        if (valid) {
            Console.Write($"[{DateTime.Now}] Creating Cache");
            _blockCache = MemoryCache.Default; //Original uses custommade CacheManager and size is 256 objects
            Console.Write(", TileData");
            TileDataProvider = new TileDataProvider(tileDataPath);
            Console.Write(", Subscriptions");
            _blockSubscriptions = new ArrayList[Width * Height];
            for (int blockId = 0; blockId < Width * Height; blockId++) {
                _blockSubscriptions[blockId] = new ArrayList(); //This is non typed linked list originally
            }
            Console.WriteLine(", RadarMap");
            _radarMap = new RadarMap(_map, _statics, _staidx, Width, Height, radarcolPath);
            PacketHandlers.RegisterPacketHandler(0x06, 8, OnDrawMapPacket);
            PacketHandlers.RegisterPacketHandler(0x07, 10, OnInsertStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x08, 10, OnDeleteStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x09, 11, OnElevateStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x0A, 14, OnMoveStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x0B, 12, OnHueStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x0E, 8, OnLargeScaleCommandPacket);
        }
        _ownsStreams = true;
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
    private MemoryCache _blockCache;

    private readonly CacheItemPolicy _cacheItemPolicy = new()
        { RemovedCallback = OnRemovedCachedObject };
    private ArrayList[] _blockSubscriptions;

    private static void OnRemovedCachedObject(CacheEntryRemovedArguments arguments) {
        //take block from arguments
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
        var result = new Block(map, statics);
        _blockCache.Set(GetId(x, y), result, _cacheItemPolicy);
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