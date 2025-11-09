namespace CentrED.Blueprints.Writers;

public class CsvWriter
{
    public static void Write(IEnumerable<BlueprintTile> tiles, StreamWriter writer)
    {
        writer.WriteLine("id,x,y,z,hue,flags");
        foreach (var t in tiles)
        {
            writer.WriteLine($"{t.Id},{t.X},{t.Y},{t.Z},{t.Hue},0");
        }
    }
}