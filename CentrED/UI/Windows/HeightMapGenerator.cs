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

    private const int MapSize = 4096;
    private const int BlockSize = 256;
    private const int MaxTiles = 16 * 1024 * 1024;
    private const string GroupsFile = "heightmap_groups.json";

    private string groupsPath = GroupsFile;

    private string heightMapPath = string.Empty;
    private sbyte[,]? heightData;
    private Color[]? heightMapTextureData;
    private int heightMapWidth;
    private int heightMapHeight;
    private int selectedQuadrant = 0;

    private readonly Dictionary<string, Group> tileGroups = new();
    private string selectedGroup = string.Empty;
    private string newGroupName = string.Empty;

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

        heightData = new sbyte[MapSize, MapSize];
        for (int y = 0; y < MapSize; y++)
        {
            int sy = qy * quadHeight + (int)(y / (float)MapSize * quadHeight);
            for (int x = 0; x < MapSize; x++)
            {
                int sx = qx * quadWidth + (int)(x / (float)MapSize * quadWidth);
                var c = heightMapTextureData[sy * heightMapWidth + sx];
                float v = c.R / 255f;
                int z = (int)MathF.Round(v * 254f - 127f);
                heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
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

        generationTask = Task.Run(() =>
        {
            var groupsList = tileGroups.Values.Where(g => g.Ids.Count > 0).ToList();
            if (groupsList.Count == 0)
                return;

            var total = MapSize * MapSize;
            if (total > MaxTiles)
                return;
            CEDClient.BulkMode = true;
            try
            {
                GenerateFractalRegion(0, 0, MapSize, MapSize, groupsList, total);
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
                GenerateFractalRegion(offX, offY, w, h, groupsList, total);
                offX += w;
            }
            offY += h;
        }
    }

    private void GenerateArea(int startX, int startY, int width, int height, List<Group> groupsList, float total)
    {
        int endX = Math.Min(MapSize - 1, startX + width - 1);
        int endY = Math.Min(MapSize - 1, startY + height - 1);

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
}
