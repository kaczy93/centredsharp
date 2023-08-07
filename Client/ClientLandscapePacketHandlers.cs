using CentrED.Network;

namespace CentrED.Client; 

public partial class ClientLandscape {
    private void OnBlockPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnBlockPacket");
        var index = new GenericIndex();
        while (reader.BaseStream.Position < reader.BaseStream.Length) {
            var coords = new BlockCoords(reader);

            var landBlock = new LandBlock(x: coords.X, y: coords.Y, reader: reader);
            foreach (var landTile in landBlock.Tiles) {
                landTile.OnIdChanged = (tile, newTileId) =>
                    ns.Send(new DrawMapPacket(tile.X, tile.Y, tile.Z, newTileId));
                landTile.OnZChanged = (tile, newZ) =>
                    ns.Send(new DrawMapPacket(tile.X, tile.Y, newZ, tile.Id));
            }
            var staticsCount = reader.ReadUInt16();
            if(staticsCount > 0 )
                index.Lookup = (int)reader.BaseStream.Position;
            else {
                index.Lookup = -1;
            }
            index.Length = StaticTile.Size * staticsCount;
            
            var staticBlock = new StaticBlock(reader, index, coords.X, coords.Y);
            foreach (var staticTile in staticBlock.AllTiles()) {
                staticTile.OnIdChanged = (tile, newId) => {
                    ns.Send(new DeleteStaticPacket(tile));
                    ns.Send(new InsertStaticPacket(tile.X, tile.Y, tile.Z, newId, tile.Hue));
                };
                staticTile.OnPosChanged = (tile, newX, newY) =>
                    ns.Send(new MoveStaticPacket(tile, newX, newY));
                staticTile.OnZChanged = (tile, newZ) =>
                    ns.Send(new ElevateStaticPacket(tile, newZ));
                staticTile.OnHueChanged = (tile, newHue) =>
                    ns.Send(new HueStaticPacket(tile, newHue));
            }
            BlockCache.Add(new Block(landBlock, staticBlock));
        }
    }
    
    private void OnDrawMapPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnDrawMapPacket");
        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();

        var tile = GetLandTile(x, y);

        tile.Z = reader.ReadSByte();
        var newId = reader.ReadUInt16();
        // AssertLandTileId(newId); //Bring me back once we have TileDataProvider in client :)
        tile.Id = newId;

        OnLandChanged(tile);
    }

    private void OnInsertStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnInsertStaticPacket");
        var staticInfo = new StaticInfo(reader);

        var block = GetStaticBlock((ushort)(staticInfo.X /8), (ushort)(staticInfo.Y / 8));
        var newTile = new StaticTile(staticInfo);
        // AssertStaticTileId(newTile.Id);
        // AssertHue(newTile.Hue);
        
        block.AddTile(newTile);
        // block.SortTiles(TileDataProvider);

        OnStaticTileAdded(newTile);
    }

    private void OnDeleteStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnDeleteStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var x = staticInfo.X;
        var y = staticInfo.Y;

        var block = GetStaticBlock((ushort)(x / 8), (ushort)(y / 8));
        var staticTile = new StaticTile(staticInfo);
        var removed = block.RemoveTile(staticTile);

        if (!removed) {
            ns.LogError($"OnDeleteStaticPacket static not found {staticInfo}");
            return;
        }
        
        OnStaticTileRemoved(staticTile);
    }

    private void OnElevateStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnElevateStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newZ = reader.ReadSByte();

        var tile = GetStaticTiles(staticInfo.X, staticInfo.Y).FirstOrDefault(staticInfo.Match);
        if (tile == null) {
            ns.LogError($"OnElevateStaticPacket static not found {staticInfo}");
            return;
        }

        tile.Z = newZ;
        OnStaticTileElevated(tile);
    }

    private void OnMoveStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnMoveStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newX = reader.ReadUInt16();
        var newY = reader.ReadUInt16();

        var sourceBlock = GetStaticBlock((ushort)(staticInfo.X / 8), (ushort)(staticInfo.Y / 8));
        var targetBlock = GetStaticBlock((ushort)(newX / 8), (ushort)(newY / 8));
        var tile = new StaticTile(staticInfo);
        
        var removed = sourceBlock.RemoveTile(tile);
        if (!removed) {
            ns.LogError($"OnMoveStaticPacket static not found {staticInfo}");
            return;
        }
        tile.UpdatePos(newX, newY, tile.Z);
        targetBlock.AddTile(tile);
        // targetBlock.SortTiles(TileDataProvider);

        OnStaticTileRemoved(tile);
        OnStaticTileAdded(tile);
    }
    private void OnHueStaticPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("Client OnHueStaticPacket");
        var staticInfo = new StaticInfo(reader);
        var newHue = reader.ReadUInt16();
        // AssertHue(newTile.Hue);
        
        var tile = GetStaticTiles(staticInfo.X, staticInfo.Y).FirstOrDefault(staticInfo.Match);
        if (tile == null) {
            ns.LogError($"OnHueStaticPacket static not found {staticInfo}");
            return;
        }
        

        tile.Hue = newHue;
        OnStaticTileHued(tile);
    }
}