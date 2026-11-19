using CentrED.IO.Models;
using static CentrED.CentrED;

namespace CentrED.IO;

public partial class LandBrushManager
{
    private readonly ConfigValue<Dictionary<string, LandBrush>> _brushes =
        App.Profile.Active.Init("land_brushes", new Dictionary<string, LandBrush>(), ConfigConverters.Json<Dictionary<string, LandBrush>>());
    public IReadOnlyDictionary<string, LandBrush> Brushes => _brushes.Value;

    private readonly Dictionary<ushort, List<(string, string)>> _tileToLandBrushNames = new();

    public IReadOnlyDictionary<ushort, List<(string, string)>> TileToLandBrushNames => _tileToLandBrushNames;

    public string ActiveName = "";
    public LandBrush? Active => _brushes.Value.GetValueOrDefault(ActiveName);

    public LandBrushManager()
    {
        App.Client.Connected += InitLandBrushes;
        App.Profile.Changed += OnProfileChanged;
        OnProfileChanged();
    }

    private void OnProfileChanged()
    {
        ActiveName = _brushes.Value.Keys.FirstOrDefault("");
        InitLandBrushes();
    }

    public LandBrush AddNew(string name)
    {
        var brush = new LandBrush();
        _brushes.Value.Add(name, brush);
        return brush;
    }

    public void Remove(string name)
    {
        var brushes = _brushes.Value;
        if (!brushes.Remove(name, out var brush))
            return;
        //Remove full tiles
        brush.Tiles.ForEach(t => RemoveLandBrushEntry(t, name, name));
        //Remove all entries that have removed brush as from-transition
        foreach (var (toName, lbts) in brush.To)
        {
            foreach (var lbt in lbts)
            {
                RemoveLandBrushEntry(lbt.TileID, name, toName);
            }
        }
        //Remove all entries that have removed brush as to-transition
        foreach (var (fromName, landBrush) in brushes)
        {
            if (landBrush.To.Remove(name, out var removed))
            {
                foreach (var transition in removed)
                {
                    RemoveLandBrushEntry(transition.TileID, fromName, name);
                }
            }
        }
        if (ActiveName == name)
        {
            ActiveName = brushes.Keys.FirstOrDefault("");
        }
    }

    public void Clear()
    {
        _brushes.Value.Clear();
        _tileToLandBrushNames.Clear();
        ActiveName = "";
    }

    public void AddLandBrushEntry(ushort tileId, string from, string to)
    {
        if (!_tileToLandBrushNames.ContainsKey(tileId))
        {
            _tileToLandBrushNames.Add(tileId, new List<(string, string)>());
        }
        _tileToLandBrushNames[tileId].Add((from, to));
    }

    public void RemoveLandBrushEntry(ushort tileId, string from, string to)
    {
        if (_tileToLandBrushNames.ContainsKey(tileId))
        {
            _tileToLandBrushNames[tileId].Remove((from, to));
            if (_tileToLandBrushNames[tileId].Count <= 0)
            {
                _tileToLandBrushNames.Remove(tileId);
            }
        }
    }
    
    public void InitLandBrushes()
    {
        _tileToLandBrushNames.Clear();
        foreach (var (name, brush) in _brushes.Value)
        {
            var fullTiles = brush.Tiles;
            foreach (var fullTile in fullTiles)
            {
                AddLandBrushEntry(fullTile, name, name);
            }
            var transitions = brush.To;
            foreach (var valuePair in transitions)
            {
                var toName = valuePair.Key;
                var tiles = valuePair.Value;
                foreach (var tile in tiles)
                {
                    AddLandBrushEntry(tile.TileID, name, toName);
                }
            }
        }
    }
}
