using CentrED.Client;
using CentrED.Network;
using CentrED.UI;
using Hexa.NET.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Imports a colored heightmap and optionally a vegetation map.
/// Maps biome colors to appropriate land tiles and vegetation colors to static objects.
/// </summary>
public partial class ImportColoredHeightmap : LocalLargeScaleTool
{
    public override string Name => LangManager.Get(LSO_IMPORT_COLORED_HEIGHTMAP);

    #region Fields

    private string _biomeFilePath = "";
    private string _heightmapFilePath = "";
    private string _vegetationFilePath = "";
    private string _structureFilePath = "";
    private Image<Rgb24>? _biomeFile;
    private Image<L8>? _heightmapFile;
    private Image<Rgb24>? _vegetationFile;
    private Image<Rgb24>? _structureFile;
    private bool _useHeightmap = false;
    private bool _importVegetation = false;
    private bool _importStructures = false;
    private bool _clearExistingStatics = false;
    private bool _vegetationOnly = false;
    private bool _coastlineOnly = false;

    // TilesBrush support
    private string _tilesBrushFilePath = "";
    private bool _useTilesBrush = false;
    private Dictionary<string, TilesBrushData>? _tilesBrushes;

    // Dragon AAAABBBB transition system
    private bool _useDragonTransitions = true;

    // Coastline fields are in ImportColoredHeightmap.Coastline.cs

    private int xOffset;
    private int yOffset;

    // Cache for biome data during processing
    private Biome[,]? _biomeCache;
    private sbyte[,]? _altitudeCache;
    private ushort[,]? _transitionCache;
    private bool _applyTransitions = true;
    private bool _smoothAltitude = true;
    private int _smoothRadius = 2;

    private Random _random = new();

    // Debug counters
    private Dictionary<Biome, int> _biomeCounts = new();
    private Dictionary<ushort, int> _tileCounts = new();

    #endregion

    #region UI

    protected override bool DrawToolUI()
    {
        var changed = false;

        // Biome map file
        ImGui.Text(LangManager.Get(IMPORT_BIOME_MAP));
        ImGui.InputText("##biomepath", ref _biomeFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("...##biome"))
        {
            if (TinyFileDialogs.TryOpenFile
                    (LangManager.Get(SELECT_FILE), Environment.CurrentDirectory, ["*.png", "*.bmp"], null, false, out var newPath))
            {
                _biomeFilePath = newPath;
                return false;
            }
        }

        ImGui.Separator();

        // Separate heightmap file (grayscale)
        changed |= ImGui.Checkbox("Use Separate Heightmap", ref _useHeightmap);
        ImGuiEx.Tooltip("Use a separate grayscale image for altitude (black=low, white=high)");

        if (_useHeightmap)
        {
            ImGui.Text("Heightmap (grayscale):");
            ImGui.InputText("##heightpath", ref _heightmapFilePath, 512);
            ImGui.SameLine();
            if (ImGui.Button("...##height"))
            {
                if (TinyFileDialogs.TryOpenFile
                        (LangManager.Get(SELECT_FILE), Environment.CurrentDirectory, ["*.png", "*.bmp"], null, false, out var newPath))
                {
                    _heightmapFilePath = newPath;
                    return false;
                }
            }
        }

        ImGui.Separator();

        // Structure map checkbox and file
        changed |= ImGui.Checkbox("Import Structures", ref _importStructures);
        ImGuiEx.Tooltip("Import structure map (walls, pillars) as static objects");

        if (_importStructures)
        {
            ImGui.Text("Structure Map");
            ImGui.InputText("##structpath", ref _structureFilePath, 512);
            ImGui.SameLine();
            if (ImGui.Button("...##struct"))
            {
                if (TinyFileDialogs.TryOpenFile
                        (LangManager.Get(SELECT_FILE), Environment.CurrentDirectory, ["*.png", "*.bmp"], null, false, out var newPath))
                {
                    _structureFilePath = newPath;
                    return false;
                }
            }
        }

        ImGui.Separator();

        // Vegetation-only mode
        changed |= ImGui.Checkbox("Vegetation Only Mode", ref _vegetationOnly);
        ImGuiEx.Tooltip("Import only vegetation/structures without modifying terrain. Biome map not required.");

        if (_vegetationOnly)
        {
            _importVegetation = true;
            _coastlineOnly = false;
        }

        // Coastline-only mode
        if (!_vegetationOnly)
        {
            changed |= ImGui.Checkbox("Coastline Only Mode", ref _coastlineOnly);
            ImGuiEx.Tooltip("Apply only coastline statics without modifying terrain. Requires biome map.");
        }

        if (_coastlineOnly)
        {
            _applyCoastline = true;
        }

        if (!_vegetationOnly && !_coastlineOnly)
        {
            changed |= ImGui.Checkbox(LangManager.Get(IMPORT_VEGETATION), ref _importVegetation);
            ImGuiEx.Tooltip(LangManager.Get(IMPORT_VEGETATION_TOOLTIP));
        }

        if (!_vegetationOnly && !_coastlineOnly)
        {
            changed |= ImGui.Checkbox("Apply Biome Transitions", ref _applyTransitions);
            ImGuiEx.Tooltip("Apply smooth transition tiles between different biomes");

            if (_applyTransitions)
            {
                ImGui.Indent();

                changed |= ImGui.Checkbox("Use Dragon AAAABBBB System", ref _useDragonTransitions);
                ImGuiEx.Tooltip("Use DragonMod-style 8-neighbor pattern matching for transitions (recommended)");

                changed |= ImGui.Checkbox("Use TilesBrush.xml", ref _useTilesBrush);
                ImGuiEx.Tooltip("Use TilesBrush.xml for more accurate biome transitions");

                if (_useTilesBrush)
                {
                    ImGui.Text("TilesBrush.xml:");
                    ImGui.InputText("##tilebrushpath", ref _tilesBrushFilePath, 512);
                    ImGui.SameLine();
                    if (ImGui.Button("...##tilebrush"))
                    {
                        if (TinyFileDialogs.TryOpenFile
                                ("Select TilesBrush.xml", Environment.CurrentDirectory, ["*.xml"], null, false, out var newPath))
                        {
                            _tilesBrushFilePath = newPath;
                            if (LoadTilesBrush(newPath))
                            {
                                Console.WriteLine($"TilesBrush.xml loaded successfully: {_tilesBrushes?.Count ?? 0} brushes");
                            }
                            return false;
                        }
                    }

                    if (_tilesBrushes != null)
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 0, 1), $"Loaded: {_tilesBrushes.Count} brushes");
                    }
                    else if (!string.IsNullOrEmpty(_tilesBrushFilePath))
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "Not loaded - click ... to load");
                    }
                }
                ImGui.Unindent();
            }

            changed |= ImGui.Checkbox("Smooth Altitude", ref _smoothAltitude);
            ImGuiEx.Tooltip("Apply smoothing to altitude to reduce sharp transitions");

            if (_smoothAltitude)
            {
                ImGui.SameLine();
                ImGui.PushItemWidth(80);
                changed |= ImGui.SliderInt("Radius", ref _smoothRadius, 1, 5);
                ImGui.PopItemWidth();
            }

            ImGui.Separator();

            // Coastline options
            changed |= ImGui.Checkbox("Apply Coastline", ref _applyCoastline);
            ImGuiEx.Tooltip("Automatically generate coastline transitions between land and water");

            if (_applyCoastline)
            {
                ImGui.Indent();
                ImGuiEx.Tooltip("Places wave statics on water tiles adjacent to land");
                ImGui.Unindent();
            }
        }

        if (_importVegetation)
        {
            ImGui.Text(LangManager.Get(IMPORT_VEGETATION_MAP));
            ImGui.InputText("##vegpath", ref _vegetationFilePath, 512);
            ImGui.SameLine();
            if (ImGui.Button("...##veg"))
            {
                if (TinyFileDialogs.TryOpenFile
                        (LangManager.Get(SELECT_FILE), Environment.CurrentDirectory, ["*.png", "*.bmp"], null, false, out var newPath))
                {
                    _vegetationFilePath = newPath;
                    return false;
                }
            }

            changed |= ImGui.Checkbox(LangManager.Get(CLEAR_EXISTING_STATICS), ref _clearExistingStatics);
            ImGuiEx.Tooltip(LangManager.Get(CLEAR_EXISTING_STATICS_TOOLTIP));
        }

        return !changed;
    }

    #endregion

    #region Validation

    public override bool CanSubmit(RectU16 area)
    {
        if (!_vegetationOnly)
        {
            try
            {
                using var fileStream = File.OpenRead(_biomeFilePath);
                _biomeFile = Image.Load<Rgb24>(fileStream);
            }
            catch (Exception e)
            {
                _submitStatus = string.Format(LangManager.Get(OPEN_FILE_ERROR_1INFO), e.Message);
                return false;
            }

            if (_biomeFile.Width != area.Width || _biomeFile.Height != area.Height)
            {
                _submitStatus = LangManager.Get(FILE_SIZE_MISMATCH_AREA);
                _biomeFile.Dispose();
                _biomeFile = null;
                return false;
            }
        }

        if (_useHeightmap && !_vegetationOnly)
        {
            try
            {
                using var heightStream = File.OpenRead(_heightmapFilePath);
                _heightmapFile = Image.Load<L8>(heightStream);
            }
            catch (Exception e)
            {
                _submitStatus = $"Heightmap error: {e.Message}";
                _biomeFile?.Dispose();
                _biomeFile = null;
                return false;
            }

            if (_heightmapFile.Width != area.Width || _heightmapFile.Height != area.Height)
            {
                _submitStatus = "Heightmap size mismatch";
                _biomeFile?.Dispose();
                _biomeFile = null;
                _heightmapFile?.Dispose();
                _heightmapFile = null;
                return false;
            }
        }

        if (_importStructures && !string.IsNullOrEmpty(_structureFilePath))
        {
            try
            {
                using var fileStream = File.OpenRead(_structureFilePath);
                _structureFile = Image.Load<Rgb24>(fileStream);
            }
            catch (Exception e)
            {
                _submitStatus = $"Structure map error: {e.Message}";
                _biomeFile?.Dispose();
                _biomeFile = null;
                _heightmapFile?.Dispose();
                _heightmapFile = null;
                return false;
            }

            if (_structureFile.Width != area.Width || _structureFile.Height != area.Height)
            {
                _submitStatus = "Structure map size mismatch";
                _biomeFile?.Dispose();
                _biomeFile = null;
                _structureFile?.Dispose();
                _structureFile = null;
                return false;
            }
        }

        if (_importVegetation && !string.IsNullOrEmpty(_vegetationFilePath))
        {
            try
            {
                using var fileStream = File.OpenRead(_vegetationFilePath);
                _vegetationFile = Image.Load<Rgb24>(fileStream);
            }
            catch (Exception e)
            {
                _submitStatus = string.Format(LangManager.Get(OPEN_FILE_ERROR_1INFO), e.Message);
                _biomeFile?.Dispose();
                _biomeFile = null;
                return false;
            }

            if (_vegetationFile.Width != area.Width || _vegetationFile.Height != area.Height)
            {
                _submitStatus = LangManager.Get(FILE_SIZE_MISMATCH_AREA);
                _biomeFile?.Dispose();
                _vegetationFile?.Dispose();
                _biomeFile = null;
                _vegetationFile = null;
                return false;
            }
        }

        if (_useTilesBrush && _tilesBrushes == null && !string.IsNullOrEmpty(_tilesBrushFilePath))
        {
            if (!LoadTilesBrush(_tilesBrushFilePath))
            {
                _submitStatus = "Failed to load TilesBrush.xml";
                _biomeFile?.Dispose();
                _biomeFile = null;
                _heightmapFile?.Dispose();
                _heightmapFile = null;
                _structureFile?.Dispose();
                _structureFile = null;
                _vegetationFile?.Dispose();
                _vegetationFile = null;
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Processing

    protected override void PreProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PreProcessArea(client, area);
        xOffset = area.X1;
        yOffset = area.Y1;
        _random = new Random(42);
        _biomeCounts.Clear();
        _tileCounts.Clear();

        // Reload files if they were disposed
        if (_biomeFile == null && !string.IsNullOrEmpty(_biomeFilePath))
        {
            try
            {
                using var fileStream = File.OpenRead(_biomeFilePath);
                _biomeFile = Image.Load<Rgb24>(fileStream);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reloading biome file: {e.Message}");
                return;
            }
        }

        if (_useHeightmap && _heightmapFile == null && !string.IsNullOrEmpty(_heightmapFilePath))
        {
            try
            {
                using var heightStream = File.OpenRead(_heightmapFilePath);
                _heightmapFile = Image.Load<L8>(heightStream);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reloading heightmap file: {e.Message}");
            }
        }

        if (_vegetationOnly)
            return;

        if (_biomeFile == null) return;

        int width = _biomeFile.Width;
        int height = _biomeFile.Height;

        // Build biome cache (needed for both normal mode and coastline-only mode)
        _biomeCache = new Biome[width, height];

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                var color = _biomeFile[px, py];
                var biome = ClassifyBiomeColorOnly(color.R, color.G, color.B);
                _biomeCache[px, py] = biome;
            }
        }

        // Coastline-only mode: skip altitude and transitions
        // Coastline is applied directly in ProcessTile using map tile IDs
        if (_coastlineOnly)
        {
            return;
        }

        // Build altitude cache for normal mode
        var rawAltitude = new sbyte[width, height];

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                var color = _biomeFile[px, py];

                if (_useHeightmap && _heightmapFile != null)
                {
                    var gray = _heightmapFile[px, py].PackedValue;
                    rawAltitude[px, py] = (sbyte)(gray - 128);
                }
                else
                {
                    var (_, altitude) = ClassifyBiomeColor(color.R, color.G, color.B);
                    rawAltitude[px, py] = altitude;
                }
            }
        }

        // Apply altitude smoothing if enabled
        if (_smoothAltitude)
        {
            _altitudeCache = new sbyte[width, height];

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    var currentBiome = _biomeCache[px, py];

                    if (currentBiome == Biome.Water)
                    {
                        _altitudeCache[px, py] = rawAltitude[px, py];
                        continue;
                    }

                    // Rock biome ALWAYS uses smooth radius 1, regardless of user setting
                    int radius = (currentBiome == Biome.Rock) ? 1 : _smoothRadius;

                    int sum = 0;
                    int count = 0;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = px + dx;
                            int ny = py + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (_biomeCache[nx, ny] != Biome.Water)
                                {
                                    sum += rawAltitude[nx, ny];
                                    count++;
                                }
                            }
                        }
                    }

                    _altitudeCache[px, py] = count > 0
                        ? (sbyte)Math.Clamp(sum / count, -25, 127)
                        : rawAltitude[px, py];
                }
            }
        }
        else
        {
            _altitudeCache = rawAltitude;
        }

        // BIDIRECTIONAL TRANSITION SYSTEM - All border tiles get transitions
        if (_applyTransitions)
        {
            // Initialize Dragon transitions if enabled
            if (_useDragonTransitions)
            {
                InitializeDragonTransitions();
            }

            _transitionCache = new ushort[width, height];

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    var centerBiome = _biomeCache[px, py];

                    if (centerBiome == Biome.Water || centerBiome == Biome.Lava || centerBiome == Biome.Void)
                    {
                        _transitionCache[px, py] = 0;
                        continue;
                    }

                    // Check if this tile has any different neighbor (cardinal or diagonal)
                    bool hasDifferentNeighbor = false;
                    int[] dx = { 0, 0, 1, -1, -1, 1, -1, 1 };  // N, S, E, W, NW, NE, SW, SE
                    int[] dy = { -1, 1, 0, 0, -1, -1, 1, 1 };

                    for (int i = 0; i < dx.Length; i++)
                    {
                        int nx = px + dx[i];
                        int ny = py + dy[i];

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            var neighborBiome = _biomeCache[nx, ny];
                            if (neighborBiome != centerBiome &&
                                neighborBiome != Biome.Water &&
                                neighborBiome != Biome.Lava &&
                                neighborBiome != Biome.Void)
                            {
                                hasDifferentNeighbor = true;
                                break;
                            }
                        }
                    }

                    if (hasDifferentNeighbor)
                    {
                        if (_useDragonTransitions)
                        {
                            // Use Dragon AAAABBBB system
                            _transitionCache[px, py] = CalculateDragonTransition(px, py, centerBiome, width, height);
                        }
                        else
                        {
                            // Use original transition system
                            _transitionCache[px, py] = CalculateTransitionTile(px, py, centerBiome, width, height);
                        }
                    }
                    else
                    {
                        _transitionCache[px, py] = 0;
                    }
                }
            }
        }

        // Coastline is now applied directly in ProcessTile using map tile IDs
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        var pixelX = x - xOffset;
        var pixelY = y - yOffset;

        var landTile = client.GetLandTile(x, y);
        var currentZ = landTile.Z;

        // Coastline-only mode: coastline is applied in batch via PostProcessArea
        if (_coastlineOnly)
        {
            return;
        }

        if (!_vegetationOnly)
        {
            var biome = _biomeCache != null ? _biomeCache[pixelX, pixelY] : Biome.Grass;
            var newZ = _altitudeCache != null ? _altitudeCache[pixelX, pixelY] : (sbyte)0;

            ushort tileId;
            if (_applyTransitions && _transitionCache != null)
            {
                var cachedTile = _transitionCache[pixelX, pixelY];
                if (cachedTile != 0)
                {
                    tileId = cachedTile;
                }
                else
                {
                    tileId = GetLandTileForBiome(biome);
                }
            }
            else
            {
                tileId = GetLandTileForBiome(biome);
            }

            landTile.ReplaceLand(tileId, newZ);

            _biomeCounts[biome] = _biomeCounts.GetValueOrDefault(biome) + 1;
            _tileCounts[tileId] = _tileCounts.GetValueOrDefault(tileId) + 1;

            currentZ = newZ;
            // Coastline is applied in batch via PostProcessArea
        }

        if ((_importVegetation || _importStructures) && _clearExistingStatics)
        {
            foreach (var staticTile in client.GetStaticTiles(x, y).ToArray())
            {
                client.Remove(staticTile);
            }
        }

        if (_importStructures && _structureFile != null)
        {
            var structColor = _structureFile[pixelX, pixelY];
            var structType = ClassifyStructureColor(structColor.R, structColor.G, structColor.B);

            if (structType != StructureType.None)
            {
                var staticId = GetStaticForStructure(structType);
                if (staticId != 0)
                {
                    client.Add(new StaticTile(staticId, x, y, currentZ, 0));
                }
            }
        }

        if (_importVegetation && _vegetationFile != null)
        {
            var vegColor = _vegetationFile[pixelX, pixelY];
            var vegType = ClassifyVegetationColor(vegColor.R, vegColor.G, vegColor.B);

            if (vegType != VegetationType.None)
            {
                ushort staticId;
                sbyte staticZ;

                if (IsCoastlineVegetation(vegType))
                {
                    // Coastline: get direction-specific static and place at z=-4
                    var direction = GetCoastlineDirection(vegColor.B);
                    staticId = GetCoastlineStaticForDirection(direction);
                    staticZ = -4;
                }
                else
                {
                    // Regular vegetation
                    staticId = GetStaticForVegetation(vegType);
                    staticZ = currentZ;
                }

                if (staticId != 0)
                {
                    client.Add(new StaticTile(staticId, x, y, staticZ, 0));
                }
            }
        }
    }

    protected override void PostProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PostProcessArea(client, area);

        // Apply coastline in batch after all tiles have been imported
        ApplyCoastlineToArea(client, area);

        Console.WriteLine("=== ImportColoredHeightmap Debug ===");
        Console.WriteLine($"Coastline: processed {_coastlineProcessed} tiles, added {_coastlineAdded} statics, modified {_coastlineTerrainModified} terrain Z");
        _coastlineProcessed = 0;
        _coastlineAdded = 0;
        _coastlineTerrainModified = 0;
        Console.WriteLine("Biome distribution:");
        foreach (var (biome, count) in _biomeCounts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {biome}: {count}");
        }
        Console.WriteLine("\nTop 10 tile IDs used:");
        foreach (var (tileId, count) in _tileCounts.OrderByDescending(x => x.Value).Take(10))
        {
            Console.WriteLine($"  0x{tileId:X4}: {count}");
        }
        Console.WriteLine("====================================");

        _biomeFile?.Dispose();
        _biomeFile = null;
        _heightmapFile?.Dispose();
        _heightmapFile = null;
        _vegetationFile?.Dispose();
        _vegetationFile = null;
        _structureFile?.Dispose();
        _structureFile = null;
        _biomeCache = null;
        _altitudeCache = null;
        _transitionCache = null;
    }

    #endregion
}
