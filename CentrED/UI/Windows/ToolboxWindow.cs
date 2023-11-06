using CentrED.Tools;
using ImGuiNET;

namespace CentrED.UI.Windows; 

public class ToolboxWindow : Window {
    public ToolboxWindow(UIManager uiManager) : base(uiManager) { }
    public override string Name => "Toolbox";
    public override void Draw() {
        if (!Show) return;

        ImGui.Begin(Id, ref _show);
        _uiManager.tools.ForEach(ToolButton);
        ImGui.End();
        _mapManager.ActiveTool?.DrawWindow();
    }
    
    private void ToolButton(Tool tool) {
        if (ImGui.RadioButton(tool.Name, _mapManager.ActiveTool == tool)) {
            _mapManager.ActiveTool?.OnDeactivated(_mapManager.Selected);
            _mapManager.ActiveTool = tool;
            _mapManager.ActiveTool?.OnActivated(_mapManager.Selected);
        }
    }
}