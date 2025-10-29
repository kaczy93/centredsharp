namespace CentrED.Blueprints;

//Source: https://github.com/CorexUO/UOArchitect/blob/main/UO%20Architect/IO/UOARBatchDataAdapter.cs
public class UOABinaryReader
{
    public static Dictionary<string, List<BlueprintTile>> Read(string file)
    {
        var result = new Dictionary<string, List<BlueprintTile>>(); //TODO Use array
        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var reader = new BinaryReader(fs))
        {
            var version = reader.ReadInt16();
            var designCount = 1;
            if (version > 2 ) // Version check
            {
                Console.WriteLine($"[UOAB] version {version} is not supported!");
                return result;
            }
            if (version == 2)
                designCount = reader.ReadInt16();

            for (int i = 0; i < designCount; i++)
            {
                var design = new List<BlueprintTile>();
                var name = ReadUOAString(reader);
                var category = ReadUOAString(reader);    // Category
                var subcategory = ReadUOAString(reader); // Subsection

                var height = reader.ReadInt32();
                var width = reader.ReadInt32();
                var userHeight = reader.ReadInt32();
                var userWidth = reader.ReadInt32();

                int count = reader.ReadInt32();
                for (var j = 0; j < count; j++)
                {
                    var id = (ushort)reader.ReadInt16();
                    var x = reader.ReadInt16();
                    var y = reader.ReadInt16();
                    var z = reader.ReadInt16();
                    var isVisible = true;
                    reader.ReadInt16(); // level
                    var hue = (ushort)reader.ReadInt16(); 

                    design.Add(new BlueprintTile(id, x, y, z, hue, isVisible));
                }
                if (result.ContainsKey(name))
                {
                    name = $"name{i}";
                }
                result.Add(name, design);
            }
        }
        return result;
    }
    
    private static string ReadUOAString(BinaryReader bin)
    {
        byte flag = bin.ReadByte();

        return flag == 0 ? null : bin.ReadString();
    }
}