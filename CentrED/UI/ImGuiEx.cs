using System.Numerics;
using ImGuiNET;

namespace CentrED.UI;

public static class ImGuiEx
{
    public static void Tooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }

    public static bool TwoWaySwitch(string leftLabel, string rightLabel, ref bool value)
    {
        ImGui.Text(leftLabel);
        ImGui.SameLine();
        var pos = ImGui.GetCursorPos();
        var wpos = ImGui.GetCursorScreenPos();
        if (value)
            wpos.X += 40;
        var result = ImGui.Button($" ##{leftLabel}{rightLabel}", new Vector2(80, 18)); //Just empty label makes button non functional
        if (result)
        {
            value = !value;
        }
        ImGui.SetCursorPos(pos);
        ImGui.GetWindowDrawList().AddRectFilled
            (wpos, wpos + new Vector2(40, 18), ImGui.GetColorU32(new Vector4(.8f, .8f, 1, 0.5f)));
        ImGui.SameLine();
        ImGui.Text(rightLabel);
        return result;
    }

    public static bool DragInt(ReadOnlySpan<char> label, ref int value, float v_speed, int v_min, int v_max)
    {
        ImGui.PushItemWidth(50);
        var result = ImGui.DragInt($"##{label}", ref value, v_speed, v_min, v_max);
        if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel > 0)
        {
            value++;
        }
        if (ImGui.IsItemHovered() && ImGui.GetIO().MouseWheel < 0)
        {
            value--;
        }
        ImGui.PopItemWidth();
        Tooltip("Drag Left/Right");
        ImGui.SameLine(0, 0);
        ImGui.PushItemFlag(ImGuiItemFlags.ButtonRepeat, true);
        if (ImGui.ArrowButton($"{label}down", ImGuiDir.Down))
        {
            value--;
            result = true;
        }
        ImGui.SameLine(0, 0);
        if (ImGui.ArrowButton($"{label}up", ImGuiDir.Up))
        {
            value++;
            result = true;
        }
        ImGui.PopItemFlag();
        ImGui.SameLine();
        ImGui.Text(label);
        value = Math.Clamp(value, v_min, v_max);
        return result;
    }

    public static unsafe bool InputUInt16(ReadOnlySpan<char> label, ref ushort value)
    {
        fixed (ushort* ptr = &value)
        {
            return ImGui.InputScalar(label, ImGuiDataType.U16, (IntPtr)ptr);
        }
    }

    public static bool ConfirmButton(string label, string prompt, string yText = "Confirm", string nText = "Cancel")
    {
        var result = false;
        if (ImGui.Button(label))
        {
            ImGui.OpenPopup(label);
        }
        if (ImGui.BeginPopupModal(label, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(prompt);
            var buttonWidth = Math.Max(ImGui.CalcTextSize(yText).X, ImGui.CalcTextSize(nText).X) + ImGui.GetStyle().FramePadding.X * 2;
            if (ImGui.Button(yText, new Vector2(buttonWidth, 0)))
            {
                result = true;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button(nText, new Vector2(buttonWidth, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        return result;
    }
}