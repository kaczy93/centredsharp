using CentrED.Lights;
using Hexa.NET.ImGui;
using Microsoft.Xna.Framework.Input;
using static CentrED.Application;
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
        var uiManager = CEDGame.UIManager;
        if (ImGui.BeginTabBar("Options"))
        {
            if (ImGui.BeginTabItem(LangManager.Get(GENERAL)))
            {
                if (ImGui.Checkbox(LangManager.Get(OPTION_PREFER_TEXMAPS), ref Config.Instance.PreferTexMaps))
                {
                    CEDGame.MapManager.UpdateAllTiles();
                }
                ImGui.Checkbox(LangManager.Get(OPTION_LEGACY_MOUSE_SCROLL), ref Config.Instance.LegacyMouseScroll);
                ImGuiEx.Tooltip(LangManager.Get(OPTION_LEGACY_MOUSE_SCROLL_TOOLTIP));
                var viewportsAvailable = uiManager.HasViewports;
                ImGui.BeginDisabled(!viewportsAvailable);
                if (ImGui.Checkbox(LangManager.Get(OPTION_VIEWPORTS), ref Config.Instance.Viewports))
                {
                    if (Config.Instance.Viewports)
                    {
                        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
                    }
                    else
                    {
                        ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.ViewportsEnable;
                    }
                }
                ImGui.EndDisabled();
                ImGuiEx.DragInt(LangManager.Get(OPTION_FONT_SIZE), ref Config.Instance.FontSize, 1, 1, 26);
                var fontIndex = uiManager.FontIndex;
                if(ImGui.Combo(LangManager.Get(OPTION_FONT_FACE), ref fontIndex, uiManager.FontNames, uiManager.FontNames.Length))
                {
                    uiManager.FontIndex = fontIndex;
                    Config.Instance.FontName = uiManager.FontNames[fontIndex];
                }
                var langIndex = LangManager.LangIndex;
                if (ImGui.Combo(LangManager.Get(OPTION_LANGUAGE), ref langIndex, LangManager.LangNames, LangManager.LangNames.Length))
                {
                    LangManager.LangIndex = langIndex;
                    Config.Instance.Language = LangManager.LangNames[langIndex];
                }
                ImGui.EndTabItem();
            }
            DrawKeymapOptions();
            DrawLightOptions();
            if (ImGui.BeginTabItem(LangManager.Get(VIRTUAL_LAYER)))
            {
                if (ImGui.ColorPicker4(LangManager.Get(FILL_COLOR), ref _virtualLayerFillColor))
                {
                    CEDGame.MapManager.MapEffect.VirtualLayerFillColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _virtualLayerFillColor.X,
                        _virtualLayerFillColor.Y,
                        _virtualLayerFillColor.Z,
                        _virtualLayerFillColor.W
                    );
                }
                if (ImGui.ColorPicker4(LangManager.Get(BORDER_COLOR), ref _virtualLayerBorderColor))
                {
                    CEDGame.MapManager.MapEffect.VirtualLayerBorderColor = new Microsoft.Xna.Framework.Vector4
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
                    CEDGame.MapManager.MapEffect.TerrainGridFlatColor = new Microsoft.Xna.Framework.Vector4
                    (
                        _terrainGridFlatColor.X,
                        _terrainGridFlatColor.Y,
                        _terrainGridFlatColor.Z,
                        _terrainGridFlatColor.W
                    );
                }
                if (ImGui.ColorPicker4(LangManager.Get(ANGLED_COLOR), ref _terrainGridAngledColor))
                {
                    CEDGame.MapManager.MapEffect.TerrainGridAngledColor = new Microsoft.Xna.Framework.Vector4
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

    private string assigningActionName = "";
    private byte assignedKeyNumber = 0;

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
                    CEDGame.MapManager.UpdateLights();
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
                    CEDGame.MapManager.UpdateLights();
                }
                if (ImGui.Checkbox(LangManager.Get(CUO_TERRAIN_LIGHTING), ref LightsManager.Instance.ClassicUONormals))
                {
                    CEDGame.MapManager.UpdateAllTiles();
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
            DrawSingleKey(Keymap.MoveUp);
            DrawSingleKey(Keymap.MoveDown);
            DrawSingleKey(Keymap.MoveLeft);
            DrawSingleKey(Keymap.MoveRight);
            ImGui.Separator();
            DrawSingleKey(Keymap.ToggleAnimatedStatics);
            DrawSingleKey(Keymap.Minimap);
            ImGui.EndTabItem();
        }
    }


    private bool _showNewKeyPopup;

    private void DrawSingleKey(string action)
    {
        var keys = Keymap.GetKeys(action);
        ImGui.Text(Keymap.PrettyName(action));
        ImGui.SameLine();
        ImGui.BeginDisabled(assigningActionName != "");
        var label1 = (assigningActionName == action && assignedKeyNumber == 1) ?
            LangManager.Get(ASSIGN_NEW_KEY) :
            string.Join(" + ", keys.Item1.Select(x => x.ToString()));
        if (ImGui.Button($"{label1}##{action}1"))
        {
            assigningActionName = action;
            assignedKeyNumber = 1;
            ImGui.OpenPopup("NewKey");
            _showNewKeyPopup = true;
        }
        ImGui.SameLine();
        var label2 = (assigningActionName == action && assignedKeyNumber == 2) ?
            LangManager.Get(ASSIGN_NEW_KEY) :
            string.Join(" + ", keys.Item2.Select(x => x.ToString()));
        if (ImGui.Button($"{label2}##{action}2"))
        {
            assigningActionName = action;
            assignedKeyNumber = 2;
            ImGui.OpenPopup("NewKey");
            _showNewKeyPopup = true;
        }
        ImGui.EndDisabled();
        if (assigningActionName == action && ImGui.BeginPopupModal
                ("NewKey", ref _showNewKeyPopup, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            var pressedKeys = Keymap.GetKeysPressed();
            ImGui.Text(string.Format(LangManager.Get(ENTER_NEW_KEY_FOR_1NAME), assigningActionName));
            ImGui.Text(string.Join("+", pressedKeys));
            ImGui.Text(LangManager.Get(PRESS_ESC_TO_CANCEL));
            
            foreach (var pressedKey in pressedKeys)
            {
                if (pressedKey == Keys.Escape)
                {
                    assigningActionName = "";
                    assignedKeyNumber = 0;
                    break;
                }
                if (pressedKey is >= Keys.A and <= Keys.Z)
                {
                    var sortedKeys = pressedKeys.Order(new Keymap.LetterLastComparer()).ToArray();
                    var oldKeys = Config.Instance.Keymap[action];
                    var newKeys = assignedKeyNumber == 1 ? (sortedKeys, oldKeys.Item2) : (oldKeys.Item1, sortedKeys);
                    Config.Instance.Keymap[action] = newKeys;
                    assigningActionName = "";
                    assignedKeyNumber = 0;
                }
            }
            if (assigningActionName == "")
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}