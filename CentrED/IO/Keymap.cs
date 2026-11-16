using CentrED.IO;
using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;

namespace CentrED;

public class Keymap
{
    private static readonly Keys[] FunctionKeys =
        [Keys.LeftControl, Keys.LeftAlt, Keys.LeftShift, Keys.RightControl, Keys.RightAlt, Keys.RightShift];
    
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

    public Keys[] GetKeysDown() => currentState.GetPressedKeys();

    public IEnumerable<Keys> GetKeysPressed() => currentState.GetPressedKeys().Except(previousState.GetPressedKeys());

    public IEnumerable<Keys> GetKeysReleased() => previousState.GetPressedKeys().Except(currentState.GetPressedKeys());

    public bool IsDown(KeyAction action) => action.Primary.Value.Length > 0 && action.Primary.Value.All(IsKeyDown) || 
                                            action.Secondary.Value.Length > 0 && action.Secondary.Value.All(IsKeyDown);
    
    public bool IsUp(KeyAction action)
    {
        return action.Primary.Value.All(IsKeyUp) || action.Secondary.Value.All(IsKeyUp);
    }

    public bool IsPressed(KeyAction action)
    {
        return (action.Primary.Value.All(IsKeyDown) && action.Primary.Value.Any(previousState.IsKeyUp)) ||
            (action.Secondary.Value.All(IsKeyDown) && action.Secondary.Value.Any(previousState.IsKeyUp));
    }

    public class LetterLastComparer : IComparer<Keys>
    {
        public int Compare(Keys k1, Keys k2)
        {
            if (k1 is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
            {
                return 1;
            }
            if (k2 is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9)
            {
                return -1;
            }
            return 0;
        }
    }
}