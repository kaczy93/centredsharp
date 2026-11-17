using System.Globalization;
using System.Xml.Serialization;
using CentrED.IO.Models;
using CentrED.IO.Models.Centredplus;
using static CentrED.CentrED;
using static CentrED.IO.Models.Direction;

namespace CentrED.IO;

public partial class LandBrushManager
{
    private static XmlSerializer _xmlSerializer = new(typeof(TilesBrush));

    public string ImportTilesBrush(string path)
    {
        try
        {
            using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var tilesBrush = (TilesBrush)_xmlSerializer.Deserialize(reader)!;
            Clear();
            var newBrushes = new Dictionary<string, LandBrush>();
            foreach (var brush in tilesBrush.Brush)
            {
                var newBrush = AddNew(brush.Name);
                newBrushes.Add(brush.Id, newBrush);
                foreach (var land in brush.Land)
                {
                    if (TryParseHex(land.ID, out var newId))
                    {
                        newBrush.Tiles.Value.Add(newId);
                    }
                    else
                    {
                        Console.WriteLine($"Unable to parse land ID {land.ID} in brush {brush.Id}");
                    }
                }
            }
            foreach (var brush in tilesBrush.Brush)
            {
                var newBrush = newBrushes[brush.Id];
                foreach (var edge in brush.Edge)
                {
                    var to = tilesBrush.Brush.Find(b => b.Id == edge.To);
                    var newList = new List<LandBrushTransition>();
                    foreach (var edgeLand in edge.Land)
                    {
                        if (TryParseHex(edgeLand.ID, out var newId))
                        {
                            var newType = ConvertType(edgeLand.Type);
                            newList.Add
                            (
                                new LandBrushTransition
                                {
                                    TileID = newId,
                                    Direction = newType
                                }
                            );
                        }
                        else
                        {
                            Console.WriteLine($"Unable to parse edgeland ID {edgeLand.ID} in brush {brush.Id}");
                        }
                    }
                    newBrush.To.Value.Add(to.Name, newList);
                }
            }
            InitLandBrushes();
            ActiveName = _brushes.Keys.FirstOrDefault("");
        }
        catch (Exception e)
        {
            return "Error: " + e.Message;
            Console.WriteLine(e);
        }
        return "Import Successful";
    }
    
    private bool TryParseHex(string value, out ushort result)
    {
        //Substring removes 0x from the value
        return ushort.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
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