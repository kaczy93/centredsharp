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
}
