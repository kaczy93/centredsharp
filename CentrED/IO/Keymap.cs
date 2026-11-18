using Microsoft.Xna.Framework.Input;

namespace CentrED.IO;

public partial class Keymap
{
    private static readonly Keys[] ModifierKeys =
        [Keys.LeftControl, Keys.LeftAlt, Keys.LeftShift, Keys.RightControl, Keys.RightAlt, Keys.RightShift];

    public static bool IsModifier(Keys key) => ModifierKeys.Contains(key);
    
    private KeyboardState currentState;
    private KeyboardState previousState;
    
    public void Update(KeyboardState newState)
    {
        previousState = currentState;
        currentState = newState;
    }

    public bool IsKeyDown(Keys key) => currentState.IsKeyDown(key);

    public bool IsKeyUp(Keys key) => currentState.IsKeyUp(key);

    public bool IsKeyPressed(Keys key) => currentState.IsKeyDown(key) && previousState.IsKeyUp(key);

    public bool IsKeyReleased(Keys key) => currentState.IsKeyUp(key) && previousState.IsKeyDown(key);

    internal bool WasKeyUp(Keys key) => previousState.IsKeyUp(key);

    public Keys[] GetKeysDown() => currentState.GetPressedKeys();

    public IEnumerable<Keys> GetKeysPressed() => currentState.GetPressedKeys().Except(previousState.GetPressedKeys());

    public IEnumerable<Keys> GetKeysReleased() => previousState.GetPressedKeys().Except(currentState.GetPressedKeys());
}
