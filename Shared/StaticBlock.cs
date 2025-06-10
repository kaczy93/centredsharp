using System.Collections.ObjectModel;
using CentrED.Network;

namespace CentrED;

public class StaticBlock
{
    public BaseLandscape Landscape { get; }
    public bool Changed { get; set; }
    public ushort X { get; }
    public ushort Y { get; }

    public StaticBlock
        (BaseLandscape landscape, BinaryReader? reader = null, GenericIndex? index = null, ushort x = 0, ushort y = 0)
    {
        Landscape = landscape;
        X = x;
        Y = y;
        _tiles = new List<StaticTile>[8, 8];

        if (reader != null && index?.Lookup >= 0 && index.Length > 0)
        {
            reader.BaseStream.Seek(index.Lookup, SeekOrigin.Begin);
            reader = new BinaryReader(reader.BaseStream, reader.CurrentEncoding, leaveOpen: true);
            for (var i = 0; i < index.Length / 7; i++)
            {
                AddTileInternal(new StaticTile(reader, this, x, y));
            }
        }

        Changed = false;
    }

    private List<StaticTile>?[,] _tiles;

    public int TotalTilesCount { get; private set; }

    public int TotalSize => TotalTilesCount * StaticTile.Size;

    public IEnumerable<StaticTile> AllTiles()
    {
        foreach (var staticTiles in _tiles)
        {
            if (staticTiles == null)
                continue;

            foreach (var staticTile in staticTiles)
            {
                yield return staticTile;
            }
        }
    }

    public StaticTile? Find(StaticInfo staticInfo)
    {
        return EnsureTiles(staticInfo.X, staticInfo.Y).FirstOrDefault(s => s.Match(staticInfo));
    }

    public ReadOnlyCollection<StaticTile> GetTiles(ushort x, ushort y) =>
        EnsureTiles(x, y).AsReadOnly();

    public void AddTile(StaticTile tile)
    {
        AddTileInternal(tile);
        Landscape.OnStaticTileAdded(tile);
    }

    internal void AddTileInternal(StaticTile tile)
    {
        EnsureTiles(tile.LocalX, tile.LocalY).Add(tile);
        TotalTilesCount++;
        tile.Block = this;
        Changed = true;
    }

    public bool RemoveTile(StaticTile tile)
    {
        var result = RemoveTileInternal(tile);
        if (result)
            Landscape.OnStaticTileRemoved(tile);
        return result;
    }

    internal bool RemoveTileInternal(StaticTile tile)
    {
        var removed = EnsureTiles(tile.LocalX, tile.LocalY).Remove(tile);
        if (removed)
        {
            tile.Block = null;
            TotalTilesCount--;
        }
        Changed = true;
        return removed;
    }

    public void SortTiles(ref TileDataStatic[] tiledata)
    {
        foreach (var staticTiles in _tiles)
        {
            if (staticTiles == null)
                continue;
            foreach (var tile in staticTiles)
            {
                if (tile.Id < tiledata.Length)
                    tile.UpdatePriority(ref tiledata[tile.Id]);
                else
                {
                    Landscape.LogError($"StaticTile with invalid Id: {tile.Id}@{tile.X},{tile.Y},{tile.Z}");
                }
            }
            staticTiles.Sort((tile1, tile2) => tile1.PriorityZ.CompareTo(tile2.PriorityZ));
            var i = staticTiles.Count;
            foreach (var tile in staticTiles)
            {
                tile.CellIndex = i--;
            }
        }
    }

    public void OnChanged()
    {
        Changed = true;
    }

    public void Write(BinaryWriter writer)
    {
        foreach (var staticTiles in _tiles)
        {
            if (staticTiles == null)
                continue;
            foreach (var staticTile in staticTiles)
            {
                staticTile.Write(writer);
            }
        }
    }

    private List<StaticTile> EnsureTiles(ushort x, ushort y)
    {
        var result = _tiles[x & 0x7, y & 0x7] ??= new List<StaticTile>();
        return result;
    }
}