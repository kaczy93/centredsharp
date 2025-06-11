using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void SaveTransitions(string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        // Serialize a simple representation to avoid accidentally picking up
        // data from the tile group structure. Each transition entry is
        // converted to a lightweight array containing only the terrain type
        // and tile id information used by the generator.
        var data = transitionTiles.ToDictionary(
            kv => kv.Key,
            kv => kv.Value
                .Select(t => new { Type = t.Type, Id = t.Id })
                .ToArray());
        File.WriteAllText(path, JsonSerializer.Serialize(data, options));
        transitionsPath = path;
    }
}
