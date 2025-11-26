using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;
using Rectangle = System.Drawing.Rectangle;

namespace CentrED.UI.Windows;

public class HuesWindow : Window
{
    public HuesWindow()
    {
        CEDClient.Connected += FilterHues;
        UpdateHueSetNames();
        UpdateHueSetValues();
    }

    public override string Name => LangManager.Get(HUES_WINDOW) + "###Hues";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    public bool UpdateScroll;
    private string _filter = "";

    private int _selectedId;
    public ushort SelectedId => (ushort)_selectedId;

    private ushort[] _matchedHueIds;
    public const string Hue_DragDrop_Target_Type = "HueDragDrop";
    
    private void FilterHues()
    {
        var huesManager = HuesManager.Instance;
        if (_filter.Length == 0)
        {
            _matchedHueIds = new ushort[huesManager.HuesCount];
            for (ushort i = 0; i < huesManager.HuesCount; i++)
            {
                _matchedHueIds[i] = i;
            }
        }
        else
        {
            var matchedIds = new List<ushort>();
            for (ushort i = 0; i < huesManager.HuesCount; i++)
            {
                var name = huesManager.Names[i];
                if (
                    name.Contains(_filter, StringComparison.InvariantCultureIgnoreCase) || 
                    i.FormatId(NumberDisplayFormat.HEX).Contains(_filter, StringComparison.InvariantCultureIgnoreCase) || 
                    i.FormatId(NumberDisplayFormat.DEC).Contains(_filter, StringComparison.InvariantCultureIgnoreCase)
                    )
                    matchedIds.Add(i);
            }
            _matchedHueIds = matchedIds.ToArray();
        }
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(NOT_CONNECTED));
            return;
        }
        if (ImGui.Button(LangManager.Get(SCROLL_TO_SELECTED)))
        {
            UpdateScroll = true;
        }

        ImGui.Text(LangManager.Get(FILTER));
        if (ImGui.InputText("##Filter", ref _filter, 64))
        {
            FilterHues();
        }
        ImGui.SetNextWindowSizeConstraints(ImGuiEx.MIN_SIZE, ImGui.GetContentRegionAvail() - ImGuiEx.MIN_HEIGHT);
        DrawHues();
        DrawHueSets();
    }

    private void DrawHues()
    {
        if (ImGui.BeginChild("Hues", ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY))
        {
            if (ImGui.BeginTable("HuesTable", 2))
            {
                var clipper = ImGui.ImGuiListClipper();
                var textSize = ImGui.CalcTextSize(0xFFFF.FormatId());
                var columnHeight = textSize.Y;
                ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, textSize.X);
                clipper.Begin(_matchedHueIds.Length, columnHeight);
                while (clipper.Step())
                {
                    for (var rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                    {
                        var hueIndex = _matchedHueIds[rowIndex];
                        DrawHueRow($"{hueIndex.FormatId()}##hue", ref _selectedId, hueIndex, columnHeight);
                        if (ImGui.BeginPopupContextItem())
                        {
                            if (ImGui.Button(LangManager.Get(ADD_TO_SET)))
                            {
                                AddToHueSet(hueIndex);
                                ImGui.CloseCurrentPopup();
                            }
                            ImGui.EndPopup();
                        }
                    }
                }
                clipper.End();
                if (UpdateScroll)
                {
                    var itemPosY = (float)clipper.StartPosY + columnHeight * Array.IndexOf
                        (_matchedHueIds, SelectedId);
                    ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                    UpdateScroll = false;
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private int _hueSetIndex;
    private string HueSetName => _hueSetNames[_hueSetIndex];
    private string _hueSetNewName = "";
    private SortedSet<ushort> _tempHueSetValues = [];
    public ushort[] ActiveHueSetValues;

    private static Dictionary<string, SortedSet<ushort>> HueSets => ProfileManager.ActiveProfile.HueSets;
    private string[] _hueSetNames = HueSets.Keys.ToArray();

    private void DrawHueSets()
    {
        if (ImGui.BeginChild("HueSets"))
        {
            ImGui.Text(LangManager.Get(HUE_SET));
            if (ImGui.Button(LangManager.Get(NEW)))
            {
                ImGui.OpenPopup("NewHueSet");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(_hueSetIndex == 0);
            if (ImGui.Button(LangManager.Get(DELETE)))
            {
                ImGui.OpenPopup("DeleteHueSet");
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveHueSetValues.Length == 0);
            if (ImGui.Button(LangManager.Get(CLEAR)))
            {
                ClearHueSet();
            }
            ImGui.EndDisabled();
            if (ImGui.Combo("##HueSetCombo", ref _hueSetIndex, _hueSetNames, _hueSetNames.Length))
            {
               UpdateHueSetValues();
            }
            if (ImGui.BeginChild("HueSetTable"))
            {
                if (ImGui.BeginTable("HueSetTable", 2) && CEDClient.Running)
                {
                    var clipper = ImGui.ImGuiListClipper();
                    var textSize = ImGui.CalcTextSize(0xFFFF.FormatId());
                    var columnHeight = textSize.Y;
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, textSize.X);
                    var ids = ActiveHueSetValues; //We copy the array here to not crash when removing, please fix :)
                    clipper.Begin(ids.Length, columnHeight);
                    while (clipper.Step())
                    {
                        for (var rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            var hueIndex = ids[rowIndex];
                            DrawHueRow($"{hueIndex.FormatId()}##hueset", ref _selectedId, hueIndex, columnHeight);
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (ImGui.Button(LangManager.Get(REMOVE)))
                                {
                                    RemoveFromHueSet(hueIndex);
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }
                    clipper.End();
                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();
            if (ImGui.BeginDragDropTarget())
            {
                var payloadPtr = ImGui.AcceptDragDropPayload(Hue_DragDrop_Target_Type);
                unsafe
                {
                    if (payloadPtr != ImGuiPayloadPtr.Null)
                    {
                        AddToHueSet(*(ushort*)payloadPtr.Data);
                    }
                }
                ImGui.EndDragDropTarget();
            }
            if (ImGui.BeginPopupModal("NewHueSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text(LangManager.Get(NAME));
                ImGui.SameLine();
                ImGui.InputText("##NewHueSetName", ref _hueSetNewName, 32);
                if (ImGui.Button(LangManager.Get(CREATE)))
                {
                    HueSets.Add(_hueSetNewName, new SortedSet<ushort>());
                    UpdateHueSetNames();
                    _hueSetIndex = Array.IndexOf(_hueSetNames, _hueSetNewName);
                    UpdateHueSetValues();
                    _hueSetNewName = "";
                    ProfileManager.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button(LangManager.Get(CANCEL)))
                {
                    _hueSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal("DeleteHueSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text(string.Format(LangManager.Get(DELETE_WARNING_1TYPE_2NAME), LangManager.Get(HUE_SET), HueSetName));
                if (ImGui.Button(LangManager.Get(YES)))
                {
                    HueSets.Remove(HueSetName);
                    UpdateHueSetNames();
                    _hueSetIndex--;
                    UpdateHueSetValues();
                    ProfileManager.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button(LangManager.Get(NO)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        ImGui.EndChild();
    }
    
    private void DrawHueRow(string label, ref int selectedIndex, int hueIndex, float height)
    {
        var realIndex = hueIndex - 1;
        var texRect = new Rectangle(realIndex % 16 * 32, realIndex / 16, 32, 1);
        
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(1);
        if (realIndex < 0)
            ImGui.TextColored(ImGuiColor.Red, "No Hue");
        else
        {
            CEDGame.UIManager.DrawImage
            (
                HuesManager.Instance.Texture,
                texRect,
                new Vector2(ImGui.GetContentRegionAvail().X, height),
                true
            );
        }
        ImGui.TableSetColumnIndex(0);
        //We draw columns in reverse order, so that this is the last item id that goes out of function 
        if (ImGui.Selectable(label, selectedIndex == hueIndex, ImGuiSelectableFlags.SpanAllColumns))
        {
            selectedIndex = hueIndex;
        }
        if (ImGui.BeginDragDropSource())
        {
            unsafe
            {
                ImGui.SetDragDropPayload(Hue_DragDrop_Target_Type, &hueIndex, sizeof(int));
            }
            var text = $"{hueIndex.FormatId()}: {HuesManager.Instance.Names[hueIndex]}";
            ImGui.Text(text);
            CEDGame.UIManager.DrawImage
            (
                HuesManager.Instance.Texture,
                texRect,
                Vector2.Max(new Vector2(64, 8), ImGui.CalcTextSize(text)),
                true
            );
            ImGui.EndDragDropSource();
        }
        ImGuiEx.Tooltip(HuesManager.Instance.Names[hueIndex]);
    }
    
    private void AddToHueSet(ushort id)
    {
        if (_hueSetIndex == 0)
        {
            _tempHueSetValues.Add(id);
        }
        else
        {
            HueSets[HueSetName].Add(id);
            ProfileManager.Save();
        }
        UpdateHueSetValues();
    }

    private void RemoveFromHueSet(ushort id)
    {
        if (_hueSetIndex == 0)
        {
            _tempHueSetValues.Remove(id);
        }
        else
        {
            HueSets[HueSetName].Remove(id);
            ProfileManager.Save();
        }
        UpdateHueSetValues();
    }

    private void ClearHueSet()
    {
        if (_hueSetIndex == 0)
        {
            _tempHueSetValues.Clear();
        }
        else
        {
            HueSets[HueSetName].Clear();
            ProfileManager.Save();
        }
        UpdateHueSetValues();
    }

    private void UpdateHueSetNames()
    {
        _hueSetNames = HueSets.Keys.Prepend("").ToArray();
    }

    private void UpdateHueSetValues()
    {
        if (_hueSetIndex == 0)
        {
            ActiveHueSetValues = _tempHueSetValues.ToArray();
        }
        else
        {
            ActiveHueSetValues = HueSets[HueSetName].ToArray();
        }
    }

    public void UpdateSelectedHue(StaticObject so)
    {
        _selectedId = so.StaticTile.Hue;
        UpdateScroll = true;
    }
}