using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;

namespace CentrED.IO;

public class KeyAction
{
    public static KeyAction MoveUp = new("move_up", [Keys.W], [Keys.Up]);
    public static KeyAction MoveDown = new("move_down", [Keys.S], [Keys.Down]);
    public static KeyAction MoveLeft = new("move_left", [Keys.A], [Keys.Left]);
    public static KeyAction MoveRight = new("move_right", [Keys.D], [Keys.Right]);
    
    public static KeyAction ToggleAnimatedStatics = new("toggle_animated_statics", [Keys.LeftControl, Keys.A], []);
    public static KeyAction Minimap = new("minimap", [Keys.M], []);
    
    public readonly string Id;
    public readonly ConfigValue<Keys[]> Primary;
    public readonly ConfigValue<Keys[]> Secondary;
    
    private KeyAction(string id, Keys[] defaultPrimary, Keys[] defaultSecondary)
    {
        Id = id;
        Primary = App.Config.Init($"keymap.{id}.primary", defaultPrimary);
        Secondary = App.Config.Init($"keymap.{id}.secondary", defaultSecondary);
    }
    
    public string PrettyName => string.Join(' ', Id.Split('_').Select(s => char.ToUpper(s[0]) + s[1..]));
    
    public string Shortcut => string.Join('+', Primary.Value);
}