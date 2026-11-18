using Microsoft.Xna.Framework.Input;

namespace CentrED.IO;

public class KeyAction
{
    private readonly Keymap _keymap;

    public readonly string Id;
    public readonly ConfigValue<Keys[]> Primary;
    public readonly ConfigValue<Keys[]> Secondary;

    internal KeyAction(Keymap keymap, Config config, string id, Keys[] defaultPrimary, Keys[] defaultSecondary)
    {
        _keymap = keymap;
        Id = id;
        Primary = config.Init($"keymap.{id}.primary", defaultPrimary);
        Secondary = config.Init($"keymap.{id}.secondary", defaultSecondary);
    }

    public string PrettyName => string.Join(' ', Id.Split('_').Select(s => char.ToUpper(s[0]) + s[1..]));

    public string Shortcut => string.Join('+', Primary.Value);

    public bool IsDown() => IsDown(Primary.Value) || IsDown(Secondary.Value);

    public bool IsPressed() => IsPressed(Primary.Value) || IsPressed(Secondary.Value);

    private bool IsDown(Keys[] keys) => keys.Length > 0 && keys.All(_keymap.IsKeyDown);

    private bool IsPressed(Keys[] keys) => IsDown(keys) && keys.Any(_keymap.WasKeyUp);
}
