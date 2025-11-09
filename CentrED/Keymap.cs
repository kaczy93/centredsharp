using Microsoft.Xna.Framework.Input;

namespace CentrED;

public class Keymap
{
    private static readonly Keys[] FunctionKeys =
        [Keys.LeftControl, Keys.LeftAlt, Keys.LeftShift, Keys.RightControl, Keys.RightAlt, Keys.RightShift];
    
    private KeyboardState currentState;
    private KeyboardState previousState;

    public static readonly Keys[] NotAssigned = [];

    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string ToggleAnimatedStatics = "toggle_animated_statics";
    public const string Minimap = "minimap";

    public void Update(KeyboardState newState)
    {
        previousState = currentState;
        currentState = newState;
    }

    public bool IsKeyDown(Keys key)
    {
        return currentState.IsKeyDown(key);
    }

    public bool IsKeyUp(Keys key)
    {
        return currentState.IsKeyUp(key);
    }
    
    public bool IsKeyPressed(Keys key)
    {
        return currentState.IsKeyDown(key) && previousState.IsKeyUp(key);
    }

    public bool IsKeyReleased(Keys key)
    {
        return currentState.IsKeyUp(key) && previousState.IsKeyDown(key);
    }
    
    public Keys[] GetKeysDown()
    {
        return currentState.GetPressedKeys();
    }

    public Keys[] GetKeysPressed()
    {
        return currentState.GetPressedKeys().Except(previousState.GetPressedKeys()).ToArray();
    }

    public Keys[] GetKeysReleased()
    {
        return previousState.GetPressedKeys().Except(currentState.GetPressedKeys()).ToArray();
    }
    
    public bool IsActionDown(string action)
    {
        var assignedKeys = GetKeys(action);
        return assignedKeys.Item1.All(currentState.IsKeyDown) || assignedKeys.Item2.All(currentState.IsKeyDown);
    }
    
    public bool IsActionUp(string action)
    {
        var assignedKeys = GetKeys(action);
        return assignedKeys.Item1.All(currentState.IsKeyUp) || assignedKeys.Item2.All(currentState.IsKeyUp);
    }

    public bool IsActionPressed(string action)
    {
        var assignedKeys = GetKeys(action);
        return (assignedKeys.Item1.All(currentState.IsKeyDown) && assignedKeys.Item1.Any(previousState.IsKeyUp)) ||
            (assignedKeys.Item2.All(currentState.IsKeyDown) && assignedKeys.Item2.Any(previousState.IsKeyUp));
    }

    public (Keys[], Keys[]) GetKeys(string action)
    {
        InitAction(action);
        return Config.Instance.Keymap[action];
    }

    public string GetShortcut(string action)
    {
        return string.Join('+', GetKeys(action).Item1);
    }

    public string PrettyName(string action)
    {
        return string.Join(' ', action.Split('_').Select(s => char.ToUpper(s[0]) + s[1..]));
    }
    
    private void InitAction(string action)
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
    
    private (Keys[],Keys[]) GetDefault(string action)
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