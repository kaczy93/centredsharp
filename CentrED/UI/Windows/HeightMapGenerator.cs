using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
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

    private string heightMapPath = string.Empty;
    private sbyte[,]? heightData;

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

        if (ImGui.Button("Load Heightmap"))
        {
            if (TinyFileDialogs.TryOpenFile("Select Heightmap", Environment.CurrentDirectory, new[] { "*.png" }, "PNG Files", false, out var path))
            {
                LoadHeightmap(path);
            }
        }
        if (!string.IsNullOrEmpty(heightMapPath))
        {
            ImGui.Text($"Loaded: {Path.GetFileName(heightMapPath)}");
        }

        ImGui.Separator();
        ImGui.Text("Tile Groups");
        DrawGroups(tileGroups, ref selectedGroup, ref newGroupName);
        ImGui.Separator();

        ImGui.BeginDisabled(heightData == null || generationTask != null && !generationTask.IsCompleted);
        if (ImGui.Button("Generate"))
        {
            Generate();
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
                ImGui.ProgressBar(generationProgress, new Vector2(-1, 0));
            }
        }
    }

    private void LoadHeightmap(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var tex = Texture2D.FromStream(CEDGame.GraphicsDevice, fs);
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);

            heightData = new sbyte[MapSize, MapSize];
            for (int y = 0; y < MapSize; y++)
            {
                int sy = (int)(y / (float)MapSize * tex.Height);
                for (int x = 0; x < MapSize; x++)
                {
                    int sx = (int)(x / (float)MapSize * tex.Width);
                    var c = data[sy * tex.Width + sx];
                    float v = c.R / 255f; // grayscale
                    int z = (int)MathF.Round(v * 254f - 127f);
                    heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
                }
            }
            heightMapPath = path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load heightmap: {e.Message}");
            heightData = null;
            heightMapPath = string.Empty;
        }
    }

    private void Generate()
    {
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

            for (int bx = 0; bx < MapSize; bx += BlockSize)
            {
                int ex = Math.Min(MapSize - 1, bx + BlockSize - 1);
                for (int by = 0; by < MapSize; by += BlockSize)
                {
                    int ey = Math.Min(MapSize - 1, by + BlockSize - 1);
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
            generationProgress = 1f;
        });
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
        if (ImGui.BeginChild("LandGroups", new Vector2(0, 120), ImGuiChildFlags.Borders))
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
            if (ImGui.BeginChild($"{selected}_tiles", new Vector2(0, 100), ImGuiChildFlags.Borders))
            {
                foreach (var id in grp.Ids.ToArray())
                {
                    ImGui.Text($"0x{id:X4}");
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 0.2f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
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

    private class Group
    {
        public float Chance = 100f;
        public sbyte MinHeight = -128;
        public sbyte MaxHeight = 127;
        public List<ushort> Ids = new();
    }
}
