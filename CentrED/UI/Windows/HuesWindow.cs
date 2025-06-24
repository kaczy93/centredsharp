using CentrED.IO;
using CentrED.IO.Models;
using CentrED.Map;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class HuesWindow : Window
{
    private static readonly Random _random = new();
    public HuesWindow()
    {
        CEDClient.Connected += FilterHues;
    }

    public override string Name => "Hues";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    public bool UpdateScroll;
    private string _filter = "";
    public int SelectedId { get; set; }
    public ushort ActiveId =>
        ActiveHueSetValues.Length > 0 ? ActiveHueSetValues[_random.Next(ActiveHueSetValues.Length)] : (ushort)SelectedId;

    private const int _hueRowHeight = 20;
    private static readonly int _totalHuesRowHeight = _hueRowHeight + (int)ImGui.GetStyle().ItemSpacing.Y;
    private int[] _matchedHueIds;
    public const string Hue_DragDrop_Target_Type = "HueDragDrop";


    private void FilterHues()
    {
        var huesManager = HuesManager.Instance;
        if (_filter.Length == 0)
        {
            _matchedHueIds = new int[huesManager.HuesCount];
            for (int i = 0; i < huesManager.HuesCount; i++)
            {
                _matchedHueIds[i] = i;
            }
        }
        else
        {
            var matchedIds = new List<int>();
            for (int i = 0; i < huesManager.HuesCount; i++)
            {
                var name = huesManager.Names[i];
                if (name.Contains(_filter) || $"{i}".Contains(_filter) || $"0x{i:X4}".Contains(_filter))
                    matchedIds.Add(i);
            }
            _matchedHueIds = matchedIds.ToArray();
        }
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text("Not connected");
            return;
        }
        if (ImGui.Button("Scroll to selected"))
        {
            UpdateScroll = true;
        }

        ImGui.Text("Filter");
        if (ImGui.InputText("##Filter", ref _filter, 64))
        {
            FilterHues();
        }
        DrawHues();
        DrawHueSets();
    }

    private void DrawHues()
    {
        if (ImGui.BeginChild
                ("Hues", new Vector2(), ImGuiChildFlags.Borders | ImGuiChildFlags.ResizeY, ImGuiWindowFlags.Modal))
        {
            if (ImGui.BeginTable("HuesTable", 2) && CEDClient.Running)
            {
                unsafe
                {
                    ImGuiListClipperPtr clipper = new ImGuiListClipperPtr
                        (ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                    ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                    var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, _hueRowHeight);
                    clipper.Begin(_matchedHueIds.Length, _totalHuesRowHeight);
                    while (clipper.Step())
                    {
                        for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                        {
                            var hueIndex = _matchedHueIds[rowIndex];
                            var posY = ImGui.GetCursorPosY();
                            DrawHueRow(hueIndex);
                            ImGui.SetCursorPosY(posY);
                            if (ImGui.Selectable
                                (
                                    $"##hue{hueIndex}",
                                    SelectedId == hueIndex,
                                    ImGuiSelectableFlags.SpanAllColumns,
                                    selectableSize
                                ))
                            {
                                SelectedId = hueIndex;
                            }
                            UIManager.Tooltip(HuesManager.Instance.Names[hueIndex]);
                            if (ImGui.BeginPopupContextItem())
                            {
                                if (_hueSetIndex != 0 && ImGui.Button("Add to set"))
                                {
                                    AddToHueSet((ushort)hueIndex);
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndPopup();
                            }
                            if (ImGui.BeginDragDropSource())
                            {
                                ImGui.SetDragDropPayload(Hue_DragDrop_Target_Type, (IntPtr)(&hueIndex), sizeof(int));
                                ImGui.Text(HuesManager.Instance.Names[hueIndex]);
                                CEDGame.UIManager.DrawImage
                                (
                                    HuesManager.Instance.Texture,
                                    new Rectangle(0, hueIndex - 1, 32, 1),
                                    new Vector2(60, _hueRowHeight),
                                    true
                                );
                                ImGui.EndDragDropSource();
                            }
                        }
                    }
                    clipper.End();
                    if (UpdateScroll)
                    {
                        float itemPosY = clipper.StartPosY + _totalHuesRowHeight * Array.IndexOf
                            (_matchedHueIds, SelectedId);
                        ImGui.SetScrollFromPosY(itemPosY - ImGui.GetWindowPos().Y);
                        UpdateScroll = false;
                    }
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private bool _hueSetShowPopupNew;
    private bool _hueSetShowPopupDelete;
    private int _hueSetIndex;
    private string _hueSetName = "";
    private string _hueSetNewName = "";
    private static readonly ushort[] Empty = Array.Empty<ushort>();
    private ushort[] ActiveHueSetValues = Empty;
    private int _hueSetSelectedId;

    private void DrawHueSets()
    {
        if (ImGui.BeginChild("HueSets"))
        {
            ImGui.Text("Hue Set");
            if (ImGui.Button("New"))
            {
                ImGui.OpenPopup("NewHueSet");
                _hueSetShowPopupNew = true;
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(_hueSetIndex == 0);
            if (ImGui.Button("Delete"))
            {
                ImGui.OpenPopup("DeleteHueSet");
                _hueSetShowPopupDelete = true;
            }
            ImGui.EndDisabled();
            var hueSets = ProfileManager.ActiveProfile.HueSets;
            //Probably slow, optimize
            var names = new[] { String.Empty }.Concat(hueSets.Keys).ToArray();
            if (ImGui.Combo("##HueSetCombo", ref _hueSetIndex, names, names.Length))
            {
                _hueSetName = names[_hueSetIndex];
                if (_hueSetIndex == 0)
                {
                    ActiveHueSetValues = Empty;
                }
                else
                {
                    ActiveHueSetValues = hueSets[_hueSetName].ToArray();
                }
            }
            if (ImGui.BeginChild("HueSetTable"))
            {
                if (ImGui.BeginTable("HueSetTable", 2) && CEDClient.Running)
                {
                    unsafe
                    {
                        ImGuiListClipperPtr clipper = new ImGuiListClipperPtr
                            (ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                        var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, _hueRowHeight);
                        var ids = ActiveHueSetValues; //We copy the array here to not crash when removing, please fix :)
                        clipper.Begin(ids.Length, _totalHuesRowHeight);
                        while (clipper.Step())
                        {
                            for (int rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                            {
                                var hueIndex = ids[rowIndex];
                                var posY = ImGui.GetCursorPosY();
                                DrawHueRow(hueIndex);
                                ImGui.SetCursorPosY(posY);
                                if (ImGui.Selectable
                                    (
                                        $"##hueset{hueIndex}",
                                        _hueSetSelectedId == hueIndex,
                                        ImGuiSelectableFlags.SpanAllColumns,
                                        selectableSize
                                    ))
                                {
                                    _hueSetSelectedId = hueIndex;
                                }
                                UIManager.Tooltip(HuesManager.Instance.Names[hueIndex]);
                                if (ImGui.BeginPopupContextItem())
                                {
                                    if (ImGui.Button("Remove"))
                                    {
                                        RemoveFromHueSet(hueIndex);
                                        ImGui.CloseCurrentPopup();
                                    }
                                    ImGui.EndPopup();
                                }
                            }
                        }
                        clipper.End();
                    }
                    ImGui.EndTable();
                }
            }
            ImGui.EndChild();
            if (_hueSetIndex != 0 && ImGui.BeginDragDropTarget())
            {
                var payloadPtr = ImGui.AcceptDragDropPayload(Hue_DragDrop_Target_Type);
                unsafe
                {
                    if (payloadPtr.NativePtr != null)
                    {
                        var dataPtr = (int*)payloadPtr.Data;
                        int id = dataPtr[0];
                        AddToHueSet((ushort)id);
                    }
                }
                ImGui.EndDragDropTarget();
            }
            if (ImGui.BeginPopupModal
                (
                    "NewHueSet",
                    ref _hueSetShowPopupNew,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text("Name");
                ImGui.SameLine();
                ImGui.InputText("##NewHueSetName", ref _hueSetNewName, 32);
                if (ImGui.Button("Add"))
                {
                    hueSets.Add(_hueSetNewName, new SortedSet<ushort>());
                    _hueSetIndex = Array.IndexOf(hueSets.Keys.ToArray(), _hueSetNewName) + 1;
                    _hueSetName = _hueSetNewName;
                    ActiveHueSetValues = Empty;
                    ProfileManager.Save();
                    _hueSetNewName = "";
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            if (ImGui.BeginPopupModal
                (
                    "DeleteHueSet",
                    ref _hueSetShowPopupDelete,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar
                ))
            {
                ImGui.Text($"Are you sure you want to delete tile set '{_hueSetName}'?");
                if (ImGui.Button("Yes"))
                {
                    hueSets.Remove(_hueSetName);
                    ProfileManager.Save();
                    _hueSetIndex--;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("No"))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        ImGui.EndChild();
    }
    
    private void DrawHueRow(int index)
    {
        var name = HuesManager.Instance.Names[index];

        ImGui.TableNextRow(ImGuiTableRowFlags.None, _hueRowHeight);
        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (_hueRowHeight - ImGui.GetFontSize()) / 2); //center vertically
            ImGui.Text($"0x{index:X4}");
        }
        if (ImGui.TableNextColumn())
        {
            if (index == 0)
                ImGui.TextColored(UIManager.Red, name);
            else
            {
                var realIndex = index - 1;
                CEDGame.UIManager.DrawImage
                (
                    HuesManager.Instance.Texture,
                    new Rectangle(realIndex % 16 * 32, realIndex / 16, 32, 1),
                    new Vector2(ImGui.GetContentRegionAvail().X, _hueRowHeight),
                    true
                );
            }
        }
    }
    
    private void AddToHueSet(ushort id)
    {
        var tileSet = ProfileManager.ActiveProfile.HueSets[_hueSetName];
        tileSet.Add(id);
        ActiveHueSetValues = tileSet.ToArray();
        ProfileManager.Save();
    }

    private void RemoveFromHueSet(ushort id)
    {
        var tileSet = ProfileManager.ActiveProfile.HueSets[_hueSetName];
        tileSet.Remove(id);
        ActiveHueSetValues = tileSet.ToArray();
        ProfileManager.Save();
    }

    public void UpdateSelectedHue(StaticObject so)
    {
        SelectedId = so.StaticTile.Hue;
        UpdateScroll = true;
    }
}