using Microsoft.Xna.Framework.Input;

namespace CentrED;

public static class Keymap
{
    private static KeyboardState currentState;
    private static KeyboardState previousState;

    public static readonly Keys[] NotAssigned = Array.Empty<Keys>();

    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string ToggleAnimatedStatics = "toggle_animated_statics";
    public const string Minimap = "minimap";

    public static void Update(KeyboardState newState)
    {
        previousState = currentState;
        currentState = newState;
    }
    
    public static bool IsKeyDown(string action)
    {
        var assignedKeys = GetKeys(action);
        return assignedKeys.Item1.All(currentState.IsKeyDown) || assignedKeys.Item2.All(currentState.IsKeyDown);
    }
    
    public static bool IsKeyUp(Keys key)
    {
        return currentState.IsKeyUp(key);
    }

    public static bool IsKeyUp(string action)
    {
        var assignedKeys = GetKeys(action);
        return assignedKeys.Item1.All(currentState.IsKeyUp) || assignedKeys.Item2.All(currentState.IsKeyUp);
    }

    public static bool IsKeyPressed(string action)
    {
        var assignedKeys = GetKeys(action);
        return (assignedKeys.Item1.All(currentState.IsKeyDown) && assignedKeys.Item1.Any(previousState.IsKeyUp)) ||
            (assignedKeys.Item2.All(currentState.IsKeyDown) && assignedKeys.Item2.Any(previousState.IsKeyUp));
    }

    public static (Keys[], Keys[]) GetKeys(string action)
    {
        InitAction(action);
        return Config.Instance.Keymap[action];
    }

    public static string GetShortcut(string action)
    {
        return string.Join('+', GetKeys(action).Item1);
    }

    public static string PrettyName(string action)
    {
        return string.Join(' ', action.Split('_').Select(s => char.ToUpper(s[0]) + s[1..]));
    }

    public static Keys[] GetKeysPressed()
    {
        return currentState.GetPressedKeys();
    }
    
    public static Keys AnyKeyPressed()
    {
        return currentState.GetPressedKeys().Except(previousState.GetPressedKeys()).FirstOrDefault();
    }
    
    public static Keys AnyKeyReleased()
    {
        return previousState.GetPressedKeys().Except(currentState.GetPressedKeys()).FirstOrDefault();
    }

    private static void InitAction(string action)
    {
        if (Config.Instance.Keymap.ContainsKey(action))
        {
            return;
        }
        var defaultKey = GetDefault(action);
        if (defaultKey != (NotAssigned, NotAssigned))
        {
            Config.Instance.Keymap[action] = defaultKey;
        }
        Config.Save();
    }
    
    private static (Keys[],Keys[]) GetDefault(string action)
    {
        return action switch
        {
            MoveUp => ([Keys.W], [Keys.Up]),
            MoveDown => ([Keys.S], [Keys.Down]),
            MoveLeft => ([Keys.A], [Keys.Left]),
            MoveRight => ([Keys.D], [Keys.Right]),
            ToggleAnimatedStatics => ([Keys.LeftControl, Keys.A], NotAssigned),
            Minimap => ([Keys.M], NotAssigned),
            _ => (NotAssigned, NotAssigned)
        };
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