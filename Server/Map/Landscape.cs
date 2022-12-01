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
        return ((x & 0x7FFF) << 15 | y & 0x7FFF).ToString();
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
        
        _cacheItemPolicy = new CacheItemPolicy { RemovedCallback = OnRemovedCachedObject };
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

    private readonly CacheItemPolicy _cacheItemPolicy;
    private ArrayList[] _blockSubscriptions;

    private void OnRemovedCachedObject(CacheEntryRemovedArguments arguments) {
        if (arguments.CacheItem.Value is Block block) {
            if (block.MapBlock.Changed) SaveBlock(block.MapBlock);
            if (block.StaticBlock.Changed) SaveBlock(block.StaticBlock);
        }
    }

    public MapCell? GetMapCell(ushort x, ushort y) {
        if (x <= CellWidth && y <= CellHeight) {
            var block = GetMapBlock((ushort)(x / 8), (ushort)(y / 8));
            if (block != null) {
                return block.Cells[y % 8 * 8 + x % 8];
            }
        }

        return null;
    }

    public List<StaticItem>? GetStaticList(ushort x, ushort y) {
        if (x <= CellWidth && y <= CellHeight) {
            var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
            if (block != null) {
                return block.Cells[y % 8 * 8 + x % 8];
            }
        }

        return null;
    }

    public ArrayList? GetBlockSubscriptions(ushort x, ushort y) {
        if (x <= Width && y <= Height) {
            return _blockSubscriptions[y * Width + x];
        }

        return null;
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
    
    public MapBlock? GetMapBlock(ushort x, ushort y) {
        return GetBlock(x, y)?.MapBlock;
    }

    public SeparatedStaticBlock? GetStaticBlock(ushort x, ushort y) {
        return GetBlock(x, y)?.StaticBlock;
    }

    private Block? GetBlock(ushort x, ushort y) {
        if (x >= Width || x >= Height) return null;
        
        var o = _blockCache.Get(GetId(x, y));
        if (o is Block block) {
            return block;
        }

        return LoadBlock(x, y);
    }
    
    public Block? LoadBlock(ushort x, ushort y) {
        _map.Position = (x * Height + y) * 196;
        var map = new MapBlock(_map, x, y);

        _staidx.Position = (x * Height + y) * 12;
        var index = new GenericIndex(_staidx);
        var statics = new SeparatedStaticBlock(_statics, index, x, y);
        statics.TileDataProvider = TileDataProvider;
        
        var result = new Block(map, statics);
        _blockCache.Set(GetId(x, y), result, _cacheItemPolicy);
        return result;
    }

    public void UpdateRadar(ushort x, ushort y) {
        if (x % 8 != 0 || y % 8 != 0) return;
        
        var staticItems = GetStaticList(x, y);
        if (staticItems == null) return;
        
        var tiles = new List<WorldItem>();
        var mapTile = GetMapCell(x, y);
        if (mapTile != null) {
            mapTile.Priority = GetEffectiveAltitute(mapTile);
            mapTile.PriorityBonus = 0;
            mapTile.PrioritySolver = 0;
            tiles.Add(mapTile);
        }

        for (var i = 0; i < staticItems.Count; i++) {
            var staticItem = staticItems[i];
            if (staticItem.TileId < TileDataProvider.StaticCount) {
                staticItem.UpdatePriorities(TileDataProvider.StaticTiles[staticItem.TileId], i);
            }
            else {
                //log.error($"Cannot find Tiledata for the Static Item with ID {staticItems[i].TileID}.");
            }
            tiles.Add(staticItem);
        }
        tiles.Sort();

        if (tiles.Count <= 0) return;
        
        var tile = tiles.Last();
        _radarMap.Update((ushort)(x/8), (ushort)(y/8), (ushort)(tile.TileId + (tile is StaticItem ? 0x4000 : 0)));
    }

    public sbyte GetLandAlt(ushort x, ushort y, sbyte defaultValue) {
        if (x < CellWidth && y < CellHeight) { // Maybe use <= like in other places?
            return GetMapCell(x, y).Altitude;
        }

        return defaultValue;
    }

    public sbyte GetEffectiveAltitute(MapCell tile) {
        var north = tile.Altitude;
        var west = GetLandAlt(tile.X, (ushort)(tile.Y + 1), north);
        var south = GetLandAlt((ushort)(tile.X + 1), (ushort)(tile.Y + 1), north);
        var east = GetLandAlt((ushort)(tile.X + 1), tile.Y, north);

        if (Math.Abs(north - south) > Math.Abs(west - east)) {
            return (sbyte)(north + south / 2);
        }
        else {
            return (sbyte)((west + east) / 2);
        }
    }

    public void SortStaticList(List<StaticItem> statics) {
        for (int i = 0; i < statics.Count; i++) {
            var staticItem = statics[i];
            if (staticItem.TileId < TileDataProvider.StaticCount) {
                staticItem.UpdatePriorities(TileDataProvider.StaticTiles[staticItem.TileId], i+1);
            }
            else {
                //log.error($"Cannot find Tiledata for the Static Item with ID {staticItems[i].TileID}.");
            }
        }
        statics.Sort();
    }

    public void Flush() {
        _blockCache.Dispose();
        _blockCache = MemoryCache.Default;
    }

    public void SaveBlock(WorldBlock worldBlock) {
        if (worldBlock is MapBlock) {
            _map.Position = (worldBlock.X * Height + worldBlock.Y) * 196;
            worldBlock.Write(new BinaryWriter(_map));
            worldBlock.Changed = false;
        } else if (worldBlock is StaticBlock) {
            _staidx.Position = (worldBlock.X * Height + worldBlock.Y) * 12;
            var index = new GenericIndex(_staidx);
            var size = worldBlock.GetSize;
            if (size > index.Size || index.Lookup < 0) {
                _statics.Position = _statics.Length;
                index.Lookup = (int)_statics.Position;
            }
            index.Size = size;
            if (size == 0) {
                index.Lookup = -1;
            }
            else {
                _statics.Position = index.Lookup;
                worldBlock.Write(new BinaryWriter(_statics));
            }

            _staidx.Seek(-12, SeekOrigin.Current);
            index.Write(new BinaryWriter(_staidx));
            worldBlock.Changed = false;
        }
    }

    public bool Validate() {
        var blocks = Width * Height;
        _staidx.Seek(0, SeekOrigin.End); //Workaround?
        return _map.Length == blocks * 196 && _staidx.Position == blocks * 12;
    }
}