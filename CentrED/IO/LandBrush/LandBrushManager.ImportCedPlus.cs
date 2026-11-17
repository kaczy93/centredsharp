using System.Globalization;
using System.Xml.Serialization;
using CentrED.IO.Models;
using CentrED.IO.Models.Centredplus;
using static CentrED.IO.Models.Direction;

namespace CentrED.IO;

public partial class LandBrushManager
{
    private static XmlSerializer _xmlSerializer = new(typeof(TilesBrush));

    public bool ImportTilesBrush(string path, out string status)
    {
        try
        {
            using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var tilesBrush = (TilesBrush)_xmlSerializer.Deserialize(reader)!;
            var sourceBrushes = tilesBrush.Brush ?? [];
            var namesById = sourceBrushes.ToDictionary(b => b.Id, b => b.Name);
            var imported = new Dictionary<string, (List<ushort> Tiles, Dictionary<string, List<LandBrushTransition>> To)>();
            foreach (var brush in sourceBrushes)
            {
                var tiles = new List<ushort>();
                foreach (var land in brush.Land ?? [])
                {
                    if (TryParseHex(land.ID, out var newId))
                        tiles.Add(newId);
                    else
                        Console.WriteLine($"Unable to parse land ID {land.ID} in brush {brush.Id}");
                }
                imported.Add(brush.Name, (tiles, new()));
            }
            foreach (var brush in sourceBrushes)
            {
                var to = imported[brush.Name].To;
                foreach (var edge in brush.Edge ?? [])
                {
                    var transitions = new List<LandBrushTransition>();
                    foreach (var edgeLand in edge.Land ?? [])
                    {
                        if (TryParseHex(edgeLand.ID, out var newId))
                            transitions.Add(new LandBrushTransition { TileID = newId, Direction = ConvertType(edgeLand.Type) });
                        else
                            Console.WriteLine($"Unable to parse edgeland ID {edgeLand.ID} in brush {brush.Id}");
                    }
                    to.Add(namesById[edge.To], transitions);
                }
            }

            Clear();
            foreach (var (name, (tiles, to)) in imported)
            {
                var newBrush = AddNew(name);
                newBrush.Tiles.Value = tiles;
                newBrush.To.Value = to;
            }
            InitLandBrushes();
            ActiveName = _brushes.Keys.FirstOrDefault("");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            status = "Error: " + e.Message;
            return false;
        }
        status = "Import Successful";
        return true;
    }
    
    private bool TryParseHex(string value, out ushort result)
    {
        //Substring removes 0x from the value
        result = 0;
        return value is { Length: > 2 } &&
               ushort.TryParse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private Direction ConvertType(string oldType)
    {
        switch (oldType)
        {
            case "DR": return Up;
            case "DL": return Right;
            case "UL": return Down;
            case "UR": return Left;
            case "LL": return Down | East | Right;
            case "UU": return Left | South | Down;
            //File mentions type FF but it's never used
            // "FF" => 
            default:
                Console.WriteLine("Unknown type " + oldType);
                return 0;
        }
    }
}