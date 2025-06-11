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
                    else
                    {
                        float lerpT = (dist - 1) / (float)(SMOOTH_RADIUS - 1);
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

        // Ensure water tiles remain at the baseline height after smoothing
        for (int y = 0; y < MapSizeY; y++)
        {
            for (int x = 0; x < MapSizeX; x++)
            {
                if (idxMap[x, y] == 0)
                    heightData[x, y] = -126;
            }
        }
    }

}
