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

public partial class HeightMapGenerator : Window
{
    public override string Name => "HeightMap Generator";

    public override WindowState DefaultState => new()
    {
        IsOpen = false
    };

    private const int MapSizeX = 4096;
    private const int MapSizeY = 4096;
    private const int BlockSize = 256;
    private const int MaxTiles = 16 * 1024 * 1024;
    private const string GroupsFile = "heightmap_groups.json";
    private const string TransitionsFile = "heightmap_transitions.json";

    private static readonly (sbyte Min, sbyte Max)[] HeightRanges =
    {
        (-127, -125), // water
        (-125, -100), // sand
        (-100, -74),   // grass
        (-74, -48),   // jungle
        (-48, -23),   // rock
        (-23, 3)      // snow
    };
    private const int NUM_CHANNELS = 6;
    private const float NOISE_SCALE = 0.05f;
    private const float NOISE_ROUGHNESS = 0.5f;
    private const int SMOOTH_RADIUS = 64;

    private string groupsPath = GroupsFile;
    private string transitionsPath = TransitionsFile;

    private string heightMapPath = string.Empty;
    private sbyte[,]? heightData;
    private Tile[,]? tileMap;
    private Color[]? heightMapTextureData;
    private int heightMapWidth;
    private int heightMapHeight;
    private int selectedQuadrant = 0;

    private readonly Perlin noise = new(Environment.TickCount);

    private readonly Dictionary<string, Group> tileGroups = new();
    private readonly Dictionary<string, Tile[]> transitionTiles = new()
    {
        ["water-sand"] = new Tile[9],
        ["sand-grass"] = new Tile[9],
        ["grass-jungle"] = new Tile[9],
        ["jungle-rock"] = new Tile[9],
        ["rock-snow"] = new Tile[9]
    };
    private string selectedGroup = string.Empty;
    private string selectedTransition = string.Empty;
    private string newGroupName = string.Empty;

    private string _statusText = string.Empty;
    private System.Numerics.Vector4 _statusColor = UIManager.Red;

    private CancellationTokenSource? cancellationSource;

    private Task? generationTask;
    private float generationProgress;
}
