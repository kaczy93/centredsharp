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

public class HeightMapGenerator : Window
{
    public override string Name => "HeightMap Generator";

    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    private const int MapSizeX = 4096;
    private const int MapSizeY = 4096;
    private const int BlockSize = 256;
    private const int MaxTiles = 16 * 1024 * 1024;
    private const string GroupsFile = "heightmap_groups.json";


    private static readonly (sbyte Min, sbyte Max)[] HeightRanges =
    {
        (-127, -126), // water
        (-125, -100), // sand
        (-99, -74),   // grass
        (-73, -48),   // jungle
        (-47, -23),   // rock
        (-22, 3)      // snow
    };
    private const int NUM_CHANNELS = 6;
    private const float NOISE_SCALE = 0.05f;
    private const float NOISE_ROUGHNESS = 0.5f;
    private const int SMOOTH_RADIUS = 64;

    private string groupsPath = GroupsFile;

    private string heightMapPath = string.Empty;
    private sbyte[,]? heightData;
    private Color[]? heightMapTextureData;
    private int heightMapWidth;
    private int heightMapHeight;
    private int selectedQuadrant = 0;

    private readonly Perlin noise = new(Environment.TickCount);

    private readonly Dictionary<string, Group> tileGroups = new();
    private string selectedGroup = string.Empty;
    private string newGroupName = string.Empty;

    private string _statusText = string.Empty;
    private System.Numerics.Vector4 _statusColor = UIManager.Red;

    private Task? generationTask;
    private float generationProgress;

    protected override void InternalDraw()
    {
        if (!CEDClient.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }

        ImGui.BeginDisabled(generationTask != null && !generationTask.IsCompleted);
        if (ImGui.Button("Load Heightmap"))
        {
            if (TinyFileDialogs.TryOpenFile("Select Heightmap", Environment.CurrentDirectory, new[] { "*.png" }, "PNG Files", false, out var path))
            {
                LoadHeightmap(path);
            }
        }
        ImGui.EndDisabled();
        if (!string.IsNullOrEmpty(heightMapPath))
        {
            ImGui.Text($"Loaded: {Path.GetFileName(heightMapPath)}");
        }

        ImGui.Text("Quadrant");
        for (int qy = 0; qy < 3; qy++)
        {
            for (int qx = 0; qx < 3; qx++)
            {
                int idx = qy * 3 + qx;
                bool check = selectedQuadrant == idx;
                if (ImGui.Checkbox($"##q_{qy}_{qx}", ref check))
                {
                    if (check)
                    {
                        selectedQuadrant = idx;
                        UpdateHeightData();
                    }
                }
                if (qx < 2) ImGui.SameLine();
            }
        }

        ImGui.Separator();
        ImGui.Text("Tile Groups");
        DrawGroups(tileGroups, ref selectedGroup, ref newGroupName);
        if (ImGui.Button("Save Groups"))
        {
            if (TinyFileDialogs.TrySaveFile("Save Groups", groupsPath, new[] { "*.json" }, "JSON Files", out var path))
            {
                SaveGroups(path);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Load Groups"))
        {
            if (TinyFileDialogs.TryOpenFile("Load Groups", Environment.CurrentDirectory, new[] { "*.json" }, "JSON Files", false, out var path))
            {
                LoadGroups(path);
            }
        }
        ImGui.Separator();

        ImGui.BeginDisabled(heightData == null || generationTask != null && !generationTask.IsCompleted);
        if (ImGui.Button("Generate"))
        {
            Generate();
        }
        ImGui.EndDisabled();
        ImGui.TextColored(UIManager.Red, "This operation cannot be undone!");
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
        if (!string.IsNullOrEmpty(_statusText))
        {
            ImGui.TextColored(_statusColor, _statusText);
        }
    }

    private void LoadHeightmap(string path)
    {
        if (generationTask != null && !generationTask.IsCompleted)
            return;
        try
        {
            using var fs = File.OpenRead(path);
            var tex = Texture2D.FromStream(CEDGame.GraphicsDevice, fs);
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);

            heightMapTextureData = data;
            heightMapWidth = tex.Width;
            heightMapHeight = tex.Height;

            UpdateHeightData();
            heightMapPath = path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load heightmap: {e.Message}");
            heightData = null;
            heightMapPath = string.Empty;
        }
    }

    private void UpdateHeightData()
    {
        if (heightMapTextureData == null)
            return;

        int quadWidth = heightMapWidth / 3;
        int quadHeight = heightMapHeight / 3;
        int qx = selectedQuadrant % 3;
        int qy = selectedQuadrant / 3;

        var groupsOrdered = tileGroups.Values.OrderBy(g => g.MinHeight).ToArray();

        heightData = new sbyte[MapSizeX, MapSizeY];

        // ---------------------------
        // 1. Primeiro passo: calcular idx para todos os pixels
        // ---------------------------
        // Build palette mapping based on unique brightness values
        int[] palette = new int[256];
        {
            HashSet<int> uniques = new();
            for (int iy = 0; iy < heightMapHeight; iy++)
            {
                for (int ix = 0; ix < heightMapWidth; ix++)
                {
                    var col = heightMapTextureData[iy * heightMapWidth + ix];
                    int b = (int)MathF.Round((col.R + col.G + col.B) / 3f);
                    uniques.Add(Math.Clamp(b, 0, 255));
                }
            }
            var sorted = uniques.OrderBy(v => v).ToArray();
            if (sorted.Length == NUM_CHANNELS)
            {
                int prev = 0;
                for (int i = 0; i < sorted.Length; i++)
                {
                    int next = i < sorted.Length - 1 ? (sorted[i] + sorted[i + 1]) / 2 : 256;
                    for (int b = prev; b < next; b++)
                        palette[b] = i;
                    prev = next;
                }
            }
            else
            {
                for (int b = 0; b < 256; b++)
                    palette[b] = Math.Clamp((int)(b / (256f / NUM_CHANNELS)), 0, NUM_CHANNELS - 1);
            }
        }

        int[,] idxMap = new int[MapSizeX, MapSizeY];
        for (int y = 0; y < MapSizeY; y++)
        {
            int sy = qy * quadHeight + (int)(y / (float)MapSizeY * quadHeight);
            for (int x = 0; x < MapSizeX; x++)
            {
                int sx = qx * quadWidth + (int)(x / (float)MapSizeX * quadWidth);
                var c = heightMapTextureData[sy * heightMapWidth + sx];
                int brightness = (int)MathF.Round((c.R + c.G + c.B) / 3f);
                idxMap[x, y] = palette[Math.Clamp(brightness, 0, 255)];
            }
        }

        // ---------------------------
        // 2. Primeiro passo: gerar altura base sem suavização
        // ---------------------------
        for (int y = 0; y < MapSizeY; y++)
        {
            for (int x = 0; x < MapSizeX; x++)
            {
                int idx = idxMap[x, y];
                var range = HeightRanges[idx];
                int z;

                bool isEdge = false;
                for (int dy = -1; dy <= 1 && !isEdge; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        if (idxMap[nx, ny] != idx)
                        {
                            isEdge = true;
                            break;
                        }
                    }
                }

                if (idx == 0)
                {
                    z = -127;
                }
                else
                {
                    float n = noise.Fractal(x * NOISE_SCALE, y * NOISE_SCALE, NOISE_ROUGHNESS);
                    float t = (n + 1f) * 0.5f;
                    z = (int)MathF.Round(range.Min + t * (range.Max - range.Min));
                    if (isEdge)
                    {
                        float edgePerturb = noise.Noise(x * 0.3f, y * 0.3f);
                        z += (int)(edgePerturb * 3);
                    }
                }

                heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
            }
        }

        // ---------------------------
        // 3. Aplicar suavização entre biomas
        // ---------------------------
        for (int src = 0; src < NUM_CHANNELS - 1; src++)
        {
            int[,] distMap = new int[MapSizeX, MapSizeY];
            var queue = new Queue<(int X, int Y)>();
            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    if (idxMap[x, y] == src)
                    {
                        distMap[x, y] = 0;
                        queue.Enqueue((x, y));
                    }
                    else
                    {
                        distMap[x, y] = int.MaxValue;
                    }
                }
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                int nd = distMap[cx, cy] + 1;
                if (nd > SMOOTH_RADIUS)
                    continue;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = cy + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        if (nd < distMap[nx, ny])
                        {
                            distMap[nx, ny] = nd;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }

            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    if (idxMap[x, y] != src + 1) continue;
                    int dist = distMap[x, y];
                    if (dist > SMOOTH_RADIUS) continue;

                    if (src == 3 && idxMap[x, y] == src + 1)
                        continue; // no smoothing from jungle to rock

                    int z;
                    if (dist <= 1)
                        z = HeightRanges[src + 1].Min;
                    else if (dist == 2)
                        z = HeightRanges[src].Max;
                    else
                    {
                        float lerpT = (dist - 2) / (float)(SMOOTH_RADIUS - 2);
                        z = (int)MathF.Round(MathHelper.Lerp(HeightRanges[src].Max, heightData[x, y], lerpT));
                    }
                    heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
                }
            }
        }

        // Segunda passada para suavizar o lado oposto das bordas
        for (int src = NUM_CHANNELS - 1; src > 0; src--)
        {
            int[,] distMap = new int[MapSizeX, MapSizeY];
            var queue = new Queue<(int X, int Y)>();
            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    if (idxMap[x, y] == src)
                    {
                        distMap[x, y] = 0;
                        queue.Enqueue((x, y));
                    }
                    else
                    {
                        distMap[x, y] = int.MaxValue;
                    }
                }
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                int nd = distMap[cx, cy] + 1;
                if (nd > SMOOTH_RADIUS)
                    continue;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = cy + dy;
                    if (ny < 0 || ny >= MapSizeY) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        if (nx < 0 || nx >= MapSizeX) continue;
                        if (nd < distMap[nx, ny])
                        {
                            distMap[nx, ny] = nd;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }

            for (int y = 0; y < MapSizeY; y++)
            {
                for (int x = 0; x < MapSizeX; x++)
                {
                    if (idxMap[x, y] != src - 1) continue;
                    int dist = distMap[x, y];
                    if (dist > SMOOTH_RADIUS) continue;

                    if (src == 4 && idxMap[x, y] == src - 1)
                        continue; // no smoothing from rock to jungle

                    int z;
                    if (dist <= 1)
                        z = HeightRanges[src - 1].Max;
                    else
                    {
                        float lerpT = (dist - 1) / (float)(SMOOTH_RADIUS - 1);
                        z = (int)MathF.Round(MathHelper.Lerp(HeightRanges[src].Min, heightData[x, y], lerpT));
                    }
                    heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
                }
            }
        }
    }

    private void Generate()
    {
        UpdateHeightData();
        if (heightData == null)
            return;
        if (generationTask != null && !generationTask.IsCompleted)
            return;

        _statusText = string.Empty;
        generationTask = Task.Run(() =>
        {
            var groupsList = tileGroups.Values.Where(g => g.Ids.Count > 0).ToList();
            if (groupsList.Count == 0)
            {
                _statusText = "No groups configured.";
                _statusColor = UIManager.Red;
                return;
            }

            var total = MapSizeX * MapSizeY;
            if (total > MaxTiles)
                return;
            CEDClient.BulkMode = true;
            try
            {
                GenerateFractalRegion(0, 0, MapSizeX, MapSizeY, groupsList, total);
            }
            finally
            {
                CEDClient.BulkMode = false;
                CEDClient.Flush();
                // Ensure pending packets are sent immediately after bulk mode
                CEDClient.Update();
            }
            generationProgress = 1f;
        });
    }

    private void GenerateFractalRegion(int startX, int startY, int width, int height, List<Group> groupsList, float total)
    {
        if (width <= BlockSize && height <= BlockSize)
        {
            GenerateArea(startX, startY, width, height, groupsList, total);
            return;
        }

        int stepX = width / 3;
        int stepY = height / 3;
        int remX = width % 3;
        int remY = height % 3;

        int offY = startY;
        for (int qy = 0; qy < 3; qy++)
        {
            int h = stepY + (qy < remY ? 1 : 0);
            int offX = startX;
            for (int qx = 0; qx < 3; qx++)
            {
                int w = stepX + (qx < remX ? 1 : 0);
                if (w == 0 || h == 0)
                {
                    Console.WriteLine($"Skipping zero-sized region {offX},{offY} size {w}x{h}");
                    continue;
                }
                GenerateFractalRegion(offX, offY, w, h, groupsList, total);
                offX += w;
            }
            offY += h;
        }
    }

    private void GenerateArea(int startX, int startY, int width, int height, List<Group> groupsList, float total)
    {
        int endX = Math.Min(MapSizeX - 1, startX + width - 1);
        int endY = Math.Min(MapSizeY - 1, startY + height - 1);

        for (int bx = startX; bx <= endX; bx += BlockSize)
        {
            int ex = Math.Min(endX, bx + BlockSize - 1);
            for (int by = startY; by <= endY; by += BlockSize)
            {
                int ey = Math.Min(endY, by + BlockSize - 1);
                CEDClient.LoadBlocks(new AreaInfo((ushort)bx, (ushort)by, (ushort)ex, (ushort)ey));
                for (int x = bx; x <= ex; x++)
                {
                    for (int y = by; y <= ey; y++)
                    {
                        if (!CEDClient.TryGetLandTile(x, y, out var landTile))
                            continue;
                        var z = heightData[x, y];
                        var candidates = groupsList.Where(g => z >= g.MinHeight && z <= g.MaxHeight).ToList();
                        if (candidates.Count == 0)
                            candidates = groupsList;
                        if (candidates.Count > 0)
                        {
                            var grp = SelectGroup(candidates);
                            var id = grp.Ids[Random.Shared.Next(grp.Ids.Count)];
                            landTile.ReplaceLand(id, z);
                        }
                        generationProgress += 1f / total;
                    }
                }
            }
        }

        CEDClient.Flush();
        CEDClient.Update();
    }

    private static Group SelectGroup(List<Group> groups)
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

    private void DrawGroups(Dictionary<string, Group> groups, ref string selected, ref string newName)
    {
        if (ImGui.BeginChild("LandGroups", new System.Numerics.Vector2(0, 120), ImGuiChildFlags.Borders))
        {
            foreach (var kv in groups.ToArray())
            {
                ImGui.PushID($"l_{kv.Key}");
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
        ImGui.InputText("##newgroup_l", ref newName, 32);
        ImGui.SameLine();
        if (ImGui.Button("Add##l"))
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
            ImGui.DragFloat($"Chance (%)##l_{selected}", ref grp.Chance, 0.1f, 0f, 100f);
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
                    var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
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

    private void SaveGroups() => SaveGroups(groupsPath);

    private void SaveGroups(string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
        File.WriteAllText(path, JsonSerializer.Serialize(tileGroups, options));
        groupsPath = path;
    }

    private void LoadGroups() => LoadGroups(groupsPath);

    private void LoadGroups(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var data = JsonSerializer.Deserialize<Dictionary<string, Group>>(File.ReadAllText(path), new JsonSerializerOptions
            {
                IncludeFields = true
            });
            if (data == null)
                return;
            tileGroups.Clear();
            foreach (var kv in data)
                tileGroups[kv.Key] = kv.Value;
            groupsPath = path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load groups: {e.Message}");
        }
    }

    private class Group
    {
        public float Chance = 100f;
        public sbyte MinHeight = -128;
        public sbyte MaxHeight = 127;
        public List<ushort> Ids = new();
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
