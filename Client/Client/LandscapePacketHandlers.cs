﻿using CentrED.Network;

namespace CentrED.Client; 

public partial class Landscape {
    private void OnBlockPacket(BinaryReader reader, NetState<CentrEDClient> ns) {
        ns.LogDebug("OnBlockPacket");
        var index = new GenericIndex();
        while (reader.PeekChar() != -1) {
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
            foreach (var staticTile in staticBlock.Tiles) {
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
}