using CentrED.UI.Windows;
using static CentrED.CentrED;

namespace CentrED;

public partial class Config
{
    public void MigrateGlobalConfig()
    {
        // throw new NotImplementedException("TODO");
        var legacy = ConfigLegacy.Instance;
        if (legacy == null)
            return;
        
        // Set("workspace.active", legacy.ActiveSettingsProfile ?? "Default");
        SetIfDefault(App.UIManager.GetWindow<ServerWindow>().ConfigPath, legacy.ServerConfigPath);
        // SetIfDefault("view.prefer_tex_maps", legacy.PreferTexMaps);
        // SetIfDefault("view.object_bright_highlight", legacy.ObjectBrightHighlight);
        // SetIfDefault("input.legacy_mouse_scroll", legacy.LegacyMouseScroll);
        // SetIfDefault("ui.viewports", legacy.Viewports);
        // SetIfDefault("graphics.driver", legacy.GraphicsDriver ?? "Auto");
        // SetIfDefault("ui.font_size", legacy.FontSize);
        // SetIfDefault("ui.font_name", legacy.FontName ?? "ProggyClean.ttf");
        // SetIfDefault("ui.language", legacy.Language ?? "English");
        // SetIfDefault("ui.number_format", legacy.NumberFormat);
        //
        // foreach (var (name, windowState) in legacy.Layout)
        // {
        //     SetIfDefault($"layout.{SanitizeKey(name)}.open", windowState.IsOpen);
        // }
        //
        // foreach (var (action, (primary, secondary)) in legacy.Keymap ?? new())
        // {
        //     if (!IsValidKey(action))
        //     {
        //         Console.WriteLine($"[CONFIG MIGRATE] invalid action: {action}");
        //         continue;
        //     }
        //     SetIfDefault($"keymap.{action}.primary", primary ?? Keymap.NotAssigned);
        //     SetIfDefault($"keymap.{action}.secondary", secondary ?? Keymap.NotAssigned);
        // }
        //
        // var overlay = legacy.ImageOverlay ?? new();
        // SetIfDefault("image_overlay.path", overlay.ImagePath ?? "");
        // SetIfDefault("image_overlay.enabled", overlay.Enabled);
        // SetIfDefault("image_overlay.draw_above_terrain", overlay.DrawAboveTerrain);
        // SetIfDefault("image_overlay.world_x", overlay.WorldX);
        // SetIfDefault("image_overlay.world_y", overlay.WorldY);
        // SetIfDefault("image_overlay.scale", overlay.Scale);
        // SetIfDefault("image_overlay.opacity", overlay.Opacity);
        // SetIfDefault("image_overlay.screen", overlay.Screen);

        Save();
        // ConfigLegacy.Purge();
    }

    private void SetIfDefault<T>(ConfigValue<T> conf, T value)
    {
        if (conf.IsDefault)
            conf.Value = value;
    }
}
