using CentrED.IO.Models;
using static CentrED.CentrED;

namespace CentrED.IO;

public partial class LandBrushManager
{
    public const string CONFIG_PREFIX = "land_brush";

    private readonly Dictionary<string, LandBrush> _brushes = new();
    public IReadOnlyDictionary<string, LandBrush> Brushes => _brushes.AsReadOnly();

    private readonly Dictionary<ushort, List<(string, string)>> _tileToLandBrushNames = new();

    public IReadOnlyDictionary<ushort, List<(string, string)>> TileToLandBrushNames =>
        _tileToLandBrushNames.AsReadOnly();

    public string ActiveName;
    public LandBrush? Active => _brushes.GetValueOrDefault(ActiveName);

    public LandBrushManager()
    {
        RegisterConfigConverters();
        App.Client.Connected += InitLandBrushes;
    }

    public LandBrush AddNew(string name)
    {
        var brush = new LandBrush(UniqueKey(name));
        brush.Name.Value = name;
        _brushes.Add(name, brush);
        return brush;
    }

    public static string ToKey(string name)
    {
        return new string(name.Select(c => char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_').ToArray());
    }

    internal string UniqueKey(string name)
    {
        var baseKey = ToKey(name);
        var usedKeys = App.Profile.Active.SubKeys(CONFIG_PREFIX);
        var key = baseKey;
        for (var i = 2; usedKeys.Contains(key); i++)
        {
            key = $"{baseKey}_{i}";
        }
        return key;
    }
    
    public void Remove(string name)
    {
        if (!_brushes.Remove(name, out var brush))
            return;
        //Remove full tiles
        brush.Tiles.Value.ForEach(t => RemoveLandBrushEntry(t, name, name));
        //Remove all entries that have removed brush as from-transition
        foreach (var (toName, lbts) in brush.To.Value)
        {
            foreach (var lbt in lbts)
            {
                RemoveLandBrushEntry(lbt.TileID, name, toName);
            }
        }
        //Remove all entries that have removed brush as to-transition
        foreach (var landBrush in Brushes.Values)
        {
            if(landBrush.To.Value.Remove(name, out var removed))
            {
                foreach (var transition in removed)
                {
                    RemoveLandBrushEntry(transition.TileID, landBrush.Name.Value, name);
                }
            }
        }
        App.Profile.Active.Remove($"{CONFIG_PREFIX}.{brush.Key}");
        if (ActiveName == name)
        {
            ActiveName = _brushes.Keys.FirstOrDefault("");
        }
    }

    public void Clear()
    {
        foreach (var name in _brushes.Keys.ToArray())
        {
            Remove(name);
        }
    }

    public static void RegisterConfigConverters()
    {
        ConfigConverters.Register
        (
            (string s, out LandBrushTransition value) =>
            {
                value = null;
                var separator = s.IndexOf(':');
                if (separator < 0 || !ConfigConverters.TryParseInteger(s[..separator], out ushort x) ||
                    !ConfigConverters.TryParseInteger(s[(separator + 1)..], out byte y))
                    return false;
                value = new LandBrushTransition { TileID = x, Direction = (Direction)y };
                return true;
            },
            v => $"{v.TileID}:{(byte)v.Direction}"
        );
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
        App.Profile.Active.DropPrefix(CONFIG_PREFIX);
        _brushes.Clear();
        foreach (var key in App.Profile.Active.SubKeys(CONFIG_PREFIX))
        {
            var brush = new LandBrush(key);
            _brushes[brush.Name.Value] = brush;
        }
        ActiveName = _brushes.Keys.FirstOrDefault("");
        _tileToLandBrushNames.Clear();
        foreach (var keyValuePair in _brushes)
        {
            var name = keyValuePair.Key;
            var brush = keyValuePair.Value;
            var fullTiles = brush.Tiles.Value;
            foreach (var fullTile in fullTiles)
            {
                AddLandBrushEntry(fullTile, name, name);
            }
            var transitions = brush.To.Value;
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
