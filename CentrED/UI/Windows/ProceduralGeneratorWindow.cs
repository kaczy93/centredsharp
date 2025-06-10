using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using CentrED.Utils;
using ImGuiNET;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class ProceduralGeneratorWindow : Window
{
    public override string Name => "Procedural Generator";

    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    public ProceduralGeneratorWindow()
    {
        LoadApiKey();
    }

    public override void OnShow()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            LoadApiKey();
    }

    private int x1;
    private int y1;
    private int x2;
    private int y2;

    private int regionType;
    private int seed = 0;
    private float roughness = 1.0f;

    private readonly Dictionary<string, Group> tileGroups = new();
    private readonly Dictionary<string, Group> staticGroups = new();
    private string selectedTileGroup = string.Empty;
    private string selectedStaticGroup = string.Empty;
    private string newTileGroupName = string.Empty;
    private string newStaticGroupName = string.Empty;
    private float waterChance = 0f;

    private string apiKey = string.Empty;
    private const int BlockSize = 256;

    private string gptResponse = string.Empty;

    private const string GroupsFile = "procedural_groups.json";
    private const string ApiKeyFile = "chatgpt_key.json";

    private static readonly string[] RegionNames = Enum.GetNames<Region>();

    private record struct GptTile(int x, int y, int tileId, int height);

    private Task? generationTask;
    private float generationProgress;

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        var minimapWindow = CEDGame.UIManager.GetWindow<MinimapWindow>();
        if (ImGui.Button(minimapWindow.Show ? "Close Minimap" : "Open Minimap"))
        {
            minimapWindow.Show = !minimapWindow.Show;
        }
        ImGui.Separator();
        ImGui.Text("Area");
        ImGui.PushItemWidth(90);
        ImGui.InputInt("X1", ref x1);
        ImGui.SameLine();
        ImGui.InputInt("Y1", ref y1);
        ImGui.SameLine();
        if (ImGui.Button("Current pos##pos1"))
        {
            var pos = CEDGame.MapManager.TilePosition;
            x1 = pos.X;
            y1 = pos.Y;
        }
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos1"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x1 = tile.Tile.X;
                y1 = tile.Tile.Y;
            }
        }
        ImGui.InputInt("X2", ref x2);
        ImGui.SameLine();
        ImGui.InputInt("Y2", ref y2);
        ImGui.SameLine();
        if (ImGui.Button("Current pos##pos2"))
        {
            var pos = CEDGame.MapManager.TilePosition;
            x2 = pos.X;
            y2 = pos.Y;
        }
        ImGui.SameLine();
        if (ImGui.Button("Selected tile##pos2"))
        {
            var tile = CEDGame.UIManager.GetWindow<InfoWindow>().Selected;
            if (tile != null)
            {
                x2 = tile.Tile.X;
                y2 = tile.Tile.Y;
            }
        }
        ImGui.PopItemWidth();
        ImGui.Separator();
        ImGui.Combo("Region", ref regionType, RegionNames, RegionNames.Length);
        ImGui.InputInt("Seed", ref seed);
        ImGui.DragFloat("Roughness", ref roughness, 0.1f, 0.1f, 10f);
        ImGui.DragFloat("Water Chance (%)", ref waterChance, 0.1f, 0f, 100f);

        ImGui.Separator();
        ImGui.Text("Tile Groups");
        DrawGroups(tileGroups, ref selectedTileGroup, ref newTileGroupName, true);
        ImGui.Separator();
        ImGui.Text("Static Groups");
        DrawGroups(staticGroups, ref selectedStaticGroup, ref newStaticGroupName, false);
        ImGui.Separator();

        if (ImGui.Button("Save Groups"))
        {
            SaveGroups();
        }
        ImGui.SameLine();
        if (ImGui.Button("Load Groups"))
        {
            if (TinyFileDialogs.TryOpenFile("Load Groups", Environment.CurrentDirectory,
                    new[] { "*.json" }, "JSON Files", false, out var path))
            {
                LoadGroups(path);
            }
        }
        ImGui.InputText("ChatGPT API Key", ref apiKey, 256, ImGuiInputTextFlags.Password);
        ImGui.SameLine();
        if (ImGui.Button("Save Key"))
        {
            SaveApiKey();
        }
        ImGui.SameLine();
        if (ImGui.Button("Load Key"))
        {
            LoadApiKey();
        }
        ImGui.Separator();
        ImGui.BeginDisabled(generationTask != null && !generationTask.IsCompleted);
        if (ImGui.Button("Generate"))
        {
            Generate();
        }
        ImGui.SameLine();
        if (ImGui.Button("Generate From Groups"))
        {
            GenerateFromGroups();
        }
        ImGui.SameLine();
        if (ImGui.Button("Generate With ChatGPT"))
        {
            GenerateWithChatGPT();
        }
        ImGui.EndDisabled();
        if (generationTask != null)
        {
            if (generationTask.IsCompleted)
            {
                generationTask = null;
                generationProgress = 0f;
            }
            else
            {
                ImGui.ProgressBar(generationProgress, new System.Numerics.Vector2(-1, 0));
            }
        }
        ImGui.Separator();
        ImGui.Text("ChatGPT Response");
        ImGui.InputTextMultiline("##gptresponse", ref gptResponse, 8192, new System.Numerics.Vector2(-1, 100), ImGuiInputTextFlags.ReadOnly);
    }

    private void Generate()
    {
        if (generationTask != null && !generationTask.IsCompleted)
            return;

        generationTask = Task.Run(() =>
        {
            var noise = new Perlin(seed);
            var startX = Math.Min(x1, x2);
            var endX = Math.Max(x1, x2);
            var startY = Math.Min(y1, y2);
            var endY = Math.Max(y1, y2);

            var landGroupsList = tileGroups.Values.Where(g => g.Ids.Count > 0).ToList();
            var staticGroupsList = staticGroups.Values.Where(g => g.Ids.Count > 0).ToList();

            var total = (endX - startX + 1) * (endY - startY + 1);
            int processed = 0;

            for (var bx = startX; bx <= endX; bx += BlockSize)
            {
                var ex = Math.Min(endX, bx + BlockSize - 1);
                for (var by = startY; by <= endY; by += BlockSize)
                {
                    var ey = Math.Min(endY, by + BlockSize - 1);
                    CEDClient.LoadBlocks(new AreaInfo((ushort)bx, (ushort)by, (ushort)ex, (ushort)ey));
                    for (var x = bx; x <= ex; x++)
                    {
                        for (var y = by; y <= ey; y++)
                        {
                            if (!CEDClient.TryGetLandTile(x, y, out var landTile))
                                continue;

                            var n = noise.Fractal(x * 0.1f, y * 0.1f, roughness);
                            var z = (sbyte)Math.Clamp((int)(n * 10), -128, 127);

                            var candidates = landGroupsList
                                .Where(g => z >= g.MinHeight && z <= g.MaxHeight)
                                .ToList();
                            if (candidates.Count == 0)
                                candidates = landGroupsList;
                            if (candidates.Count > 0)
                            {
                                var group = SelectGroup(candidates);
                                var tileId = group.Ids[Random.Shared.Next(group.Ids.Count)];
                                landTile.ReplaceLand(tileId, z);
                            }

                            var staticCandidates = staticGroupsList
                                .Where(g => z >= g.MinHeight && z <= g.MaxHeight)
                                .ToList();
                            if (staticCandidates.Count == 0)
                                staticCandidates = staticGroupsList;
                            if (staticCandidates.Count > 0)
                            {
                                var sGroup = SelectGroup(staticCandidates);
                                if (Random.Shared.NextDouble() < sGroup.Chance / 100f)
                                {
                                    var staticId = sGroup.Ids[Random.Shared.Next(sGroup.Ids.Count)];
                                    CEDClient.Add(new StaticTile(staticId, (ushort)x, (ushort)y, z, 0));
                                }
                            }

                            processed++;
                            generationProgress = processed / (float)total;
                        }
                    }
                }
            }

            generationProgress = 1f;
        });

        static Group SelectGroup(List<Group> groups)
        {
            var total = groups.Sum(g => g.Chance);
            var val = Random.Shared.NextDouble() * total;
            float acc = 0f;
            foreach (var g in groups)
            {
                acc += g.Chance;
                if (val <= acc)
                    return g;
            }
            return groups[0];
        }
    }


    private void GenerateFromGroups()
    {
        var waterGroups = tileGroups.Where(kv => kv.Value.IsWater && kv.Value.Ids.Count > 0).Select(kv => kv.Value).ToList();
        var landGroups = tileGroups.Where(kv => !kv.Value.IsWater && kv.Value.Ids.Count > 0).Select(kv => kv.Value).ToList();
        var staticGroupsList = staticGroups.Values.Where(g => g.Ids.Count > 0).ToList();
        if (waterGroups.Count == 0 && landGroups.Count == 0)
            return;

        var noise = new Perlin(seed);
        var startX = Math.Min(x1, x2);
        var endX = Math.Max(x1, x2);
        var startY = Math.Min(y1, y2);
        var endY = Math.Max(y1, y2);

        CEDClient.LoadBlocks(new AreaInfo((ushort)startX, (ushort)startY, (ushort)endX, (ushort)endY));
        for (var x = startX; x <= endX; x++)
        {
            for (var y = startY; y <= endY; y++)
            {
                if (!CEDClient.TryGetLandTile(x, y, out var landTile))
                    continue;

                var n = noise.Fractal(x * 0.1f, y * 0.1f, roughness);
                var z = (sbyte)Math.Clamp((int)(n * 10), -128, 127);

                bool useWater = Random.Shared.NextDouble() < waterChance / 100f && waterGroups.Count > 0;
                var set = useWater ? waterGroups : landGroups;
                var filtered = set.Where(g => z >= g.MinHeight && z <= g.MaxHeight).ToList();
                if (filtered.Count == 0)
                    filtered = set;
                if (filtered.Count > 0)
                {
                    var grp = SelectGroup(filtered);
                    var tileId = grp.Ids[Random.Shared.Next(grp.Ids.Count)];
                    landTile.ReplaceLand(tileId, z);
                }

                if (staticGroupsList.Count > 0)
                {
                    var sfiltered = staticGroupsList.Where(g => z >= g.MinHeight && z <= g.MaxHeight).ToList();
                    if (sfiltered.Count == 0)
                        sfiltered = staticGroupsList;
                    if (sfiltered.Count > 0)
                    {
                        var sgrp = SelectGroup(sfiltered);
                        if (Random.Shared.NextDouble() < sgrp.Chance / 100f)
                        {
                            var id = sgrp.Ids[Random.Shared.Next(sgrp.Ids.Count)];
                            CEDClient.Add(new StaticTile(id, (ushort)x, (ushort)y, z, 0));
                        }
                    }
                }
            }
        }

        static Group SelectGroup(List<Group> groups)
        {
            var total = groups.Sum(g => g.Chance);
            var val = Random.Shared.NextDouble() * total;
            float acc = 0f;
            foreach (var g in groups)
            {
                acc += g.Chance;
                if (val <= acc)
                    return g;
            }
            return groups[0];
        }
    }

    private void GenerateWithChatGPT()
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return;
        if (!File.Exists(GroupsFile))
            return;

        var promptBase = "Gere um mapa mundi fantasia com esses tiles retornando apenas um array de json COMPLETO, sem nenhum outro texto, com os campos\n{\n\"x\": value\n\"y\": value\n\"tileId\": value\n\"height\": value\n}\n\n OBS: Não retorne nada além do array de json, E O ARRAY DEVE SER COMPLETO. Se o array tiver que ter mais de 1000x1000 tiles, divida em blocos de 1000x1000, mande 1000x1000 tiles por vez, e mande o array completo de cada bloco.";
        var groupsJson = File.ReadAllText(GroupsFile);
        var client = new ChatGPTClient(apiKey);

        var startX = Math.Min(x1, x2);
        var endX = Math.Max(x1, x2);
        var startY = Math.Min(y1, y2);
        var endY = Math.Max(y1, y2);

        var prompt = $"{promptBase}\nArea x1:{startX}, x2:{endX} y1:{startY}, y2:{endY}\n{groupsJson}";
        gptResponse = client.SendPrompt(prompt);
        gptResponse = FormatGptResponse(gptResponse);
        if (string.IsNullOrWhiteSpace(gptResponse))
            return;
        try
        {
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            var tiles = JsonSerializer.Deserialize<List<GptTile>>(gptResponse, options);
            if (tiles == null)
                return;
            for (var bx = startX; bx <= endX; bx += BlockSize)
            {
                var ex = Math.Min(endX, bx + BlockSize - 1);
                for (var by = startY; by <= endY; by += BlockSize)
                {
                    var ey = Math.Min(endY, by + BlockSize - 1);
                    CEDClient.LoadBlocks(new AreaInfo((ushort)bx, (ushort)by, (ushort)ex, (ushort)ey));
                    foreach (var t in tiles.Where(t => t.x >= bx && t.x <= ex && t.y >= by && t.y <= ey && t.tileId >= 0 && t.tileId <= ushort.MaxValue))
                    {
                        if (CEDClient.TryGetLandTile(t.x, t.y, out var landTile))
                        {
                            landTile.ReplaceLand((ushort)t.tileId, (sbyte)t.height);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to parse GPT response: {e.Message}");
        }

    }

    private static string FormatGptResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;
        var trimmed = response.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var idx = trimmed.IndexOf('\n');
            if (idx >= 0)
            {
                trimmed = trimmed[(idx + 1)..];
                if (trimmed.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    idx = trimmed.IndexOf('\n');
                    if (idx >= 0)
                        trimmed = trimmed[(idx + 1)..];
                }
                var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (end >= 0)
                    trimmed = trimmed[..end];
            }
        }
        var start = trimmed.IndexOf('[');
        var endIdx = trimmed.LastIndexOf(']');
        if (start >= 0 && endIdx >= start)
            return trimmed.Substring(start, endIdx - start + 1);
        return trimmed;
    }

    private (ushort landId, sbyte altitudeOffset, sbyte altitudeRange, ushort staticId, float staticChance) GetSettings(Region region)
    {
        return region switch
        {
            Region.Desert => (0x03, 0, 5, 0x0E89, 0.02f),
            Region.Mountain => (0x21A, 10, 40, 0x0F06, 0.05f),
            Region.Swamp => (0x09A, -2, 4, 0x0D91, 0.03f),
            _ => (0x006, 0, 10, 0x0D46, 0.04f)
        };
    }

    private void DrawGroups(Dictionary<string, Group> groups, ref string selected, ref string newName, bool land)
    {
        if (ImGui.BeginChild($"{(land ? "Land" : "Static")}Groups", new System.Numerics.Vector2(0, 120), ImGuiChildFlags.Borders))
        {
            foreach (var kv in groups.ToArray())
            {
                ImGui.PushID($"{(land ? "l" : "s")}_{kv.Key}");
                bool isSel = selected == kv.Key;
                if (ImGui.Selectable($"{kv.Key} ({kv.Value.Chance:0.#}% )", isSel))
                    selected = kv.Key;
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        groups.Remove(kv.Key);
                        if (selected == kv.Key) selected = string.Empty;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
                ImGui.PopID();
            }
            ImGui.EndChild();
        }
        ImGui.InputText($"##newgroup_{(land ? "l" : "s")}", ref newName, 32);
        ImGui.SameLine();
        if (ImGui.Button($"Add##{(land ? "l" : "s")}"))
        {
            if (!string.IsNullOrWhiteSpace(newName) && !groups.ContainsKey(newName))
            {
                groups.Add(newName, new Group());
                selected = newName;
                newName = string.Empty;
            }
        }
        if (!string.IsNullOrEmpty(selected) && groups.TryGetValue(selected, out var grp))
        {
            ImGui.DragFloat($"Chance (%)##{(land ? "l" : "s")}_{selected}", ref grp.Chance, 0.1f, 0f, 100f);
            if (land)
            {
                ImGui.Checkbox("Water Group", ref grp.IsWater);
            }
            int minH = grp.MinHeight;
            int maxH = grp.MaxHeight;
            ImGui.DragInt("Min Height", ref minH, 1, -128, 127);
            ImGui.DragInt("Max Height", ref maxH, 1, -128, 127);
            grp.MinHeight = (sbyte)minH;
            grp.MaxHeight = (sbyte)maxH;
            if (ImGui.BeginChild($"{selected}_tiles", new System.Numerics.Vector2(0, 100), ImGuiChildFlags.Borders))
            {
                foreach (var id in grp.Ids.ToArray())
                {
                    ImGui.Text($"0x{id:X4}");
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(1, 0, 0, 0.2f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(1, 0, 0, 1));
                    if (ImGui.SmallButton($"x##{id}"))
                    {
                        grp.Ids.Remove(id);
                    }
                    ImGui.PopStyleColor(2);
                }
                ImGui.Button($"+##{selected}");
                if (ImGui.BeginDragDropTarget())
                {
                    var payloadPtr = ImGui.AcceptDragDropPayload(land ? TilesWindow.Land_DragDrop_Target_Type : TilesWindow.Static_DragDrop_Target_Type);
                    unsafe
                    {
                        if (payloadPtr.NativePtr != null)
                        {
                            var dataPtr = (int*)payloadPtr.Data;
                            ushort id = (ushort)dataPtr[0];
                            grp.Ids.Add(id);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
                ImGui.EndChild();
            }
        }
    }

    private enum Region
    {
        Plains,
        Desert,
        Mountain,
        Swamp
    }

    private class Group
    {
        public float Chance = 100f;
        public bool IsWater = false;
        public sbyte MinHeight = -128;
        public sbyte MaxHeight = 127;

        [JsonConverter(typeof(HexUShortListConverter))]
        public List<ushort> Ids = new();
    }

    private class GroupsData
    {
        public Dictionary<string, Group> TileGroups { get; set; } = new();
        public Dictionary<string, Group> StaticGroups { get; set; } = new();
    }

    private class HexUShortListConverter : JsonConverter<List<ushort>>
    {
        public override List<ushort> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<ushort>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return result;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
                if (reader.TokenType == JsonTokenType.Number)
                {
                    result.Add(reader.GetUInt16());
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    var str = reader.GetString();
                    if (string.IsNullOrWhiteSpace(str))
                        continue;
                    if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        str = str.Substring(2);
                    if (ushort.TryParse(str, out var val))
                        result.Add(val);
                    else if (ushort.TryParse(str, System.Globalization.NumberStyles.HexNumber, null, out val))
                        result.Add(val);
                }
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<ushort> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var v in value)
                writer.WriteStringValue($"0x{v:X4}");
            writer.WriteEndArray();
        }
    }

    private void SaveGroups()
    {
        var data = new GroupsData
        {
            TileGroups = tileGroups,
            StaticGroups = staticGroups
        };
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        File.WriteAllText(GroupsFile, JsonSerializer.Serialize(data, options));
    }

    private void SaveApiKey()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ApiKeyFile, JsonSerializer.Serialize(new { ApiKey = apiKey }, options));
    }

    private void LoadApiKey()
    {
        try
        {
            if (!File.Exists(ApiKeyFile))
                return;
            var json = File.ReadAllText(ApiKeyFile);
            if (string.IsNullOrWhiteSpace(json))
                return;
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ApiKey", out var val))
            {
                var key = val.GetString();
                if (!string.IsNullOrWhiteSpace(key))
                    apiKey = key;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load API key: {e.Message}");
        }
    }

    private void LoadGroups() => LoadGroups(GroupsFile);

    private void LoadGroups(string path)
    {
        if (!File.Exists(path))
            return;
        var data = JsonSerializer.Deserialize<GroupsData>(File.ReadAllText(path), new JsonSerializerOptions
        {
            IncludeFields = true
        });
        if (data == null)
            return;
        tileGroups.Clear();
        foreach (var kv in data.TileGroups)
            tileGroups[kv.Key] = kv.Value;
        staticGroups.Clear();
        foreach (var kv in data.StaticGroups)
            staticGroups[kv.Key] = kv.Value;
    }

    private class Perlin
    {
        private readonly int[] p = new int[512];

        public Perlin(int seed)
        {
            var perm = Enumerable.Range(0, 256).ToArray();
            var rnd = new Random(seed);
            for (int i = 0; i < 256; i++)
            {
                var j = rnd.Next(i, 256);
                (perm[i], perm[j]) = (perm[j], perm[i]);
                p[i] = perm[i];
                p[i + 256] = perm[i];
            }
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static float Lerp(float t, float a, float b) => a + t * (b - a);
        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y;
            float v = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public float Noise(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);
            int a = p[xi] + yi;
            int b = p[xi + 1] + yi;
            float u = Fade(xf);
            float v = Fade(yf);
            return Lerp(v,
                Lerp(u, Grad(p[a], xf, yf), Grad(p[b], xf - 1, yf)),
                Lerp(u, Grad(p[a + 1], xf, yf - 1), Grad(p[b + 1], xf - 1, yf - 1)));
        }

        public float Fractal(float x, float y, float roughness)
        {
            float total = 0;
            float amplitude = 1;
            float frequency = 1;
            float max = 0;
            for (int i = 0; i < 4; i++)
            {
                total += Noise(x * frequency, y * frequency) * amplitude;
                max += amplitude;
                amplitude *= roughness;
                frequency *= 2;
            }
            return total / max;
        }
    }
}
