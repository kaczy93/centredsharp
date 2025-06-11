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
