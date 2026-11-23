using CentrED.IO;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class OptionsWindow : Window
{
    public override string Name => App.Lang.Get(OPTIONS_WINDOW) + "###Options";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;

    protected override void InternalDraw()
    {
        var uiManager = App.UIManager;
        if (ImGui.BeginTabBar("Options"))
        {
            if (ImGui.BeginTabItem(App.Lang.Get(GENERAL)))
            {
                if (ImGui.Checkbox(App.Lang.Get(OPTION_PREFER_TEXMAPS), ref App.MapManager.PreferTexMaps.Value))
                {
                    App.MapManager.UpdateAllTiles();
                }
                ImGui.Checkbox(App.Lang.Get(OPTION_OBJECT_BRIGHT_HIGHLIGHT), ref App.MapManager.BrightHighlight.Value);
                ImGui.Checkbox(App.Lang.Get(OPTION_LEGACY_MOUSE_SCROLL), ref App.MapManager.LegacyMouseScroll.Value);
                ImGuiEx.Tooltip(App.Lang.Get(OPTION_LEGACY_MOUSE_SCROLL_TOOLTIP));
                var viewportsAvailable = uiManager.HasViewports;
                ImGui.BeginDisabled(!viewportsAvailable);
                if (ImGui.Checkbox(App.Lang.Get(OPTION_VIEWPORTS), ref App.UIManager.Viewports.Value))
                {
                    if (App.UIManager.Viewports.Value)
                    {
                        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
                    }
                    else
                    {
                        ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
                    }
                }
                ImGui.EndDisabled();
                var fontSize = uiManager.FontSize;
                if (ImGuiEx.DragInt(App.Lang.Get(OPTION_FONT_SIZE), ref fontSize, 1, 1, 26))
                {
                    uiManager.FontSize = fontSize;
                }
                var fontIndex = uiManager.FontIndex;
                if(ImGui.Combo(App.Lang.Get(OPTION_FONT), ref fontIndex, uiManager.FontNames, uiManager.FontNames.Length))
                {
                    uiManager.FontIndex = fontIndex;
                }
                var langIndex = App.Lang.LangIndex;
                if (ImGui.Combo(App.Lang.Get(OPTION_LANGUAGE), ref langIndex, App.Lang.LangNames, App.Lang.LangNames.Length))
                {
                    App.Lang.LangIndex = langIndex;
                }
                var numberFormatKeys = new [] { OPTION_NUMBER_FORMAT_HEX, OPTION_NUMBER_FORMAT_DEC, OPTION_NUMBER_FORMAT_HEX_DEC, OPTION_NUMBER_FORMAT_DEC_HEX };
                var numberFormatLabels = numberFormatKeys.Select(App.Lang.Get).ToArray();
                var numberFormatIndex = (int)App.UIManager.NumberFormat.Value;
                if (ImGui.Combo(App.Lang.Get(OPTION_NUMBER_FORMAT), ref numberFormatIndex, numberFormatLabels, numberFormatLabels.Length))
                {
                    App.UIManager.NumberFormat.Value = (NumberDisplayFormat)numberFormatIndex;
                }
                ImGui.EndTabItem();
            }
            DrawKeymapOptions();
            DrawLightOptions();
            if (ImGui.BeginTabItem(App.Lang.Get(VIRTUAL_LAYER)))
            {
                if (ImGui.ColorPicker4(App.Lang.Get(FILL_COLOR), ref App.MapManager.VirtualLayerFillColor.Value))
                {
                    App.MapManager.ApplyEffectColors();
                }
                if (ImGui.ColorPicker4(App.Lang.Get(BORDER_COLOR), ref App.MapManager.VirtualLayerBorderColor.Value))
                {
                    App.MapManager.ApplyEffectColors();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(App.Lang.Get(TERRAIN_GRID)))
            {
                if (ImGui.ColorPicker4(App.Lang.Get(FLAT_COLOR), ref App.MapManager.TerrainGridFlatColor.Value))
                {
                    App.MapManager.ApplyEffectColors();
                }
                if (ImGui.ColorPicker4(App.Lang.Get(ANGLED_COLOR), ref App.MapManager.TerrainGridAngledColor.Value))
                {
                    App.MapManager.ApplyEffectColors();
                }
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private KeyAction? assigningAction;
    private bool assigningPrimary;

    private void DrawLightOptions()
    {
        if (ImGui.BeginTabItem(App.Lang.Get(LIGHTS)))
        {
            if (!App.Lights.Loaded)
            {
                ImGui.Text(App.Lang.Get(NOT_CONNECTED));
            }
            else
            {
                if (ImGui.SliderInt(App.Lang.Get(LIGHT_LEVEL), ref App.Lights.GlobalLightLevel.Value, 0, 30))
                {
                    App.Lights.UpdateGlobalLight();
                }
                if (ImGui.Checkbox(App.Lang.Get(COLORED_LIGHTS), ref App.Lights.ColoredLights.Value))
                {
                    App.MapManager.UpdateLights();
                }
                ImGui.Checkbox(App.Lang.Get(ALTERNATIVE_LIGHTS), ref App.Lights.AltLights.Value);
                {
                    //Do we have to reset?
                }
                if (ImGui.Checkbox(App.Lang.Get(DARK_NIGHTS), ref App.Lights.DarkNights.Value))
                {
                    App.Lights.UpdateGlobalLight();
                }
                if(ImGui.Checkbox(App.Lang.Get(SHOW_INVISIBLE_LIGHTS), ref App.Lights.ShowInvisibleLights.Value))
                {
                    App.MapManager.UpdateLights();
                }
                if (ImGui.Checkbox(App.Lang.Get(CUO_TERRAIN_LIGHTING), ref App.Lights.ClassicUONormals.Value))
                {
                    App.MapManager.UpdateAllTiles();
                }
                ImGuiEx.Tooltip(App.Lang.Get(CUO_TERRAIN_LIGHTING_TOOLTIP));
            }
            ImGui.EndTabItem();
        }
    }
    private void DrawKeymapOptions()
    {
        if (ImGui.BeginTabItem(App.Lang.Get(KEYMAP)))
        {
            DrawSingleKey(App.Keymap.MoveUp);
            DrawSingleKey(App.Keymap.MoveDown);
            DrawSingleKey(App.Keymap.MoveLeft);
            DrawSingleKey(App.Keymap.MoveRight);
            ImGui.Separator();
            DrawSingleKey(App.Keymap.ToggleAnimatedStatics);
            DrawSingleKey(App.Keymap.Minimap);
            ImGui.EndTabItem();
        }
    }


    private bool _showNewKeyPopup;
    
    private void DrawSingleKey(KeyAction action)
    {
        ImGui.Text(action.PrettyName);
        ImGui.SameLine();
        
        ImGui.BeginDisabled(assigningAction != null);
        var label1 = (assigningAction == action && assigningPrimary) ?
            App.Lang.Get(ASSIGN_NEW_KEY) :
            string.Join(" + ", action.Primary.Value.Select(x => x.ToString()));
        if (ImGui.Button($"{label1}##{action.Id}1"))
        {
            assigningAction = action;
            assigningPrimary = true;
            ImGui.OpenPopup("NewKey");
            _showNewKeyPopup = true;
        }
        ImGui.SameLine();
        var label2 = (assigningAction == action && !assigningPrimary) ?
            App.Lang.Get(ASSIGN_NEW_KEY) :
            string.Join(" + ", action.Secondary.Value.Select(x => x.ToString()));
        if (ImGui.Button($"{label2}##{action.Id}2"))
        {
            assigningAction = action;
            assigningPrimary = false;
            ImGui.OpenPopup("NewKey");
            _showNewKeyPopup = true;
        }
        ImGui.EndDisabled();
        
        if (assigningAction == action && ImGui.BeginPopupModal
                ("NewKey", ref _showNewKeyPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            var pressedKeys = App.Keymap.GetKeysDown();
            ImGui.TextUnformatted(string.Format(App.Lang.Get(ENTER_NEW_KEY_FOR_1NAME), assigningAction.PrettyName));
            ImGui.TextUnformatted(string.Join("+", pressedKeys));
            ImGui.Text(App.Lang.Get(PRESS_ESC_TO_CANCEL));
            ImGui.Text(App.Lang.Get(PRESS_BACKSPACE_TO_CLEAR));
            
            foreach (var pressedKey in pressedKeys)
            {
                if (pressedKey == Keys.Escape)
                {
                    assigningAction = null;
                    break;
                }
                if (pressedKey == Keys.Back)
                {
                    if (assigningPrimary)
                        action.Primary.Value = [];
                    else
                        action.Secondary.Value = [];
                    assigningAction = null;
                    break;
                }
                if (!Keymap.IsModifier(pressedKey))
                {
                    var sortedKeys = pressedKeys.OrderBy(k => !Keymap.IsModifier(k)).ToArray();
                    if (assigningPrimary)
                        action.Primary.Value = sortedKeys;
                    else
                        action.Secondary.Value = sortedKeys;
                    assigningAction = null;
                    break;
                }
            }
            if (assigningAction == null)
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}