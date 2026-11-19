using Microsoft.Xna.Framework.Input;

namespace CentrED.IO;

public partial class Keymap
{
    public readonly KeyAction MoveUp;
    public readonly KeyAction MoveDown;
    public readonly KeyAction MoveLeft;
    public readonly KeyAction MoveRight;
    public readonly KeyAction ToggleAnimatedStatics;
    public readonly KeyAction Minimap;

    public Keymap(Config config)
    {
        MoveUp = new KeyAction(this, config, "move_up", [Keys.W], [Keys.Up]);
        MoveDown = new KeyAction(this, config, "move_down", [Keys.S], [Keys.Down]);
        MoveLeft = new KeyAction(this, config, "move_left", [Keys.A], [Keys.Left]);
        MoveRight = new KeyAction(this, config, "move_right", [Keys.D], [Keys.Right]);
        ToggleAnimatedStatics = new KeyAction(this, config, "toggle_animated_statics", [Keys.LeftControl, Keys.A], []);
        Minimap = new KeyAction(this, config, "minimap", [Keys.M], []);
    }
}