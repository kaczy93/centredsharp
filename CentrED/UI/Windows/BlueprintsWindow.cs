using CentrED.Blueprints;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.Blueprints.BlueprintManager;

namespace CentrED.UI.Windows;

public class BlueprintsWindow : Window
{
    public override string Name => "Blueprints";

    private BlueprintManager _manager => CEDGame.MapManager.BlueprintManager;

    public List<BlueprintTile> Active => _selectedNode?.Tiles ?? [];

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(LangEntry.NOT_CONNECTED));
            return;
        }
        if (ImGui.BeginTable("##bg", 1, ImGuiTableFlags.RowBg)){
            foreach (var blueprintNode in _manager.Root.Children)
            {
                DrawTreeNode(blueprintNode);
            }
            ImGui.EndTable();
        }
    }
    
    private BlueprintTreeEntry? _selectedNode;

    private void DrawTreeNode(BlueprintTreeEntry node)
    {
        //TODO: Add filtering
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiTreeNodeFlags tree_flags = ImGuiTreeNodeFlags.None;
        tree_flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick;// Standard opening mode as we are likely to want to add selection afterwards
        tree_flags |= ImGuiTreeNodeFlags.NavLeftJumpsToParent;  // Left arrow support
        tree_flags |= ImGuiTreeNodeFlags.SpanFullWidth;         // Span full width for easier mouse reach
        tree_flags |= ImGuiTreeNodeFlags.DrawLinesToNodes;      // Always draw hierarchy outlines
        if (node == _selectedNode)
            tree_flags |= ImGuiTreeNodeFlags.Selected;
        if (node.Children.Count == 0)
            tree_flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet;
        
        bool node_open = ImGui.TreeNodeEx(node.Name, tree_flags);
        if (ImGui.IsItemFocused())
        {
            _selectedNode = node;
            _selectedNode.Load();
        }
        if (node_open)
        {
            foreach (var child in node.Children)
            {
                DrawTreeNode(child);
            }
            ImGui.TreePop();
        }
    }
}