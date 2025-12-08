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

    private ushort _lastSelectedId;

    private MultiSelectStorage<ushort> _selection = new([0]);
    public ICollection<ushort> SelectedIds => _selection.Items;

    private List<ushort> _matchedHueIds = [];
    public const string Hue_DragDrop_Target_Type = "HueDragDrop";
    
    private void FilterHues()
    {
        var huesManager = HuesManager.Instance;
        if (_filter.Length == 0)
        {
            for (var i = 0; i < huesManager.HuesCount; i++)
            {
                _matchedHueIds.Add((ushort)i);
            }
        }
        else
        {
            for (var i = 0; i < huesManager.HuesCount; i++)
            {
                var name = huesManager.Names[i];
                if (
                    name.Contains(_filter, StringComparison.InvariantCultureIgnoreCase) || 
                    i.FormatId(NumberDisplayFormat.HEX).Contains(_filter, StringComparison.InvariantCultureIgnoreCase) || 
                    i.FormatId(NumberDisplayFormat.DEC).Contains(_filter, StringComparison.InvariantCultureIgnoreCase)
                    )
                    _matchedHueIds.Add((ushort)i);
            }
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
                clipper.Begin(_matchedHueIds.Count);
                _selection.Begin(_matchedHueIds, clipper, ImGuiMultiSelectFlags.BoxSelect1D);
                while (clipper.Step())
                {
                    for (var rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                    {
                        var hueIndex = _matchedHueIds[rowIndex];
                        DrawHueRow(rowIndex, hueIndex, columnHeight);
                        ImGuiEx.DragDropSource(Hue_DragDrop_Target_Type, _selection.Items);
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
                _selection.End();
                if (UpdateScroll)
                {
                    var itemPosY = (float)clipper.StartPosY + columnHeight * _matchedHueIds.IndexOf(_lastSelectedId);
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
    public List<ushort> ActiveHueSetValues = [];

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
            ImGui.BeginDisabled(ActiveHueSetValues.Count == 0);
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
                    clipper.Begin(ids.Count);
                    _selection.Begin(ids, clipper, ImGuiMultiSelectFlags.BoxSelect1D);
                    while (clipper.Step())
                    {
                        for (var rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            var hueIndex = ids[rowIndex];
                            DrawHueRow(rowIndex, hueIndex, columnHeight);
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
                    _selection.End();
                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();
            if(ImGuiEx.DragDropTarget(Hue_DragDrop_Target_Type, out var hueIds))
            {
                foreach (var id in hueIds)
                {
                    AddToHueSet(id);
                }
            }
            if (ImGui.BeginPopupModal("NewHueSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text(LangManager.Get(NAME));
                ImGui.SameLine();
                ImGui.InputText("##NewHueSetName", ref _hueSetNewName, 32);
                ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_hueSetNewName) || _hueSetNames.Contains(_hueSetNewName));
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
                ImGui.EndDisabled();
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
    
    private void DrawHueRow(int rowIndex, ushort hueIndex, float height)
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
        var selected = _selection.Contains(hueIndex);
        ImGui.SetNextItemSelectionUserData(rowIndex);
        if (ImGui.Selectable($"{hueIndex.FormatId()}", selected, ImGuiSelectableFlags.SpanAllColumns))
        {
            _lastSelectedId = hueIndex;
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
            ActiveHueSetValues = _tempHueSetValues.ToList();
        }
        else
        {
            ActiveHueSetValues = HueSets[HueSetName].ToList();
        }
    }

    public void ScrollToID(StaticObject so)
    {
        _lastSelectedId = so.StaticTile.Hue;
        UpdateScroll = true;
    }
}