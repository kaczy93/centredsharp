using System.Numerics;
using System.Text.Json;
using CentrED.UI;

namespace CentrED.IO;

internal static class WorkspaceMigration
{
    private const string LEGACY_DIR = "settings_profiles";
    private const string BACKUP_DIR = "settings_profiles.bak";
    private const string LEGACY_LAYOUT = "imgui.ini";

    private static readonly JsonSerializerOptions JsonOptions = new() { IncludeFields = true };

    private class WorkspaceLegacy
    {
        public ConfigRootLegacy Config { get; set; } = new();
        public bool WalkableSurfaces { get; set; }
        public bool FlatView { get; set; }
        public bool FlatShowHeight { get; set; }
        public bool AnimateObjects { get; set; }
        public bool TerrainGrid { get; set; }
        public int MaxZ { get; set; }
        public int MinZ { get; set; }
        public bool GlobalFilterLand { get; set; }
        public bool GlobalFilterObjects { get; set; }
        public bool GlobalFilterNoDraw { get; set; }
        public bool ObjectIdFilterEnabled { get; set; }
        public bool ObjectIdFilterInclusive { get; set; }
        public bool ObjectHueFilterEnabled { get; set; }
        public bool ObjectHueFilterInclusive { get; set; }
        public List<int>? ObjectIdFilter { get; set; }
        public List<int>? ObjectHueFilter { get; set; }
        public int GlobalLightLevel { get; set; } = 30;
        public bool ColoredLights { get; set; } = true;
        public bool AltLights { get; set; }
        public bool DarkNights { get; set; }
        public bool ShowInvisibleLights { get; set; }
        public bool ClassicUOTerrainLighting { get; set; }
        public Vector4 VirtualLayerFillColor { get; set; }
        public Vector4 VirtualLayerBorderColor { get; set; }
        public Vector4 TerrainGridFlatColor { get; set; }
        public Vector4 TerrainGridAngledColor { get; set; }
        public string TilesFilterText { get; set; } = "";
        public bool TilesObjectMode { get; set; }
        public bool TilesGridMode { get; set; }
        public bool TilesTexMode { get; set; }
        public bool TilesTiledataFilterEnabled { get; set; }
        public bool TilesTiledataFilterInclusive { get; set; } = true;
        public bool TilesTiledataFilterMatchAll { get; set; }
        public ulong TilesTiledataFilterValue { get; set; }
        public string HuesFilterText { get; set; } = "";

        public bool IsCodeDefault => MinZ == 0 && MaxZ == 0 && !GlobalFilterLand && !GlobalFilterObjects;
    }

    public static void MigrateLegacy(NamedConfigManager workspaces)
    {
        var legacy = ConfigLegacy.Instance;
        if (legacy != null && workspaces.ActiveName.IsDefault && !string.IsNullOrEmpty(legacy.ActiveSettingsProfile))
            workspaces.ActiveName.Value = NamedConfigManager.Sanitize(legacy.ActiveSettingsProfile);

        if (Directory.Exists(LEGACY_DIR))
        {
            foreach (var path in Directory.EnumerateFiles(LEGACY_DIR, "*.json"))
            {
                try
                {
                    var ws = JsonSerializer.Deserialize<WorkspaceLegacy>(File.ReadAllText(path), JsonOptions);
                    if (ws == null)
                        continue;
                    var cfg = new Config(workspaces.PathOf(Path.GetFileNameWithoutExtension(path)));
                    if (!ws.IsCodeDefault)
                        Migrate(cfg, ws);
                    cfg.Save();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[WORKSPACE MIGRATE] failed to migrate {path}: {e}");
                }
            }
            BackupLegacy();
        }

        var layout = workspaces.PathOf(workspaces.ActiveName.Value, UIManager.LAYOUT_EXT);
        if (File.Exists(LEGACY_LAYOUT) && !File.Exists(layout))
            File.Move(LEGACY_LAYOUT, layout);

        if (legacy == null)
            return;

        var active = new Config(workspaces.PathOf(workspaces.ActiveName.Value));
        MigrateGlobal(active, legacy);
        if (ProfileMigration.ActiveStaticFilter is { Count: > 0 } staticFilter)
            Set(active, "filter.object_id.values", new SortedSet<int>(staticFilter));
        active.Save();
    }

    private static void Migrate(Config cfg, WorkspaceLegacy ws)
    {
        MigrateGlobal(cfg, ws.Config);

        Set(cfg, "view.walkable_surfaces", ws.WalkableSurfaces);
        Set(cfg, "view.flat", ws.FlatView);
        Set(cfg, "view.flat_show_height", ws.FlatShowHeight);
        Set(cfg, "view.animated_statics", ws.AnimateObjects);
        Set(cfg, "view.terrain_grid", ws.TerrainGrid);

        if (ws.MinZ < ws.MaxZ)
        {
            Set(cfg, "filter.min_z", ws.MinZ);
            Set(cfg, "filter.max_z", ws.MaxZ);
        }
        Set(cfg, "filter.show_land", ws.GlobalFilterLand);
        Set(cfg, "filter.show_statics", ws.GlobalFilterObjects);
        Set(cfg, "filter.show_nodraw", ws.GlobalFilterNoDraw);
        Set(cfg, "filter.object_id.enabled", ws.ObjectIdFilterEnabled);
        Set(cfg, "filter.object_id.inclusive", ws.ObjectIdFilterInclusive);
        Set(cfg, "filter.object_id.values", new SortedSet<int>(ws.ObjectIdFilter ?? []));
        Set(cfg, "filter.object_hue.enabled", ws.ObjectHueFilterEnabled);
        Set(cfg, "filter.object_hue.inclusive", ws.ObjectHueFilterInclusive);
        Set(cfg, "filter.object_hue.values", new SortedSet<int>(ws.ObjectHueFilter ?? []));

        Set(cfg, "lights.global_level", ws.GlobalLightLevel);
        Set(cfg, "lights.colored", ws.ColoredLights);
        Set(cfg, "lights.alternative", ws.AltLights);
        Set(cfg, "lights.dark_nights", ws.DarkNights);
        Set(cfg, "lights.show_invisible", ws.ShowInvisibleLights);
        Set(cfg, "lights.classicuo_normals", ws.ClassicUOTerrainLighting);

        SetColor(cfg, "virtual_layer.fill_color", ws.VirtualLayerFillColor);
        SetColor(cfg, "virtual_layer.border_color", ws.VirtualLayerBorderColor);
        SetColor(cfg, "terrain_grid.flat_color", ws.TerrainGridFlatColor);
        SetColor(cfg, "terrain_grid.angled_color", ws.TerrainGridAngledColor);

        Set(cfg, "tiles.filter_text", ws.TilesFilterText ?? "");
        Set(cfg, "tiles.object_mode", ws.TilesObjectMode);
        Set(cfg, "tiles.grid_mode", ws.TilesGridMode);
        Set(cfg, "tiles.tex_mode", ws.TilesTexMode);
        Set(cfg, "tiles.tiledata_filter.enabled", ws.TilesTiledataFilterEnabled);
        Set(cfg, "tiles.tiledata_filter.inclusive", ws.TilesTiledataFilterInclusive);
        Set(cfg, "tiles.tiledata_filter.match_all", ws.TilesTiledataFilterMatchAll);
        Set(cfg, "tiles.tiledata_filter.value", ws.TilesTiledataFilterValue);

        Set(cfg, "hues.filter_text", ws.HuesFilterText ?? "");
    }

    private static void MigrateGlobal(Config cfg, ConfigRootLegacy? legacy)
    {
        if (legacy == null)
            return;
        Set(cfg, "view.prefer_tex_maps", legacy.PreferTexMaps);
        Set(cfg, "view.bright_highlight", legacy.ObjectBrightHighlight);

        var overlay = legacy.ImageOverlay ?? new();
        Set(cfg, "image_overlay.enabled", overlay.Enabled);
        Set(cfg, "image_overlay.file", overlay.ImagePath ?? "");
        Set(cfg, "image_overlay.draw_above_terrain", overlay.DrawAboveTerrain);
        Set(cfg, "image_overlay.world_x", overlay.WorldX);
        Set(cfg, "image_overlay.world_y", overlay.WorldY);
        Set(cfg, "image_overlay.scale", overlay.Scale);
        Set(cfg, "image_overlay.opacity", overlay.Opacity);
        Set(cfg, "image_overlay.screen", overlay.Screen);
    }

    private static void Set<T>(Config cfg, string key, T value) where T : notnull
    {
        cfg.Init(key, value).Value = value;
    }

    private static void SetColor(Config cfg, string key, Vector4 value)
    {
        if (value != Vector4.Zero)
            Set(cfg, key, value);
    }

    private static void BackupLegacy()
    {
        // TODO: delete instead of backup in 7.1
        if (Directory.Exists(BACKUP_DIR))
            Directory.Delete(BACKUP_DIR, true);
        Directory.Move(LEGACY_DIR, BACKUP_DIR);
    }
}
