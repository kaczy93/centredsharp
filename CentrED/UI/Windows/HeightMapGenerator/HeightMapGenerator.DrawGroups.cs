using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CentrED.Client;
using CentrED.Client.Map;
using CentrED.IO.Models;
using CentrED.Network;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.Application;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void DrawGroups(Dictionary<string, Group> groups, ref string selected, ref string newName)
    {
        if (ImGui.BeginChild("LandGroups", new System.Numerics.Vector2(0, 120), ImGuiChildFlags.Borders))
        {
            foreach (var kv in groups.ToArray())
            {
                ImGui.PushID($"l_{kv.Key}");
                bool isSel = selected == kv.Key;
                if (ImGui.Selectable($"{kv.Key} ({kv.Value.Chance:0.#}% )", isSel))
                    selected = kv.Key;
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        groups.Remove(kv.Key);
                        if (selected == kv.Key) selected = string.Empty;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
                ImGui.PopID();
            }
            ImGui.EndChild();
        }
        ImGui.InputText("##newgroup_l", ref newName, 32);
        ImGui.SameLine();
        if (ImGui.Button("Add##l"))
        {
            if (!string.IsNullOrWhiteSpace(newName) && !groups.ContainsKey(newName))
            {
                groups.Add(newName, new Group());
                selected = newName;
                newName = string.Empty;
            }
        }
        if (!string.IsNullOrEmpty(selected) && groups.TryGetValue(selected, out var grp))
        {
            ImGui.DragFloat($"Chance (%)##l_{selected}", ref grp.Chance, 0.1f, 0f, 100f);
            int minH = grp.MinHeight;
            int maxH = grp.MaxHeight;
            ImGui.DragInt("Min Height", ref minH, 1, -128, 127);
            ImGui.DragInt("Max Height", ref maxH, 1, -128, 127);
            grp.MinHeight = (sbyte)minH;
            grp.MaxHeight = (sbyte)maxH;
            if (ImGui.BeginChild($"{selected}_tiles", new System.Numerics.Vector2(0, 100), ImGuiChildFlags.Borders))
            {
                foreach (var id in grp.Ids.ToArray())
                {
                    ImGui.Text($"0x{id:X4}");
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(1, 0, 0, 0.2f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(1, 0, 0, 1));
                    if (ImGui.SmallButton($"x##{id}"))
                    {
                        grp.Ids.Remove(id);
                    }
                    ImGui.PopStyleColor(2);
                }
                ImGui.Button($"+##{selected}");
                if (ImGui.BeginDragDropTarget())
                {
                    var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
                    unsafe
                    {
                        if (payloadPtr.NativePtr != null)
                        {
                            var dataPtr = (int*)payloadPtr.Data;
                            ushort id = (ushort)dataPtr[0];
                            grp.Ids.Add(id);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
                ImGui.EndChild();
            }
        }
    }

}
