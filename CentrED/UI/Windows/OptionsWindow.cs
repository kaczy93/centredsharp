using CentrED.IO;
using CentrED.Lights;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.CentrED;
using static CentrED.LangEntry;
using Vector4 = System.Numerics.Vector4;

namespace CentrED.UI.Windows;

public class OptionsWindow : Window
{
    public override string Name => LangManager.Get(OPTIONS_WINDOW) + "###Options";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize;

    private int _lightLevel = 30;
    private Vector4 _virtualLayerFillColor = new(0.2f, 0.2f, 0.2f, 0.1f);
    private Vector4 _virtualLayerBorderColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private Vector4 _terrainGridFlatColor = new(0.5f, 0.5f, 0.0f, 0.5f);
    private Vector4 _terrainGridAngledColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    protected override void InternalDraw()
    {
        var uiManager = App.UIManager;
        if (ImGui.BeginTabBar("Options"))
        {
            if (ImGui.BeginTabItem(LangManager.Get(GENERAL)))
            {
                if (ImGui.Checkbox(LangManager.Get(OPTION_PREFER_TEXMAPS), ref App.MapManager.PreferTexMaps.Value))
                {
                    App.MapManager.UpdateAllTiles();
                }
                ImGui.Checkbox(LangManager.Get(OPTION_OBJECT_BRIGHT_HIGHLIGHT), ref App.MapManager.BrightHighlight.Value);
                ImGui.Checkbox(LangManager.Get(OPTION_LEGACY_MOUSE_SCROLL), ref App.MapManager.LegacyMouseScroll.Value);
                ImGuiEx.Tooltip(LangManager.Get(OPTION_LEGACY_MOUSE_SCROLL_TOOLTIP));
                var viewportsAvailable = uiManager.HasViewports;
                ImGui.BeginDisabled(!viewportsAvailable);
                if (ImGui.Checkbox(LangManager.Get(OPTION_VIEWPORTS), ref App.UIManager.Viewports.Value))
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
                if (ImGuiEx.DragInt(LangManager.Get(OPTION_FONT_SIZE), ref fontSize, 1, 1, 26))
                {
                    uiManager.FontSize = fontSize;
                }
                var fontIndex = uiManager.FontIndex;
                if(ImGui.Combo(LangManager.Get(OPTION_FONT), ref fontIndex, uiManager.FontNames, uiManager.FontNames.Length))
                {
                    uiManager.FontIndex = fontIndex;
                }
                var langIndex = LangManager.LangIndex;
                if (ImGui.Combo(LangManager.Get(OPTION_LANGUAGE), ref langIndex, LangManager.LangNames, LangManager.LangNames.Length))
                {
                    LangManager.LangIndex = langIndex;
                }
                var numberFormatKeys = new [] { OPTION_NUMBER_FORMAT_HEX, OPTION_NUMBER_FORMAT_DEC, OPTION_NUMBER_FORMAT_HEX_DEC, OPTION_NUMBER_FORMAT_DEC_HEX };
                var numberFormatLabels = numberFormatKeys.Select(LangManager.Get).ToArray();
                var numberFormatIndex = (int)NumberFormatter.NumberFormat.Value;
                if (ImGui.Combo(LangManager.Get(OPTION_NUMBER_FORMAT), ref numberFormatIndex, numberFormatLabels, numberFormatLabels.Length))
                {
                    NumberFormatter.NumberFormat.Value = (NumberDisplayFormat)numberFormatIndex;
                }
                ImGui.EndTabItem();
            }
            DrawKeymapOptions();
            DrawLightOptions();
            if (ImGui.BeginTabItem(LangManager.Get(VIRTUAL_LAYER)))
            {
                if (ImGui.ColorPicker4(LangManager.Get(FILL_COLOR), ref _virtualLayerFillColor))
                {
                    App.MapManager.MapEffect.VirtualLayerFillColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _virtualLayerFillColor.X,
                        _virtualLayerFillColor.Y,
                        _virtualLayerFillColor.Z,
                        _virtualLayerFillColor.W
                    );
                }
                if (ImGui.ColorPicker4(LangManager.Get(BORDER_COLOR), ref _virtualLayerBorderColor))
                {
                    App.MapManager.MapEffect.VirtualLayerBorderColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _virtualLayerBorderColor.X,
                        _virtualLayerBorderColor.Y,
                        _virtualLayerBorderColor.Z,
                        _virtualLayerBorderColor.W
                    );
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(LangManager.Get(TERRAIN_GRID)))
            {
                if (ImGui.ColorPicker4(LangManager.Get(FLAT_COLOR), ref _terrainGridFlatColor))
                {
                    App.MapManager.MapEffect.TerrainGridFlatColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _terrainGridFlatColor.X,
                        _terrainGridFlatColor.Y,
                        _terrainGridFlatColor.Z,
                        _terrainGridFlatColor.W
                    );
                }
                if (ImGui.ColorPicker4(LangManager.Get(ANGLED_COLOR), ref _terrainGridAngledColor))
                {
                    App.MapManager.MapEffect.TerrainGridAngledColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _terrainGridAngledColor.X,
                        _terrainGridAngledColor.Y,
                        _terrainGridAngledColor.Z,
                        _terrainGridAngledColor.W
                    );
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
        if (ImGui.BeginTabItem(LangManager.Get(LIGHTS)))
        {
            if (LightsManager.Instance == null)
            {
                ImGui.Text(LangManager.Get(NOT_CONNECTED));
            }
            else
            {
                if (ImGui.SliderInt(LangManager.Get(LIGHT_LEVEL), ref LightsManager.Instance.GlobalLightLevel, 0, 30))
                {
                    LightsManager.Instance.UpdateGlobalLight();
                }
                if (ImGui.Checkbox(LangManager.Get(COLORED_LIGHTS), ref LightsManager.Instance.ColoredLights))
                {
                    App.MapManager.UpdateLights();
                }
                ImGui.Checkbox(LangManager.Get(ALTERNATIVE_LIGHTS), ref LightsManager.Instance.AltLights);
                {
                    //Do we have to reset?
                }
                if (ImGui.Checkbox(LangManager.Get(DARK_NIGHTS), ref LightsManager.Instance.DarkNights))
                {
                    LightsManager.Instance.UpdateGlobalLight();
                }
                if(ImGui.Checkbox(LangManager.Get(SHOW_INVISIBLE_LIGHTS), ref LightsManager.Instance.ShowInvisibleLights))
                {
                    App.MapManager.UpdateLights();
                }
                if (ImGui.Checkbox(LangManager.Get(CUO_TERRAIN_LIGHTING), ref LightsManager.Instance.ClassicUONormals))
                {
                    App.MapManager.UpdateAllTiles();
                }
                ImGuiEx.Tooltip(LangManager.Get(CUO_TERRAIN_LIGHTING_TOOLTIP));
            }
            ImGui.EndTabItem();
        }
    }
    private void DrawKeymapOptions()
    {
        if (ImGui.BeginTabItem(LangManager.Get(KEYMAP)))
        {
            DrawSingleKey(KeyAction.MoveUp);
            DrawSingleKey(KeyAction.MoveDown);
            DrawSingleKey(KeyAction.MoveLeft);
            DrawSingleKey(KeyAction.MoveRight);
            ImGui.Separator();
            DrawSingleKey(KeyAction.ToggleAnimatedStatics);
            DrawSingleKey(KeyAction.Minimap);
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
            LangManager.Get(ASSIGN_NEW_KEY) :
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
            LangManager.Get(ASSIGN_NEW_KEY) :
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
            ImGui.Text(string.Format(LangManager.Get(ENTER_NEW_KEY_FOR_1NAME), assigningAction));
            ImGui.Text(string.Join("+", pressedKeys));
            ImGui.Text(LangManager.Get(PRESS_ESC_TO_CANCEL));
            
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
                if (pressedKey is >= Keys.A and <= Keys.Z)
                {
                    var sortedKeys = pressedKeys.Order(new Keymap.LetterLastComparer()).ToArray();
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