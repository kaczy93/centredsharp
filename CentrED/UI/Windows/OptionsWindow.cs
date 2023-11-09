using ImGuiNET;

namespace CentrED.UI.Windows; 

public class OptionsWindow : Window{
    public override string Name => "Options";
    public override void Draw() {
        if (!Show) return;
        
        ImGui.Begin("Options", ref _show, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize);
        ImGui.Text("Nothing to see here (yet) :)");
        ImGui.End();
    }
}