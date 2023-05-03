using System.Collections.ObjectModel;
using static CentrED.Network.LargeScaleOperation;

namespace CentrED.Server;

public abstract class LargeScaleOperation {
    public LargeScaleOperation(BinaryReader reader, Landscape landscape) {
        _landscape = landscape;
    }

    protected Landscape _landscape;

    public abstract void Validate();

    public abstract void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> staticTiles, ref bool[] additionalAffectedBlocks);

    protected static readonly Random random = new();
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

    public override void Validate() { }

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> staticTiles, ref bool[] additionalAffectedBlocks) {
        ushort x = (ushort)Math.Clamp(landTile.X + OffsetX, 0, _landscape.CellWidth - 1);
        ushort y = (ushort)Math.Clamp(landTile.Y + OffsetY, 0, _landscape.CellHeight - 1);
        var targetLandTile = _landscape.GetLandTile(x, y);
        var targetStaticsBlock = _landscape.GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        if (_erase) {
            foreach (var targetStatic in _landscape.GetStaticTiles(x, y)) {
                targetStaticsBlock.RemoveTile(targetStatic);
            }
        }

        targetLandTile.Id = landTile.Id;
        targetLandTile.Z = landTile.Z;
        
        switch (_type) {
            case CopyMove.Copy: {
                foreach (var staticTile in staticTiles) {
                    targetStaticsBlock.AddTile(
                        new StaticTile (
                            staticTile.Id,
                            x,
                            y,
                            staticTile.Z,
                            staticTile.Hue
                        )
                    );
                }
                break;
            }
            case CopyMove.Move: {
                foreach (var staticTile in staticTiles) {
                    staticTile.Owner!.RemoveTile(staticTile);
                    targetStaticsBlock.AddTile(staticTile);
                    staticTile.UpdatePos(x,y, staticTile.Z);
                }
                break;
            }
        }
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

    public override void Validate() { }

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> staticTiles, ref bool[] additionalAffectedBlocks) {
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
        
        foreach (var staticTile in staticTiles) {
            staticTile.Z = (sbyte)Math.Clamp(landTile.Z + diff, -128, 127);
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

    public override void Validate() {
        foreach (var tileId in _tileIds) {
            _landscape.AssertLandTileId(tileId);
        }
    }

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> staticTiles, ref bool[] additionalAffectedBlocks) {
        if (_tileIds.Length <= 0) return;

        landTile.Id = _tileIds[random.Next(_tileIds.Length)];
    }
}

public class LsDeleteStatics : LargeScaleOperation {
    public LsDeleteStatics(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        var count = reader.ReadUInt16();
        _tileIds = new ushort[count];
        for (int i = 0; i < count; i++) {
            _tileIds[i] = (ushort)(reader.ReadUInt16() - 0x4000);
        }
        _minZ = reader.ReadSByte();
        _maxZ = reader.ReadSByte();
    }
    
    private ushort[] _tileIds;
    private sbyte _minZ;
    private sbyte _maxZ;

    public override void Validate() { }

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> staticTiles, ref bool[] additionalAffectedBlocks) {
        var staticBlock = _landscape.GetStaticBlock((ushort)(landTile.X / 8), (ushort)(landTile.Y / 8));
        foreach (var staticTile in staticTiles) {
            if (staticTile.Z < _minZ || staticTile.Z > _maxZ) continue;
            
            if (_tileIds.Length > 0) {
                if (_tileIds.Contains(staticTile.Id)) {
                    staticBlock.RemoveTile(staticTile);
                }
            }
            else {
                staticBlock.RemoveTile(staticTile);
            }
        }
    }
}

public class LsInsertStatics : LargeScaleOperation {
    public LsInsertStatics(BinaryReader reader, Landscape landscape) : base(reader, landscape) {
        var count = reader.ReadUInt16();
        _tileIds = new ushort[count];
        for (int i = 0; i < count; i++) {
            _tileIds[i] = (ushort)(reader.ReadUInt16() - 0x4000);
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

    public override void Validate() {
        foreach (var tileId in _tileIds) {
            _landscape.AssertStaticTileId(tileId);
        }
    }

    public override void Apply(LandTile landTile, ReadOnlyCollection<StaticTile> staticTiles, ref bool[] additionalAffectedBlocks) {
        if (_tileIds.Length == 0 || random.Next(100) >= _probability) return;

        var staticItem = new StaticTile(
            _tileIds[random.Next(_tileIds.Length)], 
            landTile.X, 
            landTile.Y, 
            0, 
            0);
        switch (_placementType) {
            case StaticsPlacement.Terrain: {
                staticItem.Z = landTile.Z;
                break;
            }
            case StaticsPlacement.Top: {
                var topZ = landTile.Z;
                foreach (var staticTile in staticTiles) {
                    sbyte staticTop = Math.Clamp((sbyte)(staticTile.Z + _landscape.TileDataProvider.StaticTiles[staticTile.Id].Height), (sbyte)-128, (sbyte)127);
                    if (staticTop > topZ) topZ = staticTop;
                }
                staticItem.Z = topZ;
                break;
            }
            case StaticsPlacement.Fix: {
                staticItem.Z = _fixZ;
                break;
            }
        }
        var staticBlock = _landscape.GetStaticBlock((ushort)(staticItem.X / 8), (ushort)(staticItem.Y / 8));
        staticBlock.AddTile(staticItem);
    }
}



