namespace CentrED.Blueprints;

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

            if (CsvReader.Read(Path, out var csvTiles))
            {
                Tiles = csvTiles;
            }
            else if (UOABinaryReader.Read(Path, out var uoaDesigns))
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
            else if (MultiPlainTextReader.Read(Path, out var multiPlainTextTiles))
            {
                Tiles = multiPlainTextTiles;
            }
            else if (TilesEntryXmlReader.Read(Path, out var tileEntries))
            {
                foreach (var tileEntry in tileEntries)
                {
                    var path = $"{Path}/{tileEntry.Key}";
                    var entry = new BlueprintTreeEntry(path, true, []);
                    entry.Tiles = tileEntry.Value;
                    Children.Add(entry);
                }
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