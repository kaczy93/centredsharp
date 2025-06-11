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

        int prevX1 = x1;
        int prevX2 = x2;
        int prevY1 = y1;
        int prevY2 = y2;
        ImGui.InputInt("X1", ref x1);
        ImGui.InputInt("X2", ref x2);
        ImGui.InputInt("Y1", ref y1);
        ImGui.InputInt("Y2", ref y2);
        if (x1 != prevX1 || x2 != prevX2 || y1 != prevY1 || y2 != prevY2)
            UpdateHeightData();

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
        ImGui.Text("Transition Tiles");
        DrawTransitions(transitionTiles, ref selectedTransition);
        if (ImGui.Button("Save Transitions"))
        {
            if (TinyFileDialogs.TrySaveFile("Save Transitions", transitionsPath, new[] { "*.json" }, "JSON Files", out var path))
            {
                SaveTransitions(path);
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Load Transitions"))
        {
            if (TinyFileDialogs.TryOpenFile("Load Transitions", Environment.CurrentDirectory, new[] { "*.json" }, "JSON Files", false, out var path))
            {
                LoadTransitions(path);
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
                cancellationSource?.Dispose();
                cancellationSource = null;
                showProgressPopup = false;
            }
        }

        if (openProgressPopup)
        {
            ImGui.OpenPopup("HeightmapGeneration");
            openProgressPopup = false;
        }
        if (showProgressPopup && ImGui.BeginPopupModal("HeightmapGeneration", ref showProgressPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text("Generating heightmap, please wait...");
            ImGui.ProgressBar(generationProgress, new System.Numerics.Vector2(300, 0));
            if (ImGui.Button("Cancel"))
            {
                cancellationSource?.Cancel();
            }
            ImGui.EndPopup();
        }
        if (!string.IsNullOrEmpty(_statusText))
        {
            ImGui.TextColored(_statusColor, _statusText);
        }
    }
}
