using System.Numerics;
using Hexa.NET.ImGui;

namespace CentrED.UI;

public static class ImGuiEx
{
    //This tooltip will be shown instantly when hovering over the item
    //If you want a slight delay, use ImGui.SetItemTooltip()
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

    public static bool DragInt(string label, ref int value, float v_speed, int v_min, int v_max)
    {
        ImGui.BeginGroup();
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
        ImGui.EndGroup();
        return result;
    }

    public static unsafe bool InputUInt16(string label, ref ushort value, ushort minValue = ushort.MinValue, ushort maxValue = ushort.MaxValue)
    {
        fixed (ushort* ptr = &value)
        {
            var result = ImGui.InputScalar(label, ImGuiDataType.U16, ptr);
            value = Math.Clamp(value, minValue, maxValue);
            return result;
        }
    }
    
    public static unsafe bool InputUInt32
        (string label, ref uint value, uint minValue = uint.MinValue, uint maxValue = uint.MaxValue)
    {
        fixed (uint* ptr = &value)
        {
            var result = ImGui.InputScalar(label, ImGuiDataType.U32, ptr);
            value = Math.Clamp(value, minValue, maxValue);
            return result;
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

    public static bool BeginStatusBar()
    {
        var style = ImGui.GetStyle();
        ImGuiP.ImGuiNextWindowData().MenuBarOffsetMinVal = new Vector2
            (style.DisplaySafeAreaPadding.X, Math.Max(style.DisplaySafeAreaPadding.Y - style.FramePadding.Y, 0));
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoInputs;
        var height = ImGui.GetFrameHeight();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0, height));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6,4));
        var isOpen =  ImGuiP.BeginViewportSideBar("##StatusBar", ImGui.GetMainViewport(), ImGuiDir.Down, height, flags);
        ImGuiP.ImGuiNextWindowData().MenuBarOffsetMinVal = Vector2.Zero;
        if (!isOpen)
        {
            EndStatusBar();
            return false;
        }
        return isOpen;
    }

    public static void EndStatusBar()
    {
        ImGui.End();
        ImGui.PopStyleVar(2);
    }
}