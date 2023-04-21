using System.Collections.ObjectModel;

namespace CentrED.Server;

public enum CopyMove {
    Copy = 0, Move = 1
}

public enum SetAltitude {
    Terrain = 1, Relative = 2
}

public enum StaticsPlacement {
    Terrain = 1, Top = 2, Fix = 3
}

public abstract class LargeScaleOperation {
    public LargeScaleOperation(BinaryReader reader, Landscape landscape) {
        _landscape = landscape;
    }

    protected Landscape _landscape;

    public abstract void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> statics, ref bool[] additionalAffectedBlocks);

    protected static readonly Random random = new Random();
}

public class LsCopyMove : LargeScaleOperation {
    public LsCopyMove(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        _type = (CopyMove)reader.ReadByte();
        OffsetX = reader.ReadInt32();
        OffsetY = reader.ReadInt32();
        _erase = reader.ReadBoolean();
    }
    
    private CopyMove _type;
    public int OffsetX { get; }
    public int OffsetY { get; }
    private bool _erase;

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> statics, ref bool[] additionalAffectedBlocks) {
        ushort x = (ushort)Math.Clamp(landTile.X + OffsetX, 0, _landscape.CellWidth - 1);
        ushort y = (ushort)Math.Clamp(landTile.Y + OffsetY, 0, _landscape.CellHeight - 1);
        var targetCell = _landscape.GetLandTile(x, y);
        var targetStaticsBlock = _landscape.GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        var targetStatics = targetStaticsBlock.CellItems(Landscape.GetTileId(x, y));
        if (_erase) {
            foreach (var targetStatic in targetStatics) {
                targetStatic.Delete();
                targetStaticsBlock.Tiles.Remove(targetStatic);
            }
        }

        targetCell.TileId = landTile.TileId;
        targetCell.Z = landTile.Z;

      
        switch (_type) {
            case CopyMove.Copy: {
                foreach (var staticItem in statics) {
                    targetStaticsBlock.Tiles.Add(
                        new StaticTile {
                            X = x,
                            Y = y,
                            Z = staticItem.Z,
                            TileId = staticItem.TileId,
                            Hue = staticItem.Hue,
                            Owner = targetStaticsBlock
                        }
                    );
                }
                break;
            }
            case CopyMove.Move: {
                foreach (var staticItem in statics) {
                    staticItem.UpdatePos(x,y, staticItem.Z);
                    staticItem.Owner = targetStaticsBlock;
                    targetStaticsBlock.Tiles.Add(staticItem);
                }
                break;
            }
        }
        _landscape.SortStaticList(targetStaticsBlock.Tiles);
        additionalAffectedBlocks[_landscape.GetBlockId(x,y)] = true;
    }
}

public class LsSetAltitude : LargeScaleOperation {
    public LsSetAltitude(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        _type = (SetAltitude)reader.ReadByte();
        switch (_type) {
            case SetAltitude.Terrain: {
                _minZ = reader.ReadSByte();
                _maxZ = reader.ReadSByte();
                break;
            }
            case SetAltitude.Relative: {
                _relativeZ = reader.ReadSByte();
                break;
            }
        }
    }

    private SetAltitude _type;
    private sbyte _minZ;
    private sbyte _maxZ;
    private sbyte _relativeZ;

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> statics, ref bool[] additionalAffectedBlocks) {
        sbyte diff = 0;
        switch (_type) {
            case SetAltitude.Terrain: {
                var newZ = (sbyte)(_minZ + random.Next(_maxZ - _minZ + 1));
                diff = (sbyte)(newZ - landTile.Z);
                landTile.Z = newZ;
                break;
            }
            case SetAltitude.Relative: {
                diff = _relativeZ;
                landTile.Z = (sbyte)Math.Clamp(landTile.Z + diff, -128, 127);
                break;
            }
        }
        
        foreach (var staticItem in statics) {
            staticItem.Z = (sbyte)Math.Clamp(landTile.Z + diff, -128, 127);
        }
    }
}

public class LsDrawTerrain : LargeScaleOperation {
    public LsDrawTerrain(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        var count = reader.ReadUInt16();
        _tileIds = new ushort[count];
        for (int i = 0; i < count; i++) {
            _tileIds[i] = reader.ReadUInt16();
        }
    }

    private ushort[] _tileIds;

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> statics, ref bool[] additionalAffectedBlocks) {
        if (_tileIds.Length <= 0) return;

        landTile.TileId = _tileIds[random.Next(_tileIds.Length)];
    }
}

public class LsDeleteStatics : LargeScaleOperation {
    public LsDeleteStatics(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        var count = reader.ReadUInt16();
        _tileIds = new ushort[count];
        for (int i = 0; i < count; i++) {
            _tileIds[i] = reader.ReadUInt16();
        }
        _minZ = reader.ReadSByte();
        _maxZ = reader.ReadSByte();
    }
    
    private ushort[] _tileIds;
    private sbyte _minZ;
    private sbyte _maxZ;

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> statics, ref bool[] additionalAffectedBlocks) {
        foreach (var staticItem in statics) {
            var staticBlock = _landscape.GetStaticBlock((ushort)(staticItem.X / 8), (ushort)(staticItem.Y / 8));

            if (staticItem.Z < _minZ || staticItem.Z > _maxZ) continue;
            
            if (_tileIds.Length > 0) {
                if (_tileIds.Contains((ushort)(staticItem.TileId + 0x4000))) {
                    staticItem.Delete();
                    staticBlock.Tiles.Remove(staticItem);
                }
            }
            else {
                staticItem.Delete();
                staticBlock.Tiles.Remove(staticItem);
            }
        }
    }
}

public class LsInsertStatics : LargeScaleOperation {
    public LsInsertStatics(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        var count = reader.ReadUInt16();
        _tileIds = new ushort[count];
        for (int i = 0; i < count; i++) {
            _tileIds[i] = reader.ReadUInt16();
        }
        _probability = reader.ReadByte();
        _placementType = (StaticsPlacement)reader.ReadByte();
        if (_placementType == StaticsPlacement.Fix) {
            _fixZ = reader.ReadSByte();
        }
    }
    
    private ushort[] _tileIds;
    private byte _probability;
    private StaticsPlacement _placementType;
    private sbyte _fixZ;

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> statics, ref bool[] additionalAffectedBlocks) {
        if (_tileIds.Length == 0 || random.Next(100) >= _probability) return;

        var staticItem = new StaticTile {
            X = landTile.X,
            Y = landTile.Y,
            TileId = (ushort)(_tileIds[random.Next(_tileIds.Length)] - 0x4000),
            Hue = 0
        };
        switch (_placementType) {
            case StaticsPlacement.Terrain: {
                staticItem.Z = landTile.Z;
                break;
            }
            case StaticsPlacement.Top: {
                var topZ = landTile.Z;
                foreach (var @static in statics) {
                    sbyte staticTop = Math.Clamp((sbyte)(@static.Z + _landscape.TileDataProvider.StaticTiles[@static.TileId].Height), (sbyte)-128, (sbyte)127);
                    if (staticTop > topZ) topZ = staticTop;
                }
                staticItem.Z = topZ; //This is missing in original code
                break;
            }
            case StaticsPlacement.Fix: {
                staticItem.Z = _fixZ;
                break;
            }
        }
        var staticBlock = _landscape.GetStaticBlock((ushort)(staticItem.X / 8), (ushort)(staticItem.Y / 8));
        staticItem.Owner = staticBlock;
        staticBlock.Tiles.Add(staticItem);
    }
}



