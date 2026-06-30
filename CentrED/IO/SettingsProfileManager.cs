using System.Reflection;
using System.Text.Json;
using CentrED.Lights;
using CentrED.UI.Windows;
using Hexa.NET.ImGui;
using Vector2 = System.Numerics.Vector2;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;
using static CentrED.Application;

namespace CentrED;


internal sealed class SettingsProfileToast
{
    public string Text = "";
    public bool Success;
    public double StartTime;
    public double Duration;
}


/* SUMMARY 
 Lightweight, file-backed "Settings Profile" system.

 Design goals:
 - All profile logic lives here (UIManager only wires the menu + calls DrawPopups()).
 - Profiles are plain JSON files in ./settings_profiles/.
 - Captures and restores:
   - All OptionsWindow-backed settings (ConfigRoot + lights + virtual-layer/grid colors)
   - View menu toggles
   - Tool/menu window open state via Config.Layout
   - FilterWindow values
   - Tiles/Hues window UI state (list/grid/art/tex, filters, etc.) */

public static class SettingsProfileManager
{
    private const string ProfilesDir = "settings_profiles";
    private const string FileExt = ".json";

    private static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        WriteIndented = true,
    };

    public sealed class SettingsProfile
    {
        public string Name = "Default";

        // Persisted app config (OptionsWindow: general + keymap + layout + misc)
        public ConfigRoot Config = new();

        // View menu options
        public bool WalkableSurfaces;
        public bool FlatView;
        public bool FlatShowHeight;
        public bool AnimateObjects;
        public bool TerrainGrid;
        public bool NoDrawTiles;

        // FilterWindow options
        public int MaxZ;
        public int MinZ;
        public bool GlobalFilterLand;
        public bool GlobalFilterObjects;
        public bool GlobalFilterNoDraw;
        public bool ObjectIdFilterEnabled;
        public bool ObjectIdFilterInclusive;
        public bool ObjectHueFilterEnabled;
        public bool ObjectHueFilterInclusive;

        public List<int>? ObjectIdFilter;
        public List<int>? ObjectHueFilter;

        // Lights tab
        public int GlobalLightLevel = 30;
        public bool ColoredLights = true;
        public bool AltLights;
        public bool DarkNights;
        public bool ShowInvisibleLights;
        public bool ClassicUOTerrainLighting;

        // Virtual Layer tab (captured from OptionsWindow private fields)
        public System.Numerics.Vector4 VirtualLayerFillColor;
        public System.Numerics.Vector4 VirtualLayerBorderColor;

        // Terrain Grid tab (captured from OptionsWindow private fields)
        public System.Numerics.Vector4 TerrainGridFlatColor;
        public System.Numerics.Vector4 TerrainGridAngledColor;

        // TilesWindow UI state
        public string TilesFilterText = "";
        public bool TilesObjectMode;
        public bool TilesGridMode;
        public bool TilesTexMode;
        public bool TilesTiledataFilterEnabled;
        public bool TilesTiledataFilterInclusive = true;
        public bool TilesTiledataFilterMatchAll;
        public ulong TilesTiledataFilterValue;

        // HuesWindow UI state
        public string HuesFilterText = "";
    }

    private static string GetPath(string name)
        => Path.Combine(ProfilesDir, Sanitize(name) + FileExt);

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    public static IEnumerable<string> ListProfiles()
    {
        Directory.CreateDirectory(ProfilesDir);
        return Directory
            .EnumerateFiles(ProfilesDir, "*" + FileExt)
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
    }

    public static bool Exists(string name) => File.Exists(GetPath(name));

    /// <summary>//
    /// Creates a "Default" profile based on code defaults (fresh default-constructed objects),
    /// NOT by capturing current runtime values.
    /// </summary>
    private static SettingsProfile CreateDefaultProfileFromCode()
    {
        return new SettingsProfile
        {
            Name = "Default",
            Config = new ConfigRoot(),

            // These are left as type defaults unless explicitly set in SettingsProfile itself.
            // Lights defaults are already set in-field (GlobalLightLevel=30, ColoredLights=true, etc.)
        };
    }

    private static void EnsureDefaultProfileExists()
    {
        Directory.CreateDirectory(ProfilesDir);
        if (Exists("Default"))
            return;

        var p = CreateDefaultProfileFromCode();
        var path = GetPath("Default");
        File.WriteAllText(path, JsonSerializer.Serialize(p, Json));
    }
    public static void SaveCurrent(string name)
    {
        var path = GetPath(name);
        var existed = File.Exists(path);

        var profile = CaptureCurrent(name);
        Directory.CreateDirectory(ProfilesDir);
        File.WriteAllText(path, JsonSerializer.Serialize(profile, Json));

        var existsNow = File.Exists(path);
        if (existsNow)
        {
            if (existed)
                EnqueueToast(true, $"Profile: {name} has been updated.");
            else
                EnqueueToast(true, $"Profile: {name} has been created.");
        }
        else
        {
            EnqueueToast(false, $"Profile: {name} could not be saved, try again.");
        }

        Config.Instance.ActiveSettingsProfile = name;
        Config.Save();
    }

    public static void Delete(string name)
    {
        var path = GetPath(name);
        var existed = File.Exists(path);
        if (existed)
            File.Delete(path);

        var existsNow = File.Exists(path);
        if (existed && !existsNow)
            EnqueueToast(true, $"Profile: {name} has been deleted.");
        else
            EnqueueToast(false, $"Profile: {name} could not be found, try again.");

        // If you deleted the active profile, fall back to Default.
        if (string.Equals(Config.Instance.ActiveSettingsProfile, name, StringComparison.OrdinalIgnoreCase))
        {
            Config.Instance.ActiveSettingsProfile = "Default";
            Config.Save();
        }
    }

    public static SettingsProfile Load(string name)
    {
        var path = GetPath(name);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Profile '{name}' not found.", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SettingsProfile>(json, Json)
               ?? throw new InvalidOperationException($"Failed to parse settings profile '{name}'.");
    }

    public static void Apply(string name)
    {
        EnsureDefaultProfileExists();
        // Auto-create Default if missing
        if (!Exists(name) && string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
            SaveCurrent("Default");

        try
        {
            var profile = Load(name);
            Apply(profile);

            Config.Instance.ActiveSettingsProfile = name;
            Config.Save();

            EnqueueToast(true, $"Profile: {name} loaded.");
        }
        catch
        {
            EnqueueToast(false, $"Profile: {name} failed to load.");
        }
    }

    public static SettingsProfile CaptureCurrent(string name)
    {
        var map = CEDGame.MapManager;
        var ui = CEDGame.UIManager;

        // Deep copy ConfigRoot (so profile is self-contained)
        var configClone = JsonSerializer.Deserialize<ConfigRoot>(JsonSerializer.Serialize(Config.Instance, Json), Json)
                         ?? new ConfigRoot();

        var p = new SettingsProfile
        {
            Name = name,
            Config = configClone,

            WalkableSurfaces = map.WalkableSurfaces,
            FlatView = map.FlatView,
            FlatShowHeight = map.FlatShowHeight,
            AnimateObjects = map.AnimatedStatics,
            TerrainGrid = map.ShowGrid,
            NoDrawTiles = map.ShowNoDraw,

            MaxZ = map.MaxZ,
            MinZ = map.MinZ,
            GlobalFilterLand = map.ShowLand,
            GlobalFilterObjects = map.ShowStatics,
            GlobalFilterNoDraw = map.ShowNoDraw,
            ObjectIdFilterEnabled = map.ObjectIdFilterEnabled,
            ObjectIdFilterInclusive = map.ObjectIdFilterInclusive,
            ObjectHueFilterEnabled = map.ObjectHueFilterEnabled,
            ObjectHueFilterInclusive = map.ObjectHueFilterInclusive,

            ObjectIdFilter = map.ObjectIdFilter?.ToList(),
            ObjectHueFilter = map.ObjectHueFilter?.ToList(),

            GlobalLightLevel = LightsManager.Instance?.GlobalLightLevel ?? 30,
            ColoredLights = LightsManager.Instance?.ColoredLights ?? true,
            AltLights = LightsManager.Instance?.AltLights ?? false,
            DarkNights = LightsManager.Instance?.DarkNights ?? false,
            ShowInvisibleLights = LightsManager.Instance?.ShowInvisibleLights ?? false,
            ClassicUOTerrainLighting = LightsManager.Instance?.ClassicUONormals ?? false,

            // Capture from OptionsWindow pickers (MapEffect colors are write-only)
            VirtualLayerFillColor = GetOptionsColor("_virtualLayerFillColor"),
            VirtualLayerBorderColor = GetOptionsColor("_virtualLayerBorderColor"),
            TerrainGridFlatColor = GetOptionsColor("_terrainGridFlatColor"),
            TerrainGridAngledColor = GetOptionsColor("_terrainGridAngledColor"),
        };

        // Keep the current runtime font selection inside the config snapshot.
        p.Config.FontSize = ui.FontSize;
        p.Config.FontName = ui.FontNames[ui.FontIndex];

        FillTilesAndHuesState(p);
        return p;
    }

    public static void Apply(SettingsProfile p)
    {
        var map = CEDGame.MapManager;
        var ui = CEDGame.UIManager;

        // --- 1) Restore Config (OptionsWindow: General + Keymap + Layout + misc) ---
        Config.Instance.ServerConfigPath = p.Config.ServerConfigPath;
        Config.Instance.PreferTexMaps = p.Config.PreferTexMaps;
        Config.Instance.ObjectBrightHighlight = p.Config.ObjectBrightHighlight;
        Config.Instance.LegacyMouseScroll = p.Config.LegacyMouseScroll;
        Config.Instance.Viewports = p.Config.Viewports;
        Config.Instance.GraphicsDriver = p.Config.GraphicsDriver;
        Config.Instance.Layout = p.Config.Layout ?? new();
        ApplyLayoutToWindows();
        Config.Instance.Keymap = p.Config.Keymap ?? new();
        Config.Instance.FontSize = p.Config.FontSize;
        Config.Instance.FontName = p.Config.FontName;
        Config.Instance.Language = p.Config.Language;
        Config.Instance.NumberFormat = p.Config.NumberFormat;
        Config.Instance.ImageOverlay = p.Config.ImageOverlay;

        // Apply Viewports flag immediately
        if (Config.Instance.Viewports)
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        else
            ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;

        // Apply language immediately
        var langIndex = Array.IndexOf(LangManager.LangNames, Config.Instance.Language);
        if (langIndex >= 0)
            LangManager.LangIndex = langIndex;

        // Apply font immediately (via UIManager setters)
        var fontIndex = Array.IndexOf(ui.FontNames, Config.Instance.FontName);
        if (fontIndex >= 0)
            ui.FontIndex = fontIndex;
        ui.FontSize = Config.Instance.FontSize;

        // PreferTexMaps impacts tiles
        map.UpdateAllTiles();

        // --- 2) Restore View menu state ---
        map.WalkableSurfaces = p.WalkableSurfaces;
        map.FlatView = p.FlatView;
        map.FlatShowHeight = p.FlatShowHeight;
        map.AnimatedStatics = p.AnimateObjects;
        map.ShowGrid = p.TerrainGrid;
        map.ShowNoDraw = p.NoDrawTiles;
        map.UpdateAllTiles();

        // --- 3) Restore FilterWindow state ---
        map.MaxZ = p.MaxZ;
        map.MinZ = p.MinZ;
        map.ShowLand = p.GlobalFilterLand;
        map.ShowStatics = p.GlobalFilterObjects;
        map.ShowNoDraw = p.GlobalFilterNoDraw;
        map.ObjectIdFilterEnabled = p.ObjectIdFilterEnabled;
        map.ObjectIdFilterInclusive = p.ObjectIdFilterInclusive;
        map.ObjectHueFilterEnabled = p.ObjectHueFilterEnabled;
        map.ObjectHueFilterInclusive = p.ObjectHueFilterInclusive;

        if (p.ObjectIdFilter != null)
            map.ObjectIdFilter = new SortedSet<int>(p.ObjectIdFilter);
        if (p.ObjectHueFilter != null)
            map.ObjectHueFilter = new SortedSet<int>(p.ObjectHueFilter);
        map.UpdateLights();

        // --- 4) Restore Lights ---
        if (LightsManager.Instance != null)
        {
            LightsManager.Instance.GlobalLightLevel = p.GlobalLightLevel;
            LightsManager.Instance.ColoredLights = p.ColoredLights;
            LightsManager.Instance.AltLights = p.AltLights;
            LightsManager.Instance.DarkNights = p.DarkNights;
            LightsManager.Instance.ShowInvisibleLights = p.ShowInvisibleLights;
            LightsManager.Instance.ClassicUONormals = p.ClassicUOTerrainLighting;
            LightsManager.Instance.UpdateGlobalLight();
            map.UpdateLights();
            map.UpdateAllTiles();
        }

        // --- 5) Restore Virtual Layer & Terrain Grid colors ---
        map.MapEffect.VirtualLayerFillColor = ToXna(p.VirtualLayerFillColor);
        map.MapEffect.VirtualLayerBorderColor = ToXna(p.VirtualLayerBorderColor);
        map.MapEffect.TerrainGridFlatColor = ToXna(p.TerrainGridFlatColor);
        map.MapEffect.TerrainGridAngledColor = ToXna(p.TerrainGridAngledColor);

        // Also update OptionsWindow's cached color pickers (private fields)
        SetOptionsColor("_virtualLayerFillColor", p.VirtualLayerFillColor);
        SetOptionsColor("_virtualLayerBorderColor", p.VirtualLayerBorderColor);
        SetOptionsColor("_terrainGridFlatColor", p.TerrainGridFlatColor);
        SetOptionsColor("_terrainGridAngledColor", p.TerrainGridAngledColor);

        // --- 6) Restore Tiles/Hues window UI state ---
        ApplyTilesAndHuesState(p);

        // Persist the config snapshot
        Config.Save();
    }

    // ---- UI: CentrED -> Settings Profile menu ----
    private static string _saveNameBuf = "";
    private static bool _requestOpenSavePopup;

    // --- Delete confirmation popup ---
    private static bool _requestOpenDeleteConfirm;
    private static string _pendingDeleteName = "";
    // --- Non-blocking notifications (toast) ---
    private static bool _hasMenuAnchor;
    private static Vector2 _menuAnchorMin;
    private static Vector2 _menuAnchorMax;

    private static readonly List<SettingsProfileToast> _toastQueue = new();
    private const double ToastDurationSeconds = 10.0;
    private static SettingsProfileToast? _lastToastForMenu;

    private static void EnqueueToast(bool success, string text)
    {
        var t = new SettingsProfileToast
        {
            Text = text,
            Success = success,
            StartTime = ImGui.GetTime(),
            Duration = ToastDurationSeconds
        };

        _toastQueue.Add(t);
        _lastToastForMenu = t;
    }

    /// <summary>
    /// Call from the "Settings Profile" menu (inside menu scope).
    /// </summary>
    public static void DrawMenu()
    {
        foreach (var name in ListProfiles())
        {
            if (ImGui.MenuItem(name, "", string.Equals(Config.Instance.ActiveSettingsProfile, name, StringComparison.OrdinalIgnoreCase)))
            {
                Apply(name);
            }
        }

        ImGui.Separator();

        if (ImGui.MenuItem("Save..."))
        {
            // Pre-fill with current active name (but user can type a new one).
            _saveNameBuf = Config.Instance.ActiveSettingsProfile ?? "";
            _requestOpenSavePopup = true;
        }

        if (ImGui.BeginMenu("Delete"))
        {
            foreach (var name in ListProfiles())
            {
                if (string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ImGui.MenuItem(name))
                {
                    _pendingDeleteName = name;
                    _requestOpenDeleteConfirm = true;
                }
            }
            ImGui.EndMenu();
        }
    }

    /// <summary>
    /// Draw the Settings Profile selector directly on the main menu bar as:
    /// "Settings Profile: {ActiveProfile}".
    /// Clicking it opens the same profile menu (list/apply + save/delete) as before.
    /// </summary>
    public static void DrawTopBarEntry()
    {
        var profileName = Config.Instance.ActiveSettingsProfile ?? "Default";

        // Clickable menu item is just "Settings Profile",
        // and we render ": <profile>" next to it with the profile name tinted bright green.
        bool open = ImGui.BeginMenu("Profile");

        // Anchor toasts to this menu item
        _menuAnchorMin = ImGui.GetItemRectMin();
        _menuAnchorMax = ImGui.GetItemRectMax();
        _hasMenuAnchor = true;

        // Render the suffix text on the menu bar
        ImGui.SameLine(0, 0);
        ImGui.TextUnformatted(":");
        ImGui.SameLine(0, 6f);

        ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f));
        ImGui.TextUnformatted(profileName);
        ImGui.PopStyleColor();

        if (open)
        {
            DrawMenu();
            ImGui.EndMenu();
        }
    }

    /// <summary>
    /// Call once per frame inside the main menu bar (outside any menu scope),
    /// to render popups + toasts.
    /// </summary>
    public static void DrawMenuBarPopups()
    {
        DrawPopups();
    }

    private static void DrawToasts()
    {
        if (_toastQueue.Count == 0)
            return;

        var now = ImGui.GetTime();
        for (int i = _toastQueue.Count - 1; i >= 0; i--)
        {
            var t = _toastQueue[i];
            if (now - t.StartTime >= t.Duration)
                _toastQueue.RemoveAt(i);
        }

        if (_toastQueue.Count == 0)
            return;

        var vp = ImGui.GetMainViewport();
        var padding = new System.Numerics.Vector2(12, 12);
        float yOffset = 0f;

        for (int i = _toastQueue.Count - 1; i >= 0; i--)
        {
            var t = _toastQueue[i];
            var age = now - t.StartTime;
            var remaining = Math.Max(0.0, t.Duration - age);

            float alpha = 1f;
            const double fadeWindow = 2.0;
            if (remaining < fadeWindow)
                alpha = (float)(remaining / fadeWindow);

            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, alpha);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(10, 8));

            var bg = t.Success
                ? new System.Numerics.Vector4(0.12f, 0.35f, 0.16f, 0.92f)
                : new System.Numerics.Vector4(0.42f, 0.12f, 0.12f, 0.92f);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, bg);
            ImGui.PushStyleColor(ImGuiCol.Border, new System.Numerics.Vector4(1, 1, 1, 0.15f));

            var textSize = ImGui.CalcTextSize(t.Text);
            var baseX = vp.WorkPos.X + vp.WorkSize.X - textSize.X - padding.X;
            var baseY = vp.WorkPos.Y + padding.Y;

            if (_hasMenuAnchor)
            {
                baseX = _menuAnchorMax.X;
                baseY = _menuAnchorMax.Y + 2f;
            }

            // Clamp within viewport
            var desiredX = baseX;
            var desiredY = baseY + yOffset;

            var w = textSize.X + 16;
            var maxX = vp.WorkPos.X + vp.WorkSize.X - w - padding.X;
            if (desiredX > maxX)
                desiredX = maxX;

            var pos = new System.Numerics.Vector2(desiredX, desiredY);

            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(textSize.X + 16, 0), ImGuiCond.Always);

            ImGui.Begin($"##SettingsProfileToast{i}", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
                                                ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing |
                                                ImGuiWindowFlags.NoInputs);

            ImGui.TextUnformatted(t.Text);
            ImGui.End();

            yOffset += textSize.Y + 10;

            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(3);
        }
    }

    /// <summary>
    /// Must be called once per frame, OUTSIDE the menu scope (UIManager already does this).
    /// This is required for reliable modal popup opening across backends.
    /// </summary>
    public static void DrawPopups()
    {
        if (_requestOpenSavePopup)
        {
            ImGui.OpenPopup("SaveSettingsProfile");
            _requestOpenSavePopup = false;
        }

        if (_requestOpenDeleteConfirm)
        {
            ImGui.OpenPopup("ConfirmDeleteSettingsProfile");
            _requestOpenDeleteConfirm = false;
        }

        bool open = true;
        if (ImGui.BeginPopupModal("SaveSettingsProfile", ref open,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.Text("Profile name:");
            ImGui.SameLine();
            ImGui.InputText("##SettingsProfileName", ref _saveNameBuf, 64);

            var name = _saveNameBuf.Trim();
            var disabled = string.IsNullOrWhiteSpace(name);
            if (disabled) ImGui.BeginDisabled();
            if (ImGui.Button("Save"))
            {
                SaveCurrent(name);
                ImGui.CloseCurrentPopup();
            }
            if (disabled) ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        bool delOpen = true;
        if (ImGui.BeginPopupModal("ConfirmDeleteSettingsProfile", ref delOpen,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            var name = _pendingDeleteName?.Trim() ?? "";
            ImGui.Text($"Are you sure you wish to DELETE profile '{name}'?");
            ImGui.Separator();

            if (ImGui.Button("Yes"))
            {
                if (!string.IsNullOrWhiteSpace(name))
                    Delete(name);
                _pendingDeleteName = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                if (!string.IsNullOrWhiteSpace(name))
                    EnqueueToast(true, $"You decide against deleting {name} profile.");
                _pendingDeleteName = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }


        // Render non-blocking notifications
        DrawToasts();
    }

    // ---- Tiles/Hues window state capture/apply (reflection, no edits required) ----
    private static readonly BindingFlags _rf = BindingFlags.Instance | BindingFlags.NonPublic;

    private static void FillTilesAndHuesState(SettingsProfile p)
    {
        try
        {
            var ui = CEDGame?.UIManager;
            if (ui == null) return;

            var tiles = ui.GetWindow<TilesWindow>();
            if (tiles != null)
            {
                p.TilesFilterText = GetField<string>(tiles, "_filterText") ?? "";
                p.TilesObjectMode = GetField<bool>(tiles, "_objectMode");
                p.TilesGridMode = GetField<bool>(tiles, "_gridMode");
                p.TilesTexMode = GetField<bool>(tiles, "_texMode");

                p.TilesTiledataFilterEnabled = GetField<bool>(tiles, "_tiledataFilterEnabled");
                p.TilesTiledataFilterInclusive = GetField<bool>(tiles, "_tiledataFilterInclusive");
                p.TilesTiledataFilterMatchAll = GetField<bool>(tiles, "_tiledataFilterMatchAll");
                p.TilesTiledataFilterValue = GetField<ulong>(tiles, "_tiledataFilterValue");
            }

            var hues = ui.GetWindow<HuesWindow>();
            if (hues != null)
            {
                p.HuesFilterText = GetField<string>(hues, "_filter") ?? "";
            }
        }
        catch
        {
            // non-fatal
        }
    }

    private static void ApplyTilesAndHuesState(SettingsProfile p)
    {
        try
        {
            var ui = CEDGame?.UIManager;
            if (ui == null) return;

            var tiles = ui.GetWindow<TilesWindow>();
            if (tiles != null)
            {
                SetField(tiles, "_filterText", p.TilesFilterText ?? "");
                SetField(tiles, "_objectMode", p.TilesObjectMode);
                SetField(tiles, "_gridMode", p.TilesGridMode);
                SetField(tiles, "_texMode", p.TilesTexMode);

                SetField(tiles, "_tiledataFilterEnabled", p.TilesTiledataFilterEnabled);
                SetField(tiles, "_tiledataFilterInclusive", p.TilesTiledataFilterInclusive);
                SetField(tiles, "_tiledataFilterMatchAll", p.TilesTiledataFilterMatchAll);
                SetField(tiles, "_tiledataFilterValue", p.TilesTiledataFilterValue);

                InvokeMethod(tiles, "FilterTiles");
                SetField(tiles, "_updateScroll", true);
            }

            var hues = ui.GetWindow<HuesWindow>();
            if (hues != null)
            {
                SetField(hues, "_filter", p.HuesFilterText ?? "");
                InvokeMethod(hues, "FilterHues");
                hues.UpdateScroll = true;
            }
        }
        catch
        {
            // non-fatal
        }
    }

    private static T? GetField<T>(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, _rf);
        if (f == null) return default;
        var v = f.GetValue(obj);
        return v is T t ? t : default;
    }

    private static void SetField<T>(object obj, string fieldName, T value)
    {
        var f = obj.GetType().GetField(fieldName, _rf);
        f?.SetValue(obj, value);
    }

    private static void InvokeMethod(object obj, string methodName)
    {
        var m = obj.GetType().GetMethod(methodName, _rf);
        m?.Invoke(obj, null);
    }

    // OptionsWindow color picker accessors (reflection)
    private static OptionsWindow? GetOptionsWindow()
    {
        try
        {
            return CEDGame?.UIManager?.GetWindow<OptionsWindow>();
        }
        catch
        {
            return null;
        }
    }

    private static System.Numerics.Vector4 GetOptionsColor(string fieldName)
    {
        var ow = GetOptionsWindow();
        if (ow == null)
            return default;

        var f = ow.GetType().GetField(fieldName, _rf);
        if (f?.GetValue(ow) is System.Numerics.Vector4 v)
            return v;

        return default;
    }

    private static void SetOptionsColor(string fieldName, System.Numerics.Vector4 value)
    {
        var ow = GetOptionsWindow();
        if (ow == null)
            return;

        var f = ow.GetType().GetField(fieldName, _rf);
        f?.SetValue(ow, value);
    }

    // Apply window open/close state from Config.Layout (Tools menu, etc.)
    private static void ApplyLayoutToWindows()
    {
        try
        {
            var ui = CEDGame?.UIManager;
            if (ui == null) return;

            var layout = Config.Instance.Layout;
            if (layout == null || layout.Count == 0) return;

            // UIManager.AllWindows is internal; use reflection to iterate all window instances.
            var allWindowsField = ui.GetType().GetField("AllWindows", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (allWindowsField?.GetValue(ui) is not System.Collections.IDictionary allWindows)
                return;

            foreach (System.Collections.DictionaryEntry kv in allWindows)
            {
                var win = kv.Value;
                if (win == null) continue;

                // Layout key is typically the visible name before any ### suffix
                var nameProp = win.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                var name = nameProp?.GetValue(win) as string;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var key = name!;
                var hash = key.IndexOf("###", StringComparison.Ordinal);
                if (hash >= 0)
                    key = key.Substring(0, hash);

                if (!layout.TryGetValue(key, out var savedState) || savedState == null)
                    continue;

                var savedType = savedState.GetType();

                // Prefer writable State property
                var stateProp = win.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (stateProp != null && stateProp.CanWrite && stateProp.PropertyType == savedType)
                {
                    stateProp.SetValue(win, savedState);
                    continue;
                }

                // Fall back to backing field patterns
                var stateField = win.GetType().GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)
                               ?? win.GetType().GetField("State", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (stateField != null && stateField.FieldType == savedType)
                {
                    stateField.SetValue(win, savedState);
                }
            }
        }
        catch
        {
            // non-fatal
        }
    }

    private static XnaVector4 ToXna(System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
}