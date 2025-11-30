using CentrED.Client;
using CentrED.Map;
using CentrED.UI;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;

namespace CentrED.Tools;

public abstract class Tool
{
    protected MapManager MapManager => Application.CEDGame.MapManager;
    protected UIManager UIManager => Application.CEDGame.UIManager;
    protected CentrEDClient Client => Application.CEDClient;
    public abstract string Name { get; }
    public virtual Keys Shortcut => Keys.None;
    private bool openPopup;

    public virtual void PostConstruct(MapManager mapManager)
    {
    }

    internal virtual void Draw()
    {
    }
    
    public virtual void OnActivated(TileObject? o)
    {
    }

    public virtual void OnDeactivated(TileObject? o)
    {
    }
    
    public virtual void OnMousePressed(TileObject? o)
    {
    }

    public virtual void OnMouseReleased(TileObject? o)
    {
    }

    public virtual void OnKeyPressed(Keys key)
    {
    }

    public virtual void OnKeyReleased(Keys key)
    {
    }
    
    public virtual void OnMouseEnter(TileObject? o)
    {
    }

    public virtual void OnMouseLeave(TileObject? o)
    {
    }

    public virtual void Apply(TileObject? o)
    {
        OnMouseEnter(o);
        OnMousePressed(o);
        OnMouseReleased(o);
    }
    
    public void OpenPopup()
    {
        openPopup = true;
        ImGui.SetWindowPos("ToolPopup", ImGui.GetMousePos());
    }

    public void ClosePopup()
    {
        openPopup = false;
    }
    
    public void DrawFloatingWindow()
    {
        if (openPopup)
        {
            if (ImGui.Begin("ToolPopup", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.AlwaysAutoResize))
            {
                openPopup = ImGui.IsWindowFocused();
                Draw();
                ImGui.End();
            }
        }
    }
}