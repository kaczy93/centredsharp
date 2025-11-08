using CentrED.Blueprints;
using Hexa.NET.ImGui;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public class BlueprintsWindow : Window
{
    public override string Name => "Blueprints";

    private BlueprintManager _manager => CEDGame.MapManager.BlueprintManager;
    private string _filter = "";

    public List<BlueprintTile> Active => _selectedNode?.Tiles ?? [];

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(LangEntry.NOT_CONNECTED));
            return;
        }
        ImGui.InputText("##Filter", ref _filter, 64);
        if (ImGui.BeginTable("##bg", 1, ImGuiTableFlags.RowBg)){
            foreach (var blueprintNode in _manager.Root.Children)
            {
                DrawTreeNode(blueprintNode);
            }
            ImGui.EndTable();
        }
    }
    
    private BlueprintTreeEntry? _selectedNode;

    private bool ShouldDraw(BlueprintTreeEntry node)
    {
        if (node.Children.Count == 0)
        {
            return node.Name.Contains(_filter, StringComparison.InvariantCultureIgnoreCase);
        }
        return node.Children.Aggregate(false, (current, child) => current | ShouldDraw(child));
    }

    private void DrawTreeNode(BlueprintTreeEntry node)
    {
        if (!ShouldDraw(node))
        {
            return;
        }
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiTreeNodeFlags tree_flags = ImGuiTreeNodeFlags.None;
        tree_flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;// Standard opening mode as we are likely to want to add selection afterwards
        tree_flags |= ImGuiTreeNodeFlags.NavLeftJumpsToParent;  // Left arrow support
        tree_flags |= ImGuiTreeNodeFlags.SpanFullWidth;         // Span full width for easier mouse reach
        tree_flags |= ImGuiTreeNodeFlags.DrawLinesToNodes;      // Always draw hierarchy outlines
        if (node == _selectedNode)
            tree_flags |= ImGuiTreeNodeFlags.Selected;
        if (!node.Loaded || node.Tiles.Count > 0)
            tree_flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet;
        
        var nodeOpen = ImGui.TreeNodeEx(node.Name, tree_flags);
        if (ImGui.IsItemFocused())
        {
            _selectedNode = node;
            _selectedNode.Load();
        }
        if (nodeOpen)
        {
            foreach (var child in node.Children)
            {
                DrawTreeNode(child);
            }
            ImGui.TreePop();
        }
    }
}