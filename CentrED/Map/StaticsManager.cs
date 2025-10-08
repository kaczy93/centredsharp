using System.Diagnostics.CodeAnalysis;

namespace CentrED.Map;

public class StaticsManager
{
    private ushort _Width;
    private ushort _Height;

    public int Count { get; private set; }

    private List<StaticObject>?[] _Tiles;
    private Dictionary<int, StaticObject> _IdDictionary = new();
    
    private List<StaticObject> _AnimatedTiles = [];
    public IReadOnlyList<StaticObject> AnimatedTiles => _AnimatedTiles.AsReadOnly();
    
    private Dictionary<StaticObject, LightObject> _LightTiles = new();
    public IReadOnlyDictionary<StaticObject, LightObject>  LightTiles => _LightTiles.AsReadOnly();
    
    private Dictionary<TileObject, List<StaticObject>>  _GhostTiles = new();
    public IEnumerable<StaticObject> GhostTiles => _GhostTiles.Values.SelectMany(x => x);

    public void Initialize(ushort width, ushort height)
    {
        _Width = width;
        _Height = height;
        Clear();
    }

    public void Clear()
    {
        Count = 0;
        _Tiles = new List<StaticObject>[_Width * _Height];
        _IdDictionary.Clear();
        _AnimatedTiles.Clear();
        _LightTiles.Clear();
        _GhostTiles.Clear();
    }

    public void UpdateAll()
    {
        foreach (var so in _IdDictionary.Values)
        {
            so.Update();
        }
    }

    public StaticObject? Get(int id)
    {
        _IdDictionary.TryGetValue(id, out var result);
        return result;
    }
    
    public List<StaticObject> Get(int x, int y)
    {
        return Get((ushort)x, (ushort)y);
    }
    
    public List<StaticObject> Get(ushort x, ushort y)
    {
        var list = _Tiles[Index(x, y)];
        return list ?? [];
    }
    
    public StaticObject? Get(StaticTile staticTile)
    {
        var list = _Tiles[Index(staticTile)];
        if (list == null || list.Count == 0)
            return null;
        return list.FirstOrDefault(so => so.StaticTile.Equals(staticTile));
    }
    
    public void Add(StaticTile staticTile)
    {
        var so = new StaticObject(staticTile);
        var index = Index(staticTile);
        var list = _Tiles[index];
        if (list == null)
        {
            list = [];
            _Tiles[index] = list;
        }
        list.Add(so);
        list.Sort();
        _IdDictionary.Add(so.ObjectId, so);
        Count++;
        if (so.IsAnimated)
        {
            _AnimatedTiles.Add(so);
        }
        if (so.IsLight)
        {
            _LightTiles.Add(so, new LightObject(so));
        }
    }
    
    public void Remove(StaticTile staticTile)
    {
        var list = _Tiles[Index(staticTile)];
        if (list == null || list.Count == 0)
            return;
        var found = list.Find(so => so.StaticTile.Equals(staticTile));
        if (found != null)
        {
            list.Remove(found);
            list.Sort();
            _IdDictionary.Remove(found.ObjectId);
            if (found.IsAnimated)
            {
                _AnimatedTiles.Remove(found);
            }
            if (found.IsLight)
            {
                _LightTiles.Remove(found);
            }
        }
        Count--;
    }

    public void Remove(ushort x, ushort y)
    {
        var index = Index(x, y);
        var so = _Tiles[index];
        if (so != null)
        {
            _Tiles[index] = null;
            Count -= so.Count;
            foreach (var staticObject in so)
            {
                _IdDictionary.Remove(staticObject.ObjectId);
                if (staticObject.IsAnimated)
                {
                    _AnimatedTiles.Remove(staticObject);
                }
                if (staticObject.IsLight)
                {
                    _LightTiles.Remove(staticObject);
                }
            }
        }
    }
    
    public void Move(StaticTile staticTile, ushort newX, ushort newY)
    {
        var list = _Tiles[Index(staticTile)];
        if (list == null || list.Count == 0)
            return;
        var found = list.Find(so => so.StaticTile.Equals(staticTile));
        if (found != null)
        {
            list.Remove(found);
            found.UpdatePos(newX, newY, staticTile.Z);
            var newIndex = Index(newX, newY);
            var newList = _Tiles[newIndex];
            if (newList == null)
            {
                newList = new();
                _Tiles[newIndex] = newList;
            }
            newList.Add(found);
            newList.Sort();
        }
    }

    public void Elevate(StaticTile tile, sbyte newZ)
    {
        Get(tile)?.UpdatePos(tile.X, tile.Y, newZ);
        _Tiles[Index(tile)]?.Sort();
    }

    public List<StaticObject> GetGhosts(TileObject parent)
    {
        return _GhostTiles.TryGetValue(parent, out var ghosts) ? ghosts : [];
    }
    
    public bool TryGetGhost(TileObject parent, [MaybeNullWhen(false)] out StaticObject result)
    {
        if (_GhostTiles.TryGetValue(parent, out var ghosts))
        {
            result = ghosts.FirstOrDefault();
            if(result == null)
                Console.WriteLine("[WARN] Encountered empty list of ghost tiles");
            return result != null;
        }
        result = null;
        return false;
    }

    public void AddGhost(TileObject parent, StaticObject ghost)
    {
        _GhostTiles[parent] = [ghost];
    }

    public void AddGhosts(TileObject parent, IEnumerable<StaticObject> ghosts)
    {
        _GhostTiles[parent] = ghosts.ToList();
    }

    public void ClearGhost(TileObject parent)
    {
        _GhostTiles.Remove(parent);
    }
    
    private int Index(StaticTile tile) => tile.X * _Height + tile.Y;
    private int Index(ushort x, ushort y) => x * _Height + y;
}