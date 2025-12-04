using System.Xml.Linq;

namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Partial class containing TilesBrush XML parsing and transition logic.
/// </summary>
public partial class ImportColoredHeightmap
{
    /// <summary>
    /// Data structure for a TilesBrush definition.
    /// </summary>
    private class TilesBrushData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public List<(ushort TileId, float Chance)> LandTiles { get; set; } = new();
        public Dictionary<string, TilesBrushEdge> Edges { get; set; } = new();
    }

    /// <summary>
    /// Edge transition data for TilesBrush.
    /// </summary>
    private class TilesBrushEdge
    {
        public string TargetBrushId { get; set; } = "";
        public List<ushort> UL { get; set; } = new();  // Upper-left corner
        public List<ushort> UR { get; set; } = new();  // Upper-right corner
        public List<ushort> DL { get; set; } = new();  // Down-left corner
        public List<ushort> DR { get; set; } = new();  // Down-right corner
        public List<ushort> UU { get; set; } = new();  // Upper edge (horizontal)
        public List<ushort> LL { get; set; } = new();  // Left edge (vertical)
    }

    /// <summary>
    /// Load and parse TilesBrush.xml file.
    /// </summary>
    private bool LoadTilesBrush(string filePath)
    {
        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Root;
            if (root == null || root.Name != "TilesBrush")
            {
                Console.WriteLine("Invalid TilesBrush.xml: Root element must be 'TilesBrush'");
                return false;
            }

            _tilesBrushes = new Dictionary<string, TilesBrushData>();

            foreach (var brushElement in root.Elements("Brush"))
            {
                var brush = new TilesBrushData
                {
                    Id = brushElement.Attribute("Id")?.Value ?? "",
                    Name = brushElement.Attribute("Name")?.Value ?? ""
                };

                // Parse land tiles
                foreach (var landElement in brushElement.Elements("Land").Where(e => e.Attribute("Type") == null))
                {
                    var idStr = landElement.Attribute("ID")?.Value;
                    if (idStr != null)
                    {
                        var tileId = ParseHexOrDecimal(idStr);
                        var chanceStr = landElement.Attribute("Chance")?.Value;
                        float chance = 1.0f;
                        if (chanceStr != null)
                        {
                            chanceStr = chanceStr.Replace(',', '.');
                            float.TryParse(chanceStr, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out chance);
                        }
                        brush.LandTiles.Add((tileId, chance));
                    }
                }

                // Parse edge definitions
                foreach (var edgeElement in brushElement.Elements("Edge"))
                {
                    var targetId = edgeElement.Attribute("To")?.Value ?? "";
                    var edge = new TilesBrushEdge { TargetBrushId = targetId };

                    foreach (var landElement in edgeElement.Elements("Land"))
                    {
                        var typeStr = landElement.Attribute("Type")?.Value;
                        var idStr = landElement.Attribute("ID")?.Value;
                        if (typeStr != null && idStr != null)
                        {
                            var tileId = ParseHexOrDecimal(idStr);
                            switch (typeStr)
                            {
                                case "UL": edge.UL.Add(tileId); break;
                                case "UR": edge.UR.Add(tileId); break;
                                case "DL": edge.DL.Add(tileId); break;
                                case "DR": edge.DR.Add(tileId); break;
                                case "UU": edge.UU.Add(tileId); break;
                                case "LL": edge.LL.Add(tileId); break;
                            }
                        }
                    }

                    brush.Edges[targetId] = edge;
                }

                _tilesBrushes[brush.Id] = brush;
                Console.WriteLine($"Loaded brush: {brush.Id} ({brush.Name}) with {brush.LandTiles.Count} tiles and {brush.Edges.Count} edges");
            }

            Console.WriteLine($"TilesBrush loaded: {_tilesBrushes.Count} brushes");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading TilesBrush.xml: {e.Message}");
            _tilesBrushes = null;
            return false;
        }
    }

    private static ushort ParseHexOrDecimal(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToUInt16(value[2..], 16);
        }
        return ushort.Parse(value);
    }

    /// <summary>
    /// Map biome enum to TilesBrush ID.
    /// </summary>
    private string GetBrushIdForBiome(Biome biome)
    {
        return biome switch
        {
            Biome.Grass => "0203",
            Biome.Dirt => "0002",
            Biome.Sand => "0217",
            Biome.Forest => "0204",
            Biome.Jungle => "0202",
            Biome.Snow => "0210",
            Biome.Rock => "0209",  // Mountain
            Biome.Swamp => "0240",  // Swamp Water
            Biome.Cobblestone => "0198",
            Biome.Brick => "0199",
            Biome.Cave => "0215",
            _ => "0203"  // Default to grass
        };
    }

    /// <summary>
    /// Get transition tile using TilesBrush data.
    /// </summary>
    private ushort? GetTilesBrushTransition(int px, int py, Biome centerBiome)
    {
        if (_tilesBrushes == null || _biomeCache == null)
            return null;

        var brushId = GetBrushIdForBiome(centerBiome);
        if (!_tilesBrushes.TryGetValue(brushId, out var brush))
            return null;

        int width = _biomeCache.GetLength(0);
        int height = _biomeCache.GetLength(1);

        Biome GetBiomeAt(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return centerBiome;
            return _biomeCache[x, y];
        }

        // Get neighboring biomes - standard UO coordinate system (matches LandBrush Direction.Offset)
        // North = (0, -1), South = (0, +1), East = (+1, 0), West = (-1, 0)
        var n = GetBiomeAt(px, py - 1);      // North
        var s = GetBiomeAt(px, py + 1);      // South
        var e = GetBiomeAt(px + 1, py);      // East
        var w = GetBiomeAt(px - 1, py);      // West

        // Check for edges with different biomes
        var differentNeighbors = new HashSet<Biome>();
        if (n != centerBiome) differentNeighbors.Add(n);
        if (s != centerBiome) differentNeighbors.Add(s);
        if (e != centerBiome) differentNeighbors.Add(e);
        if (w != centerBiome) differentNeighbors.Add(w);

        if (differentNeighbors.Count == 0)
            return null;

        // Find an edge that matches one of our neighbors
        foreach (var neighborBiome in differentNeighbors)
        {
            var neighborBrushId = GetBrushIdForBiome(neighborBiome);
            if (!brush.Edges.TryGetValue(neighborBrushId, out var edge))
                continue;

            bool hasN = n == neighborBiome;
            bool hasS = s == neighborBiome;
            bool hasE = e == neighborBiome;
            bool hasW = w == neighborBiome;

            List<ushort>? tiles = null;

            // Corner cases (two adjacent cardinal neighbors)
            if (hasN && hasW && !hasS && !hasE && edge.UL.Count > 0)
                tiles = edge.UL;
            else if (hasN && hasE && !hasS && !hasW && edge.UR.Count > 0)
                tiles = edge.UR;
            else if (hasS && hasW && !hasN && !hasE && edge.DL.Count > 0)
                tiles = edge.DL;
            else if (hasS && hasE && !hasN && !hasW && edge.DR.Count > 0)
                tiles = edge.DR;
            // Edge cases (one cardinal neighbor)
            else if (hasN && !hasS && !hasE && !hasW && edge.UU.Count > 0)
                tiles = edge.UU;
            else if (hasS && !hasN && !hasE && !hasW && edge.UU.Count > 0)
                tiles = edge.UU;
            else if (hasW && !hasN && !hasS && !hasE && edge.LL.Count > 0)
                tiles = edge.LL;
            else if (hasE && !hasN && !hasS && !hasW && edge.LL.Count > 0)
                tiles = edge.LL;

            if (tiles != null && tiles.Count > 0)
            {
                return tiles[_random.Next(tiles.Count)];
            }
        }

        return null;
    }
}
