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
    private void LoadTransitions(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var data = JsonSerializer.Deserialize<Dictionary<string, Tile[]>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });
            if (data == null)
                return;
            transitionTiles.Clear();
            foreach (var kv in data)
                transitionTiles[kv.Key] = kv.Value;
            transitionsPath = path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load transitions: {e.Message}");
        }
    }
}
