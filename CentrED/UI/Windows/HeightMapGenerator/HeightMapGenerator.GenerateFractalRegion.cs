using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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
    private void GenerateFractalRegion(int startX, int startY, int width, int height, List<Group> groupsList, float total, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;

        if (width <= BlockSize && height <= BlockSize)
        {
            GenerateArea(startX, startY, width, height, groupsList, total, ct);
            return;
        }

        int stepX = width / 3;
        int stepY = height / 3;
        int remX = width % 3;
        int remY = height % 3;

        int offY = startY;
        for (int qy = 0; qy < 3 && !ct.IsCancellationRequested; qy++)
        {
            int h = stepY + (qy < remY ? 1 : 0);
            int offX = startX;
            for (int qx = 0; qx < 3 && !ct.IsCancellationRequested; qx++)
            {
                int w = stepX + (qx < remX ? 1 : 0);
                if (w == 0 || h == 0)
                {
                    Console.WriteLine($"Skipping zero-sized region {offX},{offY} size {w}x{h}");
                    continue;
                }
                GenerateFractalRegion(offX, offY, w, h, groupsList, total, ct);
                offX += w;
            }
            offY += h;
        }
    }
}
