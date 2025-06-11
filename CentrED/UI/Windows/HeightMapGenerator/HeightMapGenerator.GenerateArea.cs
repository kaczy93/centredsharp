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

    private void GenerateArea(int startX, int startY, int width, int height, List<Group> groupsList, float total, CancellationToken ct)
    {
        int endX = Math.Min(mapSizeX - 1, startX + width - 1);
        int endY = Math.Min(mapSizeY - 1, startY + height - 1);

        for (int bx = startX; bx <= endX && !ct.IsCancellationRequested; bx += BlockSize)
        {
            int ex = Math.Min(endX, bx + BlockSize - 1);
            for (int by = startY; by <= endY && !ct.IsCancellationRequested; by += BlockSize)
            {
                int ey = Math.Min(endY, by + BlockSize - 1);
                CEDClient.LoadBlocks(new AreaInfo((ushort)bx, (ushort)by, (ushort)ex, (ushort)ey));
                for (int x = bx; x <= ex && !ct.IsCancellationRequested; x++)
                {
                    for (int y = by; y <= ey && !ct.IsCancellationRequested; y++)
                    {
                        if (!CEDClient.TryGetLandTile(x, y, out var landTile))
                            continue;
                        var z = heightData[x, y];
                        ushort id;
                        if (tileMap != null)
                        {
                            id = tileMap[x, y].Id;
                        }
                        else
                        {
                            var candidates = groupsList.Where(g => z >= g.MinHeight && z <= g.MaxHeight).ToList();
                            if (candidates.Count == 0)
                                candidates = groupsList;
                            if (candidates.Count > 0)
                            {
                                var grp = SelectGroup(candidates);
                                id = grp.Ids[Random.Shared.Next(grp.Ids.Count)];
                            }
                            else
                            {
                                id = 0;
                            }
                        }
                        if (id != 0)
                            landTile.ReplaceLand(id, z);
                        
                        generationProgress += 1f / total;
                        if (ct.IsCancellationRequested)
                            break;
                    }
                    if (ct.IsCancellationRequested)
                        break;
                }
                if (ct.IsCancellationRequested)
                    break;
            }
            if (ct.IsCancellationRequested)
                break;
        }

        CEDClient.Flush();
        CEDClient.Update();
    }
}
