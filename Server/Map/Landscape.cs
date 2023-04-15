using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Cedserver;
using Shared;
using Shared.MulProvider;
using Map = Shared.Map;

namespace Server;

public partial class Landscape {
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

    public static int GetId(ushort x, ushort y) {
        return HashCode.Combine(x, y);
    }

    public ushort GetBlockId(ushort x, ushort y) {
        return (ushort)(x / 8 * Height + y / 8);
    }

    public static ushort GetCellId(ushort x, ushort y) {
        return (ushort)(y % 8 * 8 + x % 8);
    }

    public ushort GetSubBlockId(ushort x, ushort y) {
        return (ushort)(y / 8 * Width + x / 8);
    }

    public Landscape(string mapPath, string staticsPath, string staidxPath, string tileDataPath, string radarcolPath,
        ushort width, ushort height, ref bool valid) {
        CEDServer.LogInfo("Loading Map");
        _map = File.Open(mapPath, FileMode.Open, FileAccess.ReadWrite);
        var fi = new FileInfo(mapPath);
        IsUop = fi.Extension == ".uop";
        if (IsUop) {
            string uopPattern = fi.Name.Replace(fi.Extension, "").ToLowerInvariant();
            ReadUOPFiles(uopPattern);
        }

        CEDServer.LogInfo($"Loaded {fi.Name}");
        CEDServer.LogInfo("Loading Statics");
        _statics = File.Open(staticsPath, FileMode.Open, FileAccess.ReadWrite);
        CEDServer.LogInfo("Loading StaIdx");
        _staidx = File.Open(staidxPath, FileMode.Open, FileAccess.ReadWrite);
        CEDServer.LogInfo("Loading Tiledata");
        _tileData = File.Open(tileDataPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        Width = width;
        Height = height;
        CellWidth = (ushort)(Width * 8);
        CellHeight = (ushort)(Height * 8);
        _ownsStreams = false;
        valid = Validate();
        if (valid) {
            CEDServer.LogInfo("Creating Cache");
            _blockCache = new BlockCache(OnRemovedCachedObject);
            CEDServer.LogInfo("Creating TileData");
            TileDataProvider = new TileDataProvider(_tileData, true);
            CEDServer.LogInfo("Creating Subscriptions");
            _blockSubscriptions = new Dictionary<int, List<NetState>>();

            CEDServer.LogInfo("Creating RadarMap");
            _radarMap = new RadarMap(this, _map, _staidx, _statics, radarcolPath);
            PacketHandlers.RegisterPacketHandler(0x06, 8, OnDrawMapPacket);
            PacketHandlers.RegisterPacketHandler(0x07, 10, OnInsertStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x08, 10, OnDeleteStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x09, 11, OnElevateStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x0A, 14, OnMoveStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x0B, 12, OnHueStaticPacket);
            PacketHandlers.RegisterPacketHandler(0x0E, 0, OnLargeScaleCommandPacket);

            _ownsStreams = true;
        }
    }

    public ushort Width { get; }
    public ushort Height { get; }
    public ushort CellWidth { get; }
    public ushort CellHeight { get; }
    private FileStream _map;
    private FileStream _statics;
    private FileStream _staidx;
    private FileStream _tileData;
    public bool IsUop { get; }
    private UopFile[] UOPFiles { get; set; }
    public TileDataProvider TileDataProvider { get; }
    private bool _ownsStreams;
    private RadarMap _radarMap;
    private BlockCache _blockCache;

    private Dictionary<int, List<NetState>> _blockSubscriptions;

    private void OnRemovedCachedObject(Block block) {
        if (block.MapBlock.Changed)
            SaveBlock(block.MapBlock);
        if (block.StaticBlock.Changed)
            SaveBlock(block.StaticBlock);
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
        var key = y * Width + x;
        if (x > Width || y > Height) {
            return null;
        }

        if (_blockSubscriptions.ContainsKey(key)) {
            return _blockSubscriptions[key];
        }
        else {
            var result = new List<NetState>();
            _blockSubscriptions.Add(key, result);
            return result;
        }
    }

    public MapBlock? GetMapBlock(ushort x, ushort y) {
        return GetBlock(x, y)?.MapBlock;
    }

    public SeparatedStaticBlock? GetStaticBlock(ushort x, ushort y) {
        return GetBlock(x, y)?.StaticBlock;
    }

    private Block? GetBlock(ushort x, ushort y) {
        if (x >= Width || y >= Height) return null;

        var block = _blockCache.Get(x, y);

        return block ?? LoadBlock(x, y);
    }

    public long GetBlockNumber(ushort x, ushort y) {
        return x * Height + y;
    }

    public long GetMapOffset(ushort x, ushort y) {
        long offset = GetBlockNumber(x, y) * 196;
        if (IsUop)
            offset = CalculateOffsetFromUOP(offset);
        return offset;
    }

    public long GetStaidxOffset(ushort x, ushort y) {
        return GetBlockNumber(x, y) * 12;
    }

    private Block LoadBlock(ushort x, ushort y) {
        _map.Position = GetMapOffset(x, y);
        var map = new MapBlock(_map, x, y);

        _staidx.Position = GetStaidxOffset(x, y);
        var index = new GenericIndex(_staidx);
        var statics = new SeparatedStaticBlock(_statics, index, x, y);
        statics.TileDataProvider = TileDataProvider;

        var result = new Block(map, statics);
        _blockCache.Add(result);
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
                CEDServer.LogError($"Cannot find Tiledata for the Static Item with ID {staticItems[i].TileId}.");
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
                CEDServer.LogError($"Cannot find Tiledata for the Static Item with ID {statics[i].TileId}.");
            }
        }

        statics.Sort();
    }

    public void Flush() {
        _blockCache.Clear();
        _map.Flush();
        _staidx.Flush();
        _statics.Flush();
    }
    
    public void Backup() {
        Flush();
        var logMsg = "Automatic backup in progress";
        CEDServer.LogInfo(logMsg);
        CEDServer.SendPacket(null, new ConnectionHandling.ServerStatePacket(ServerState.Other, logMsg));
        String backupDir;
        for (var i = Config.Autobackup.MaxBackups; i > 0; i--) {
            backupDir = $"{Config.Autobackup.Directory}/Backup{i}";
            if(Directory.Exists(backupDir))
                if (i == Config.Autobackup.MaxBackups)
                    Directory.Delete(backupDir, true);
                else 
                    Directory.Move(backupDir, $"{Config.Autobackup.Directory}/Backup{i + 1}");
        }
        backupDir = $"{Config.Autobackup.Directory}/Backup1";
        Directory.CreateDirectory(backupDir);
        foreach (var fs in new []{ _map, _staidx, _statics})
        {
            FileInfo fi = new FileInfo(fs.Name);
            using var backupStream = new FileStream($"{backupDir}/{fi.Name}", FileMode.CreateNew, FileAccess.Write);
            fs.Position = 0;
            fs.CopyTo(backupStream);
        }
        CEDServer.SendPacket(null, new ConnectionHandling.ServerStatePacket(ServerState.Running));
        CEDServer.LogInfo("Automatic backup finished.");
    }

    public void SaveBlock(MapBlock mapBlock) {
        CEDServer.LogDebug($"Saving mapBlock {mapBlock.X},{mapBlock.Y}");
        _map.Position = GetMapOffset(mapBlock.X, mapBlock.Y);
        mapBlock.Write(new BinaryWriter(_map));
        mapBlock.Changed = false;
    }

    public void SaveBlock(StaticBlock staticBlock) {
        CEDServer.LogDebug($"Saving staticBlock {staticBlock.X},{staticBlock.Y}");
        _staidx.Position = GetStaidxOffset(staticBlock.X, staticBlock.Y);
        var index = new GenericIndex(_staidx);
        var size = staticBlock.GetSize;
        if (size > index.Size || index.Lookup <= 0) {
            _statics.Position = _statics.Length;
            index.Lookup = (int)_statics.Position;
        }

        index.Size = size;
        if (size == 0) {
            index.Lookup = -1;
        }
        else {
            _statics.Position = index.Lookup;
            staticBlock.Write(new BinaryWriter(_statics));
        }

        _staidx.Seek(-12, SeekOrigin.Current);
        index.Write(new BinaryWriter(_staidx));
        staticBlock.Changed = false;
    }

    public long MapLength {
        get {
            if (IsUop)
                return UOPFiles.Sum(f => f.Length) - Map.BlockSize; //UOP have extra block at the end
            else {
                return _map.Length;
            }
        }
    }


    public bool Validate() {
        var blocks = Width * Height;
        var mapSize = blocks * Map.BlockSize;
        var staidxSize = blocks * GenericIndex.BlockSize;
        var mapFileBlocks = MapLength / Map.BlockSize;
        var staidxFileBlocks = _staidx.Length / GenericIndex.BlockSize;

        var valid = true;
        if (MapLength != mapSize) {
            CEDServer.LogError($"{_map.Name} file doesn't match configured size: {MapLength} != {mapSize}");
            CEDServer.LogInfo($"{_map.Name} seems to be {MapSizeHint()}");
            valid = false;
        }

        if (_staidx.Length != staidxSize) {
            CEDServer.LogError($"{_staidx.Name} file doesn't match configured size: {_staidx.Length} != {staidxSize}");
            CEDServer.LogInfo($"{_staidx.Name} seems to be {StaidxSizeHint()}");
            valid = false;
        }

        if (mapFileBlocks != staidxFileBlocks) {
            CEDServer.LogError(
                $"{_map.Name} file doesn't match {_staidx.Name} file in blocks: {mapFileBlocks} != {staidxFileBlocks} ");
            CEDServer.LogInfo($"{_map.Name} seems to be {MapSizeHint()}, and staidx seems to be {StaidxSizeHint()}");
            valid = false;
        }

        return valid;
    }

    private string MapSizeHint() {
        return MapLength switch {
            3_211_264 => "128x128 (map0 Pre-Alpha)",
            77_070_336 => "768x512 (map0,map1 Pre-ML)",
            89_915_392 => "896x512 (map0,map1 Post-ML)",
            11_289_600 => "288x200 (map2)",
            16_056_320 => "320x256 (map3) or 160x512(map5)",
            6_421_156 => "160x512 (map4)",
            _ => "Unknown size"
        };
    }

    private string StaidxSizeHint() {
        return _staidx.Length switch {
            196_608 => "128x128 (map0 Pre-Alpha)",
            4_718_592 => "768x512 (map0,map1 Pre-ML)",
            5_505_024 => "896x512 (map0,map1 Post-ML)",
            691_200 => "288x200 (map2)",
            983_040 => "320x256 (map3) or 160x512(map5)",
            393_132 => "160x512 (map4)",
            _ => "Unknown size"
        };
    }

    private void ReadUOPFiles(string pattern) {
        using var uopReader = new BinaryReader(_map, Encoding.UTF8, true);

        uopReader.BaseStream.Seek(0, SeekOrigin.Begin);

        if (uopReader.ReadInt32() != 0x50594D) {
            throw new ArgumentException("Bad UOP file.");
        }

        uopReader.ReadInt64(); // version + signature
        long nextBlock = uopReader.ReadInt64();
        uopReader.ReadInt32(); // block capacity
        int count = uopReader.ReadInt32();

        UOPFiles = new UopFile[count];

        var hashes = new Dictionary<ulong, int>();

        for (int i = 0; i < count; i++) {
            string file = $"build/{pattern}/{i:D8}.dat";
            ulong hash = Uop.HashFileName(file);

            if (!hashes.ContainsKey(hash)) {
                hashes.Add(hash, i);
            }
        }

        uopReader.BaseStream.Seek(nextBlock, SeekOrigin.Begin);

        do {
            int filesCount = uopReader.ReadInt32();
            nextBlock = uopReader.ReadInt64();

            for (int i = 0; i < filesCount; i++) {
                long offset = uopReader.ReadInt64();
                int headerLength = uopReader.ReadInt32();
                int compressedLength = uopReader.ReadInt32();
                int decompressedLength = uopReader.ReadInt32();
                ulong hash = uopReader.ReadUInt64();
                uopReader.ReadUInt32(); // Adler32
                short flag = uopReader.ReadInt16();

                int length = flag == 1 ? compressedLength : decompressedLength;

                if (offset == 0) {
                    continue;
                }

                if (hashes.TryGetValue(hash, out int idx)) {
                    if (idx < 0 || idx > UOPFiles.Length) {
                        throw new IndexOutOfRangeException(
                            "hashes dictionary and files collection have different count of entries!");
                    }

                    UOPFiles[idx] = new UopFile(offset + headerLength, length);
                }
                else {
                    throw new ArgumentException(
                        $"File with hash 0x{hash:X8} was not found in hashes dictionary! EA Mythic changed UOP format!");
                }
            }
        } while (uopReader.BaseStream.Seek(nextBlock, SeekOrigin.Begin) != 0);
    }
    
    private long CalculateOffsetFromUOP(long offset)
    {
        long pos = 0;

        foreach (UopFile t in UOPFiles)
        {
            long currentPosition = pos + t.Length;

            if (offset < currentPosition)
            {
                return t.Offset + (offset - pos);
            }

            pos = currentPosition;
        }

        return _map.Length;
    }
}