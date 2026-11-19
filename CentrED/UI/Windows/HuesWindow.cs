using CentrED.Map;
using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;
using Rectangle = System.Drawing.Rectangle;

namespace CentrED.UI.Windows;

public class HuesWindow : Window
{
    public override string Name => App.Lang.Get(HUES_WINDOW) + "###Hues";
    
       
    public HuesWindow() : base(true)
    {
        App.Client.Connected += OnConnected;
        App.Workspace.Changed += OnWorkspaceChanged;
        UpdateHueSetNames();
        UpdateHueSetValues();
    }


    public readonly ConfigValue<Dictionary<string, SortedSet<ushort>>> _HueSets = App.Profile.Active.Init
        ("hue_sets", new Dictionary<string, SortedSet<ushort>>());
    private readonly ConfigValue<string> _filter = App.Workspace.Active.Init("hues.filter_text", "");

    public bool UpdateScroll;
    private ushort _lastSelectedId;

    private MultiSelectStorage<ushort> _selection = new([0]);
    public ICollection<ushort> SelectedIds => _selection.Items;

    private List<ushort> _matchedHueIds = [];
    public const string Hue_DragDrop_Target_Type = "HueDragDrop";
    
    private void OnConnected()
    {
        FilterHues();
        UpdateHueSetNames();
        UpdateHueSetValues();
    }
    
    private void OnWorkspaceChanged()
    {
        if (!App.Client.Running)
            return;
        FilterHues();
        UpdateScroll = true;
    }
    
    private void FilterHues()
    {
        var huesManager = HuesManager.Instance;
        _matchedHueIds.Clear();
        if (_filter.Value.Length == 0)
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
                    name.Contains(_filter.Value, StringComparison.InvariantCultureIgnoreCase) || 
                    i.FormatId(NumberDisplayFormat.HEX).Contains(_filter.Value, StringComparison.InvariantCultureIgnoreCase) || 
                    i.FormatId(NumberDisplayFormat.DEC).Contains(_filter.Value, StringComparison.InvariantCultureIgnoreCase)
                    )
                    _matchedHueIds.Add((ushort)i);
            }
        }
    }

    protected override void InternalDraw()
    {
        if (!App.Client.Running)
        {
            ImGui.Text(App.Lang.Get(NOT_CONNECTED));
            return;
        }
        if (ImGui.Button(App.Lang.Get(SCROLL_TO_SELECTED)))
        {
            UpdateScroll = true;
        }

        ImGui.Text(App.Lang.Get(FILTER));
        if (ImGui.InputText("##Filter", ref _filter.Value, 64))
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
                            if (ImGui.Button(App.Lang.Get(ADD_TO_SET)))
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
                    var itemPosY = (float)clipper.StartPosY + clipper.ItemsHeight * _matchedHueIds.IndexOf(_lastSelectedId);
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

    private int _hueSetRemoveIndex = -1;

    private string[] _hueSetNames = [];

    private void DrawHueSets()
    {
        if (ImGui.BeginChild("HueSets"))
        {
            ImGui.Text(App.Lang.Get(HUE_SET));
            if (ImGui.Button(App.Lang.Get(NEW)))
            {
                ImGui.OpenPopup("NewHueSet");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(_hueSetIndex == 0);
            if (ImGui.Button(App.Lang.Get(DELETE)))
            {
                ImGui.OpenPopup("DeleteHueSet");
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(ActiveHueSetValues.Count == 0);
            if (ImGui.Button(App.Lang.Get(CLEAR)))
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
                if (ImGui.BeginTable("HueSetTable", 2) && App.Client.Running)
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
                                if (ImGui.Button(App.Lang.Get(REMOVE)))
                                {
                                    _hueSetRemoveIndex = hueIndex;
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }
                    _selection.End();
                    ImGui.EndTable();
                }
                if (_hueSetRemoveIndex != -1)
                {
                    RemoveFromHueSet((ushort)_hueSetRemoveIndex);
                    _hueSetRemoveIndex = -1;
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
                ImGui.Text(App.Lang.Get(NAME));
                ImGui.SameLine();
                ImGui.InputText("##NewHueSetName", ref _hueSetNewName, 32);
                ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_hueSetNewName) || _hueSetNames.Contains(_hueSetNewName));
                if (ImGui.Button(App.Lang.Get(CREATE)))
                {
                    _HueSets.Value.Add(_hueSetNewName, new SortedSet<ushort>());
                    UpdateHueSetNames();
                    _hueSetIndex = Array.IndexOf(_hueSetNames, _hueSetNewName);
                    UpdateHueSetValues();
                    _hueSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndDisabled();
                ImGui.SameLine();
                if (ImGui.Button(App.Lang.Get(CANCEL)))
                {
                    _hueSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal("DeleteHueSet", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.Text(string.Format(App.Lang.Get(DELETE_WARNING_1TYPE_2NAME), App.Lang.Get(HUE_SET), HueSetName));
                if (ImGui.Button(App.Lang.Get(YES)))
                {
                    _HueSets.Value.Remove(HueSetName);
                    _hueSetIndex--;
                    UpdateHueSetNames();
                    UpdateHueSetValues();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button(App.Lang.Get(NO)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        ImGui.EndChild();
    }
    
    public void DrawHueRow(int rowIndex, ushort hueIndex, float height)
    {
        var realIndex = hueIndex - 1;
        var texRect = new Rectangle(realIndex % 16 * 32, realIndex / 16, 32, 1);
        
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(1);
        if (realIndex < 0)
            ImGui.TextColored(ImGuiColor.Red, "No Hue");
        else
        {
            App.UIManager.DrawImage
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
            _HueSets.Value[HueSetName].Add(id);
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
            _HueSets.Value[HueSetName].Remove(id);
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
            _HueSets.Value[HueSetName].Clear();
        }
        UpdateHueSetValues();
    }

    private void UpdateHueSetNames()
    {
        _hueSetNames = _HueSets.Value.Keys.Prepend("").ToArray();
        _hueSetIndex = Math.Clamp(_hueSetIndex, 0, _HueSets.Value.Count);
    }

    private void UpdateHueSetValues()
    {
        if (_hueSetIndex == 0)
        {
            ActiveHueSetValues = _tempHueSetValues.ToList();
        }
        else
        {
            ActiveHueSetValues = _HueSets.Value[HueSetName].ToList();
        }
    }

    public void UpdateSelection(StaticObject so)
    {
        _selection.SetSelection(so.StaticTile.Hue);
        _lastSelectedId = so.StaticTile.Hue;
        UpdateScroll = true;
    }
}