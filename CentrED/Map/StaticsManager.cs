using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace CentrED.Map;

public class StaticsManager
{
    private static readonly ReadOnlyCollection<StaticObject> EMPTY = [];
    
    private ushort _Width;
    private ushort _Height;

    public int Count { get; private set; }

    private List<StaticObject>?[] _tiles;
    private Dictionary<int, StaticObject> _idDictionary = new();
    
    private List<StaticObject> _animatedTiles = [];
    public IReadOnlyList<StaticObject> AnimatedTiles => _animatedTiles.AsReadOnly();
    
    private Dictionary<StaticObject, LightObject> _lightTiles = new();
    public IReadOnlyDictionary<StaticObject, LightObject>  LightTiles => _lightTiles.AsReadOnly();
    
    private Dictionary<TileObject, List<StaticObject>>  _ghostTiles = new();
    public IEnumerable<StaticObject> GhostTiles => _ghostTiles.Values.SelectMany(x => x);

    public void Initialize(ushort width, ushort height)
    {
        _Width = width;
        _Height = height;
        Clear();
    }

    public void Clear()
    {
        Count = 0;
        _tiles = new List<StaticObject>[_Width * _Height];
        _idDictionary.Clear();
        _animatedTiles.Clear();
        _lightTiles.Clear();
        _ghostTiles.Clear();
    }

    public void UpdateAll()
    {
        foreach (var so in _idDictionary.Values)
        {
            so.Update();
        }
    }

    public StaticObject? Get(int id)
    {
        _idDictionary.TryGetValue(id, out var result);
        return result;
    }
    
    public ReadOnlyCollection<StaticObject> Get(int x, int y)
    {
        return Get((ushort)x, (ushort)y);
    }

    public ReadOnlyCollection<StaticObject> Get(ushort x, ushort y)
    {
        if (x > _Width || y > _Height)
            return EMPTY;
        var list = _tiles[Index(x, y)];
        return list?.AsReadOnly() ?? EMPTY;
    }
    
    public StaticObject? Get(StaticTile staticTile)
    {
        var list = _tiles[Index(staticTile)];
        if (list == null || list.Count == 0)
            return null;
        return list.FirstOrDefault(so => so.StaticTile.Equals(staticTile));
    }
    
    public void Add(StaticTile staticTile)
    {
        var so = new StaticObject(staticTile);
        var index = Index(staticTile);
        var list = _tiles[index];
        if (list == null)
        {
            list = [];
            _tiles[index] = list;
        }
        list.Add(so);
        list.Sort();
        _idDictionary.Add(so.ObjectId, so);
        Count++;
        if (so.IsAnimated)
        {
            _animatedTiles.Add(so);
        }
        if (so.IsLight)
        {
            _lightTiles.Add(so, new LightObject(so));
        }
    }
    
    public void Remove(StaticTile staticTile)
    {
        var list = _tiles[Index(staticTile)];
        if (list == null || list.Count == 0)
            return;
        var found = list.Find(so => so.StaticTile.Equals(staticTile));
        if (found != null)
        {
            list.Remove(found);
            list.Sort();
            _idDictionary.Remove(found.ObjectId);
            if (found.IsAnimated)
            {
                _animatedTiles.Remove(found);
            }
            if (found.IsLight)
            {
                _lightTiles.Remove(found);
            }
        }
        Count--;
    }

    public void Remove(ushort x, ushort y)
    {
        var index = Index(x, y);
        var so = _tiles[index];
        if (so != null)
        {
            _tiles[index] = null;
            Count -= so.Count;
            foreach (var staticObject in so)
            {
                _idDictionary.Remove(staticObject.ObjectId);
                if (staticObject.IsAnimated)
                {
                    _animatedTiles.Remove(staticObject);
                }
                if (staticObject.IsLight)
                {
                    _lightTiles.Remove(staticObject);
                }
            }
        }
    }
    
    public void Move(StaticTile staticTile, ushort newX, ushort newY)
    {
        var list = _tiles[Index(staticTile)];
        if (list == null || list.Count == 0)
            return;
        var found = list.Find(so => so.StaticTile.Equals(staticTile));
        if (found != null)
        {
            list.Remove(found);
            found.UpdatePos(newX, newY, staticTile.Z);
            var newIndex = Index(newX, newY);
            var newList = _tiles[newIndex];
            if (newList == null)
            {
                newList = new();
                _tiles[newIndex] = newList;
            }
            newList.Add(found);
            newList.Sort();
        }
    }

    public void Elevate(StaticTile tile, sbyte newZ)
    {
        Get(tile)?.UpdatePos(tile.X, tile.Y, newZ);
        _tiles[Index(tile)]?.Sort();
    }

    public List<StaticObject> GetGhosts(TileObject parent)
    {
        return _ghostTiles.TryGetValue(parent, out var ghosts) ? ghosts : [];
    }
    
    public bool TryGetGhost(TileObject parent, [MaybeNullWhen(false)] out StaticObject result)
    {
        if (_ghostTiles.TryGetValue(parent, out var ghosts))
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
        _ghostTiles[parent] = [ghost];
    }

    public void AddGhosts(TileObject parent, IEnumerable<StaticObject> ghosts)
    {
        _ghostTiles[parent] = ghosts.ToList();
    }

    public void ClearGhost(TileObject parent)
    {
        _ghostTiles.Remove(parent);
    }
    
    private int Index(StaticTile tile) => tile.X * _Height + tile.Y;
    private int Index(ushort x, ushort y) => x * _Height + y;
}