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
    private void Generate()
    {
        UpdateHeightData();
        if (heightData == null)
            return;
        if (generationTask != null && !generationTask.IsCompleted)
            return;

        _statusText = string.Empty;
        cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;
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
                GenerateFractalRegion(x1, y1, MapSizeX, MapSizeY, groupsList, total, token);
            }
            finally
            {
                CEDClient.BulkMode = false;
                CEDClient.Flush();
                // Ensure pending packets are sent immediately after bulk mode
                CEDClient.Update();
            }
            if (token.IsCancellationRequested)
            {
                _statusText = "Generation cancelled.";
                _statusColor = UIManager.Red;
            }
            else
            {
                generationProgress = 1f;
            }
        }, token);
    }
}
