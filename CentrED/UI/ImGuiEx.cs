using System.Numerics;
using Hexa.NET.ImGui;
using static CentrED.LangEntry;

namespace CentrED.UI;

public static class ImGuiEx
{
    public static readonly Vector2 MIN_SIZE = new Vector2(100, 100);
    public static readonly Vector2 MIN_HEIGHT = new Vector2(0, 100);
    public static readonly Vector2 MIN_WIDTH = new Vector2(100, 0);

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
        return TwoWaySwitch(leftLabel, rightLabel, ref value, new Vector2(80, 18));
    }
    
    public static bool TwoWaySwitch(string leftLabel, string rightLabel, ref bool value, Vector2 size, bool rounding = true)
    {
        ImGui.Text(leftLabel);
        ImGui.SameLine();
        var pos = ImGui.GetCursorPos();
        var wpos = ImGui.GetCursorScreenPos();
        if (value)
            wpos.X += size.X / 2;
        var result = ImGui.Button($" ##{leftLabel}{rightLabel}", size); //Just empty label makes button non functional
        if (result)
        {
            value = !value;
        }
        var padding = ImGui.GetStyle().FramePadding;
        ImGui.SetCursorPos(pos);
        ImGui.GetWindowDrawList().AddRectFilled
            (wpos + padding, 
             wpos + new Vector2(size.X / 2, size.Y) - padding, 
             ImGui.GetColorU32(new Vector4(.8f, .8f, 1, 0.5f)), 
             rounding ? ImGui.GetStyle().FrameRounding : 0);
        ImGui.SameLine();
        ImGui.Text(rightLabel);
        return result;
    }

    public static unsafe bool DragInt(string label, ref int value, float v_speed, int v_min, int v_max, int v_step = 1, string format = "%d")
    {
        fixed (void* valuePtr = &value)
        {
            return DragScalar(label, ImGuiDataType.S32, valuePtr, v_speed, &v_min, &v_max, &v_step, format);
        }
    }

    public static unsafe bool DragFloat(string label, ref float value, float v_speed, float v_min, float v_max, float v_step = 1, string format = "%.2f%%")
    {
        fixed (void* valuePtr = &value)
        {
            return DragScalar(label, ImGuiDataType.Float, valuePtr, v_speed, &v_min, &v_max,  &v_step, format);
        }
    }

    private static unsafe bool DragScalar(string label, ImGuiDataType type, void* pData, float v_speed, void* pMin, void* pMax, void* pStep, string format)
    {
        var io = ImGui.GetIO();
        var buttonWidth = ImGui.GetFrameHeight();
        var buttonSize = new Vector2(buttonWidth, buttonWidth);
        var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
        var inputWidth = ImGui.CalcItemWidth() - (buttonWidth + spacing) * 2;
        ImGui.SetNextItemWidth(inputWidth);
        ImGui.PushID(label);
        var result = ImGui.DragScalar("##", type, pData, v_speed, pMin, pMax, format );
        if (ImGui.IsItemHovered() && io.MouseWheel < 0)
        {
            ImGuiP.DataTypeApplyOp(type, '-', pData, pData, pStep);
        }
        if (ImGui.IsItemHovered() && io.MouseWheel > 0)
        {
            ImGuiP.DataTypeApplyOp(type, '+', pData, pData, pStep);
        }
        ImGui.SameLine(0, spacing);
        ImGui.PushItemFlag(ImGuiItemFlags.ButtonRepeat, true);
        if (ImGui.Button("-", buttonSize))
        {
            ImGuiP.DataTypeApplyOp(type, '-', pData, pData, pStep);
            result = true;
        }
        ImGui.SameLine(0, spacing);
        if (ImGui.Button("+", buttonSize))
        {
            ImGuiP.DataTypeApplyOp(type, '+', pData, pData, pStep);
            result = true;
        }
        ImGui.PopItemFlag();
        ImGui.SameLine();
        ImGui.Text(label);
        ImGui.PopID();
        ImGuiP.DataTypeClamp(type, pData, pMin, pMax);
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

    //Regular InputText but label is on the left
    public static bool InputText(string label, string labelId, ref string value, UIntPtr bufSize)
    {
        ImGui.Text(label);
        ImGui.SameLine();
        return ImGui.InputText(labelId, ref value, 32);
    }

    public static bool ConfirmButton(string label, string prompt)
    {
        return ConfirmButton(label, prompt, LangManager.Get(CONFIRM), LangManager.Get(CANCEL));
    }
    
    public static bool ConfirmButton(string label, string prompt, string yText, string nText)
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