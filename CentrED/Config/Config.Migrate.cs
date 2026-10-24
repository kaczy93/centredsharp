using static CentrED.CentrED;

namespace CentrED;

public partial class Config
{
    public void MigrateGlobalConfig()
    {
        throw new NotImplementedException("TODO");
        var legacy = ConfigLegacy.Instance;
        SetIfDefault(App.Profile.ActiveName, legacy.ActiveProfile);
        Set("workspace.active", legacy.ActiveSettingsProfile ?? "Default");
        Set("server.config_file", legacy.ServerConfigPath ?? "cedserver.xml");
        Set("view.prefer_tex_maps", legacy.PreferTexMaps);
        Set("view.object_bright_highlight", legacy.ObjectBrightHighlight);
        Set("input.legacy_mouse_scroll", legacy.LegacyMouseScroll);
        Set("ui.viewports", legacy.Viewports);
        Set("graphics.driver", legacy.GraphicsDriver ?? "Auto");
        Set("ui.font_size", legacy.FontSize);
        Set("ui.font_name", legacy.FontName ?? "ProggyClean.ttf");
        Set("ui.language", legacy.Language ?? "English");
        Set("ui.number_format", legacy.NumberFormat);
        
        foreach (var (name, windowState) in legacy.Layout)
        {
            Set($"layout.{SanitizeKey(name)}.open", windowState.IsOpen);
        }

        foreach (var (action, (primary, secondary)) in legacy.Keymap ?? new())
        {
            if (!IsValidKey(action))
            {
                Console.WriteLine($"[CONFIG MIGRATE] invalid action: {action}");
                continue;
            }
            Set($"keymap.{action}.primary", primary ?? Keymap.NotAssigned);
            Set($"keymap.{action}.secondary", secondary ?? Keymap.NotAssigned);
        }

        var overlay = legacy.ImageOverlay ?? new();
        Set("image_overlay.path", overlay.ImagePath ?? "");
        Set("image_overlay.enabled", overlay.Enabled);
        Set("image_overlay.draw_above_terrain", overlay.DrawAboveTerrain);
        Set("image_overlay.world_x", overlay.WorldX);
        Set("image_overlay.world_y", overlay.WorldY);
        Set("image_overlay.scale", overlay.Scale);
        Set("image_overlay.opacity", overlay.Opacity);
        Set("image_overlay.screen", overlay.Screen);

        Save();
        // ConfigLegacy.Purge();
    }

    private void SetIfDefault<T>(ConfigValue<T> conf, T value)
    {
        if (conf.IsDefault)
            conf.Value = value;
    }
}
