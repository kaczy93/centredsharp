using ImGuiNET;
using System.Numerics;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void DrawTransitions(Dictionary<string, Tile[]> transitions, ref string selected)
    {
        if (ImGui.BeginChild("TransitionList", new Vector2(0, 120), ImGuiChildFlags.Borders))
        {
            foreach (var kv in transitions)
            {
                bool isSel = selected == kv.Key;
                if (ImGui.Selectable(kv.Key, isSel))
                    selected = kv.Key;
            }
            ImGui.EndChild();
        }

        if (!string.IsNullOrEmpty(selected) && transitions.TryGetValue(selected, out var tiles))
        {
            if (ImGui.BeginChild($"{selected}_tiles", new Vector2(0, 120), ImGuiChildFlags.Borders))
            {
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        int idx = row * 3 + col;
                        var tile = tiles[idx];
                        string label = tile.Id != 0 ? $"0x{tile.Id:X4}" : "---";
                        ImGui.PushID(idx);
                        if (ImGui.SmallButton(label))
                        {
                            tiles[idx] = new Tile(tile.Type, 0);
                        }
                        if (ImGui.BeginDragDropTarget())
                        {
                            var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
                            unsafe
                            {
                                if (payloadPtr.NativePtr != null)
                                {
                                    var dataPtr = (int*)payloadPtr.Data;
                                    ushort id = (ushort)dataPtr[0];
                                    tiles[idx] = new Tile(tile.Type, id);
                                }
                            }
                            ImGui.EndDragDropTarget();
                        }
                        ImGui.PopID();
                        if (col < 2)
                            ImGui.SameLine();
                    }
                }
                ImGui.EndChild();
            }
        }
    }
}
