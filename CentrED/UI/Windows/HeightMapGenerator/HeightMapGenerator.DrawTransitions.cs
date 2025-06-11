using ImGuiNET;
using ClassicUO.Assets;
using System.Numerics;
using static CentrED.Application;

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
                        if (tile.Id != 0)
                        {
                            var tex = CalculateButtonTexture(tile.Id);
                            if (ImGui.ImageButton("tile", tex.texPtr, new Vector2(44, 44), tex.uv0, tex.uv1))
                            {
                                tiles[idx] = new Tile(tile.Type, 0);
                            }
                            UIManager.Tooltip(label);
                        }
                        else
                        {
                            if (ImGui.Button("---", new Vector2(44, 44)))
                            {
                                // already empty, nothing to do
                            }
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

    private (nint texPtr, Vector2 uv0, Vector2 uv1) CalculateButtonTexture(ushort tileId)
    {
        var spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(TileDataLoader.Instance.LandData[tileId].TexID);
        if (spriteInfo.Texture == null)
        {
            spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(0x0001);
        }
        var tex = spriteInfo.Texture;
        var bounds = spriteInfo.UV;
        var texPtr = CEDGame.UIManager._uiRenderer.BindTexture(tex);
        var fWidth = (float)tex.Width;
        var fHeight = (float)tex.Height;
        var uv0 = new Vector2(bounds.X / fWidth, bounds.Y / fHeight);
        var uv1 = new Vector2((bounds.X + bounds.Width) / fWidth, (bounds.Y + bounds.Height) / fHeight);
        return (texPtr, uv0, uv1);
    }
}
