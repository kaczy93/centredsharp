using CentrED.Map;

namespace CentrED.Tools; 

public abstract class Tool {
    public bool Active;
    
    public virtual void DrawWindow() {
        
    }

    public virtual void Action(StaticObject so) {
        
    }
}