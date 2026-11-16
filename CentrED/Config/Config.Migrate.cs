using Microsoft.Xna.Framework.Input;

namespace CentrED;

public partial class Config
{
    public void MigrateGlobalConfig()
    {
        ConfigLegacy.Initialize();
        var legacy = ConfigLegacy.Instance;
        if (legacy == null)
            return;

        if(legacy.GraphicsDriver != "Auto")
            _rawEntries.TryAdd("graphics.driver", legacy.GraphicsDriver);
        
        _rawEntries.TryAdd("server.config_file", legacy.ServerConfigPath);
        _rawEntries.TryAdd("input.legacy_mouse_scroll", legacy.LegacyMouseScroll.ToString());
        _rawEntries.TryAdd("ui.viewports", legacy.Viewports.ToString());
        _rawEntries.TryAdd("ui.font_size", legacy.FontSize.ToString());
        _rawEntries.TryAdd("ui.font_name", legacy.FontName);
        _rawEntries.TryAdd("lang.name", legacy.Language);
        _rawEntries.TryAdd("ui.number_display_format", legacy.NumberFormat.ToString());
        
        foreach (var (windowId, state) in legacy.Layout)
        {
            _rawEntries.TryAdd(SanitizeKey($"layout.{windowId}.open"), state.IsOpen.ToString());
        }
        
        foreach (var (action, (primary, secondary)) in legacy.Keymap ?? new())
        {
            if (!IsValidKey(action))
            {
                Console.WriteLine($"[CONFIG MIGRATE] invalid action: {action}");
                continue;
            }
            _rawEntries.TryAdd($"keymap.{action}.primary", ConfigConverters.Get<Keys[]>().Format(primary));
            _rawEntries.TryAdd($"keymap.{action}.secondary",  ConfigConverters.Get<Keys[]>().Format(secondary));
        }
        
        Save();
        // ConfigLegacy.Purge();
    }
}
