using ClassicUO.Assets;

namespace CentrED.Blueprints;

public class BlueprintManager
{
    #region Dont look at this code

    private readonly Dictionary<uint, string> _nameDict = new()
    {
        { 0, "Small Boat [north]" },
        { 1, "Small Boat [east]" },
        { 2, "Small Boat [south]" },
        { 3, "Small Boat [west]" },
        { 4, "Small Dragon Boat [north]" },
        { 5, "Small Dragon Boat [east]" },
        { 6, "Small Dragon Boat [south]" },
        { 7, "Small Dragon Boat [west]" },
        { 8, "Medium Boat [north]" },
        { 9, "Medium Boat [east]" },
        { 10, "Medium Boat [south]" },
        { 11, "Medium Boat [west]" },
        { 12, "Medium Dragon Boat [north]" },
        { 13, "Medium Dragon [east]" },
        { 14, "Medium Dragon [south]" },
        { 15, "Medium Dragon [west]" },
        { 16, "Large Boat [north]" },
        { 17, "Large Boat [east]" },
        { 18, "Large Boat [south]" },
        { 19, "Large Boat [west]" },
        { 20, "Large Dragon Boat [north]" },
        { 21, "Large Dragon Boat [east]" },
        { 22, "Large Dragon Boat [south]" },
        { 23, "Large Dragon Boat [west]" },
        { 24, "Orcish Galleon [north]" },
        { 25, "Orcish Galleon [east]" },
        { 26, "Orcish Galleon [south]" },
        { 27, "Orcish Galleon [west]" },
        { 28, "Orcish Galleon [damaged][north]" },
        { 29, "Orcish Galleon [damaged][east]" },
        { 30, "Orcish Galleon [damaged][south]" },
        { 31, "Orcish Galleon [damaged][west]" },
        { 32, "Orcish Galleon [destroyed][north]" },
        { 33, "Orcish Galleon [destroyed][east]" },
        { 34, "Orcish Galleon [destroyed][south]" },
        { 35, "Orcish Galleon [destroyed][west]" },
        { 36, "Gargish Galleon [north]" },
        { 37, "Gargish Galleon [east]" },
        { 38, "Gargish Galleon [south]" },
        { 39, "Gargish Galleon [west]" },
        { 40, "Gargish Galleon [damaged][north]" },
        { 41, "Gargish Galleon [damaged][east]" },
        { 42, "Gargish Galleon [damaged][south]" },
        { 43, "Gargish Galleon [damaged][west]" },
        { 44, "Gargish Galleon [destroyed][north]" },
        { 45, "Gargish Galleon [destroyed][east]" },
        { 46, "Gargish Galleon [destroyed][south]" },
        { 47, "Gargish Galleon [destroyed][west]" },
        { 48, "Tokuno Galleon [north]" },
        { 49, "Tokuno Galleon [east]" },
        { 50, "Tokuno Galleon [south]" },
        { 51, "Tokuno Galleon [west]" },
        { 52, "Tokuno Galleon [damaged][north]" },
        { 53, "Tokuno Galleon [damaged][east]" },
        { 54, "Tokuno Galleon [damaged][south]" },
        { 55, "Tokuno Galleon [damaged][west]" },
        { 56, "Tokuno Galleon [destroyed][north]" },
        { 57, "Tokuno Galleon [destroyed][east]" },
        { 58, "Tokuno Galleon [destroyed][south]" },
        { 59, "Tokuno Galleon [destroyed][west]" },
        { 60, "Rowbaot [north]" },
        { 61, "Rowboat [east]" },
        { 62, "Rowboat [south]" },
        { 63, "Rowboat [west]" },
        { 64, "British Galleon [north]" },
        { 65, "British Galleon [east]" },
        { 66, "British Galleon [south]" },
        { 67, "British Galleon [west]" },
        { 68, "British Galleon [damaged][north]" },
        { 69, "British Galleon [damaged][east]" },
        { 70, "British Galleon [damaged][south]" },
        { 71, "British Galleon [damaged][west]" },
        { 100, "Small Stone and Plaster House" },
        { 101, "Small Stone and Plaster House" },
        { 102, "Small Fieldstone House" },
        { 103, "Small Fieldstone House" },
        { 104, "Small Brick House" },
        { 105, "Small Brick House" },
        { 106, "Small Wood House" },
        { 107, "Small Wood House" },
        { 108, "Small Wood and Plaster House" },
        { 109, "Small Wood and Plaster House" },
        { 110, "Small Thatched Roof Cottage" },
        { 111, "Small Thatched Roof Cottage" },
        { 112, "Tent [blue]" },
        { 113, "Tent [blue]" },
        { 114, "Tent [green]" },
        { 115, "Tent [green]" },
        { 116, "Large Brick House" },
        { 117, "Large Brick House" },
        { 118, "Two Story Wood and Plaster House" },
        { 119, "Two Story Wood and Plaster House" },
        { 120, "Two Story Stone and Plaster House" },
        { 121, "Two Story Stone and Plaster House" },
        { 122, "Large Tower" },
        { 123, "Large Tower" },
        { 124, "Stone Keep" },
        { 125, "Stone Keep" },
        { 126, "Castle" },
        { 127, "Castle" },
        { 135, "Large Patio House" },
        { 140, "Large Patio House" },
        { 141, "Large Patio House" },
        { 150, "Large Marble Patio House" },
        { 152, "Small Tower" },
        { 154, "Log Cabin" },
        { 156, "Sandstone Patio House" },
        { 158, "Two-Story Villa" },
        { 160, "Small Stone Workshop" },
        { 162, "Small Marble Workshop" },
        { 201, "Mine's Elevator" },
        { 202, "Lifting Bridge" },
        { 500, "Wandering Healer Camp" },
        { 501, "Wandering Mage Camp" },
        { 502, "Wandering Bank Camp" },
        { 1000, "Treasure Pile" },
        { 1001, "Treasure Pile" },
        { 1002, "Treasure Pile" },
        { 1003, "Treasure Pile" },
        { 2000, "Wooden Supports" },
        { 5243, "Foundation [18x18]" },
        { 7500, "New Player Quest Cam" },
    };

    #endregion

    public class BlueprintTreeEntry
    {
        public string Path { get; private set; }
        public bool Loaded { get; private set; }
        public List<BlueprintTreeEntry> Children { get; private set; }
        public string Name { get; private set; }
        
        private List<BlueprintTile> _Tiles = [];

        public BlueprintTreeEntry(string path, bool loaded, List<BlueprintTreeEntry> children)
        {
            Path = path;
            Loaded = loaded;
            Children = children;
            Name =  Path.Split('/').Last();
        }
        
        public List<BlueprintTile> Tiles
        {
            get
            {
                if (!Loaded)
                    Load();
                return _Tiles;
            }
            internal set => _Tiles = Center(value);
        }

        public void Load()
        {
            if (Loaded)
                return;
            Console.WriteLine($"Loading {Path}");

            if (UOABinaryReader.Read(Path, out var uoaDesigns))
            {
                if (uoaDesigns.Count == 1)
                {
                    Tiles = uoaDesigns.Values.First();
                }
                else
                {
                    foreach (var design in uoaDesigns)
                    {
                        var path = $"{Path}/{design.Key}";
                        var entry = new BlueprintTreeEntry(path, true, []);
                        entry.Tiles = design.Value;
                        Children.Add(entry);
                    }
                }
            }
            else if (MultiTextReader.Read(Path, out var multiTextTiles))
            {
                Tiles = multiTextTiles;
            }
            else
            {
                Name += "(INVALID)"; //Didn't match any reader
            }
            Loaded = true;
        }
        
        private List<BlueprintTile> Center(List<BlueprintTile> input)
        {
            var minX = Math.Min((short)0, input.Min(t => t.X));
            var minY = Math.Min((short)0, input.Min(t => t.Y));
            var maxX = Math.Max((short)0, input.Max(t => t.X));
            var maxY = Math.Max((short)0, input.Max(t => t.Y));

            if (maxX + minX <= 1 && maxY + minY <= 1)
                return input; //We are centered

            var deltaX = (maxX - minX) / 2;
            var deltaY = (maxY - minY) / 2;

            return input.Select
            (t => t with
                {
                    X = (short)(t.X - deltaX),
                    Y = (short)(t.Y - deltaY)
                }
            ).ToList();
        }
    }


    public BlueprintTreeEntry Root = new("Root", true, []);

    public void Load(MultiLoader loader)
    {
        LoadMultis(loader);
        //LoadTilesEntry()
        LoadBlueprints();
    }

    private void LoadMultis(MultiLoader loader)
    {
        var multisEntry = new BlueprintTreeEntry("multi.mul", true, []);
        for (uint i = 0; i < MultiLoader.MAX_MULTI_DATA_INDEX_COUNT; i++)
        {
            var info = loader.GetMultis(i);
            if (info != null && info.Count > 0)
            {
                if (info.All(x => x.ID == 0))
                    continue;

                var path = $"{multisEntry.Path}/0x{i:X4}:{_nameDict.GetValueOrDefault(i, "Unknown")}";
                var entry = new BlueprintTreeEntry(path, true, []);
                entry.Tiles = info.Select(tile => new BlueprintTile(tile)).ToList();
                multisEntry.Children.Add(entry);
            }
        }
        Root.Children.Add(multisEntry);
    }

    private void LoadBlueprints()
    {
        if (!Directory.Exists("Blueprints"))
            Directory.CreateDirectory("Blueprints");

        var blueprints = LoadBlueprintDirectory("Blueprints");
        Root.Children.AddRange(blueprints.Children);
    }

    private BlueprintTreeEntry LoadBlueprintDirectory(string path)
    {
        var result = new BlueprintTreeEntry(path, true, []);
        var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        foreach (var dir in dirs)
        {
            result.Children.Add(LoadBlueprintDirectory(dir));
        }
        var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            result.Children.Add(new BlueprintTreeEntry(file, false, []));
        }
        return result;
    }
}