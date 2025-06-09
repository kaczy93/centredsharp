using System;
using System.Linq;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
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

    private int x1;
    private int y1;
    private int x2;
    private int y2;

    private int regionType;
    private int seed = 0;
    private float roughness = 1.0f;

    private static readonly string[] RegionNames = Enum.GetNames<Region>();

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
        if (ImGui.Button("Generate"))
        {
            Generate();
        }
    }

    private void Generate()
    {
        var noise = new Perlin(seed);
        var startX = Math.Min(x1, x2);
        var endX = Math.Max(x1, x2);
        var startY = Math.Min(y1, y2);
        var endY = Math.Max(y1, y2);
        for (var x = startX; x <= endX; x++)
        {
            for (var y = startY; y <= endY; y++)
            {
                if (!CEDClient.TryGetLandTile(x, y, out var landTile))
                    continue;
                var n = noise.Fractal(x * 0.1f, y * 0.1f, roughness);
                var settings = GetSettings((Region)regionType);
                var z = (sbyte)Math.Clamp((int)(n * settings.altitudeRange + settings.altitudeOffset), -128, 127);
                landTile.ReplaceLand(settings.landId, z);
                if (settings.staticId != 0 && Random.Shared.NextDouble() < settings.staticChance)
                {
                    CEDClient.Add(new StaticTile(settings.staticId, (ushort)x, (ushort)y, z, 0));
                }
            }
        }
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

    private enum Region
    {
        Plains,
        Desert,
        Mountain,
        Swamp
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
