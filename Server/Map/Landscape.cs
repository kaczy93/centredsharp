//Server/ULandscape.pas

using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cedserver;
using Shared;
using Shared.MulProvider;

namespace Server;

//TLandscape
public class Landscape {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct StaticInfo(ushort X = 0, ushort Y = 0, sbyte Z = 0, ushort TileId = 0, ushort Hue = 0) {
        public StaticInfo(BinaryReader buffer) : this(0) { // We can for sure improve this
            X = buffer.ReadUInt16();
            Y = buffer.ReadUInt16();
            Z = buffer.ReadSByte();
            TileId = buffer.ReadUInt16();
            Hue = buffer.ReadUInt16();
        }
    }

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

    public ushort GetBlockId(ushort x, ushort y) {
        return (ushort)(x / 8 * Height + y / 8);
    }

    public ushort GetCellId(ushort x, ushort y) {
        return (ushort)(y % 8 * 8 + x % 8);
    }

    public ushort GetSubBlockId(ushort x, ushort y) {
        return (ushort)(y / 8 * Width + x / 8);
    }

    public Landscape(string mapPath, string staticsPath, string staidxPath, string tileDataPath, string radarcolPath,
        ushort width,
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
            _blockSubscriptions = new List<NetState>[Width * Height];
            for (int blockId = 0; blockId < Width * Height; blockId++) {
                _blockSubscriptions[blockId] = new List<NetState>(); //This is non typed linked list originally
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
    private List<NetState>[] _blockSubscriptions;

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
                return block.Cells[GetCellId(x, y)];
            }
        }

        return null;
    }

    public List<StaticItem>? GetStaticList(ushort x, ushort y) {
        if (x <= CellWidth && y <= CellHeight) {
            var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
            if (block != null) {
                return block.Cells[GetCellId(x, y)];
            }
        }

        return null;
    }

    public List<NetState>? GetBlockSubscriptions(ushort x, ushort y) {
        if (x <= Width && y <= Height) {
            return _blockSubscriptions[y * Width + x];
        }

        return null;
    }

    private void OnDrawMapPacket(BinaryReader buffer, NetState ns) {
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var cell = GetMapCell(x, y);
        if (cell == null) return;

        cell.Altitude = buffer.ReadSByte();
        cell.TileId = buffer.ReadUInt16();

        var packet = new DrawMapPacket(cell);
        var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }

        UpdateRadar(x, y);
    }

    private void OnInsertStaticPacket(BinaryReader buffer, NetState ns) {
        var x = buffer.ReadUInt16();
        var y = buffer.ReadUInt16();
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;

        var staticItem = new StaticItem();
        staticItem.X = x;
        staticItem.Y = y;
        staticItem.Z = buffer.ReadSByte();
        staticItem.TileId = buffer.ReadUInt16();
        staticItem.Hue = buffer.ReadUInt16();
        var targetStaticList = block.Cells[GetCellId(x,y)];
        targetStaticList.Add(staticItem);
        SortStaticList(targetStaticList);
        staticItem.Owner = block;

        var packet = new InsertStaticPacket(staticItem);
        var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }

        UpdateRadar(x, y);
    }

    private void OnDeleteStaticPacket(BinaryReader buffer, NetState ns) {
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;

        var statics = block.Cells[GetCellId(x,y)];
        for (var i = 0; i < statics.Count; i++) {
            var staticItem = statics[i];
            if (staticItem.Z != staticInfo.Z || 
                staticItem.TileId != staticInfo.TileId ||
                staticItem.Hue != staticInfo.Hue) continue;

            var packet = new DeleteStaticPacket(staticItem);
            
            staticItem.Delete();
            statics.RemoveAt(i);
            
            var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
            foreach (var netState in subscriptions) {
                CEDServer.SendPacket(netState, packet);
            }

            UpdateRadar(x, y);

            break;
        }
    }

    private void OnElevateStaticPacket(BinaryReader buffer, NetState ns) {
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;
        
        var statics = block.Cells[GetCellId(x,y)];
        
        var staticItem = statics.Find(s => s.Z == staticInfo.Z && s.TileId == staticInfo.TileId && s.Hue == staticInfo.Hue);
        if (staticItem == null) return;
       
        var newZ = buffer.ReadSByte();
        var packet = new ElevateStaticPacket(staticItem, newZ);
        staticItem.Z = newZ;
        SortStaticList(statics);
        
        var subscriptions = _blockSubscriptions[GetSubBlockId(x,y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }

        UpdateRadar(x, y);
    }

    private void OnMoveStaticPacket(BinaryReader buffer, NetState ns) {
        var staticInfo = new StaticInfo(buffer);
        var newX = (ushort)Math.Clamp(buffer.ReadUInt16(), 0, CellWidth - 1);
        var newY = (ushort)Math.Clamp(buffer.ReadUInt16(), 0, CellHeight - 1);
        
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, staticInfo.X, staticInfo.Y)) return;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, newX, newY)) return;

        if (staticInfo.X == newX && staticInfo.Y == newY) return;
        
        if((Math.Abs(staticInfo.X - newX) > 8 || Math.Abs(staticInfo.Y - newY) > 8) && 
           !PacketHandlers.ValidateAccess(ns, AccessLevel.Administrator)) return;

        var sourceBlock = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));
        if (sourceBlock == null || targetBlock == null) return;

        var statics = sourceBlock.Cells[GetCellId(staticInfo.X, staticInfo.Y)];
        int i;
        StaticItem? staticItem = null;
        for(i = 0;i < statics.Count; i++) {
            if (statics[i].Z != staticInfo.Z || 
                statics[i].TileId != staticInfo.TileId ||
                statics[i].Hue != staticInfo.Hue) continue;
            staticItem = statics[i];
            break;
        }

        if (staticItem == null) return;
        var deletePacket = new DeleteStaticPacket(staticItem);
        var movePacket = new MoveStaticPacket(staticItem, newX, newY);

        i = statics.IndexOf(staticItem);
        statics.RemoveAt(i);

        statics = targetBlock.Cells[GetCellId(newX, newY)];
        statics.Add(staticItem);
        staticItem.UpdatePos(newX, newY, staticItem.Z);
        staticItem.Owner = targetBlock;

        var insertPacket = new InsertStaticPacket(staticItem);
        
        SortStaticList(statics);
        
        var sourceSubscriptions = _blockSubscriptions[GetSubBlockId(staticInfo.X, staticInfo.Y)];
        var targetSubscriptions = _blockSubscriptions[GetSubBlockId(newX, newY)];
        
        foreach (var netState in sourceSubscriptions) {
            if(targetSubscriptions.Contains(netState))
                CEDServer.SendPacket(netState, movePacket);
            else {
                CEDServer.SendPacket(netState, deletePacket);
            }
        }

        foreach (var netState in sourceSubscriptions) {
            CEDServer.SendPacket(netState, insertPacket);
        }

        UpdateRadar(staticInfo.X, staticInfo.Y);
        UpdateRadar(newX, newY);
    }

    private void OnHueStaticPacket(BinaryReader buffer, NetState ns) {
        var staticInfo = new StaticInfo(buffer);
        var x = staticInfo.X;
        var y = staticInfo.Y;
        if (!PacketHandlers.ValidateAccess(ns, AccessLevel.Normal, x, y)) return;
        
        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (block == null) return;
        
        var statics = block.Cells[GetCellId(x, y)];
        
        var staticItem = statics.Find(s => s.Z == staticInfo.Z && s.TileId == staticInfo.TileId && s.Hue == staticInfo.Hue);
        if (staticItem == null) return;
       
        var newHue = buffer.ReadUInt16();
        var packet = new HueStaticPacket(staticItem, newHue);
        staticItem.Hue = newHue;
        
        var subscriptions = _blockSubscriptions[GetSubBlockId(x, y)];
        foreach (var netState in subscriptions) {
            CEDServer.SendPacket(netState, packet);
        }
    }

    private void OnLargeScaleCommandPacket(BinaryReader buffer, NetState ns) { }

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
        _radarMap.Update((ushort)(x / 8), (ushort)(y / 8), (ushort)(tile.TileId + (tile is StaticItem ? 0x4000 : 0)));
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
                staticItem.UpdatePriorities(TileDataProvider.StaticTiles[staticItem.TileId], i + 1);
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
        }
        else if (worldBlock is StaticBlock) {
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