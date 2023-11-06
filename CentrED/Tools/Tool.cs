using CentrED.Map;
using CentrED.UI;

namespace CentrED.Tools; 

public abstract class Tool {
    internal UIManager _uiManager;
    internal MapManager _mapManager;
    internal Tool(UIManager uiManager) {
        _uiManager = uiManager;
        _mapManager = uiManager._mapManager;
    }
    public abstract string Name { get; }
    
    internal virtual void DrawWindow() {
        
    }

    public virtual void OnActivated(MapObject? o) {
        
    }

    public virtual void OnDeactivated(MapObject? o) {
        
    }

    public virtual void OnMouseEnter(MapObject? o) {
        
    }

    public virtual void OnMouseLeave(MapObject? o) {
        
    }

    public virtual void OnMousePressed(MapObject? o) {
        
    }

    public virtual void OnMouseReleased(MapObject? o) {
        
    }
}