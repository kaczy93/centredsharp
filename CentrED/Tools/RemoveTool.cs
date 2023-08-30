using CentrED.UI;

namespace CentrED.Tools; 

public class RemoveTool : Tool {
    internal RemoveTool(UIManager uiManager) : base(uiManager) { }
    public override string Name => "RemoveTool";
    protected override void DrawWindowInternal() {
        
    }
}