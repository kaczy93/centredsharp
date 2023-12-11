using ClassicUO.Assets;
using ImGuiNET;
using Microsoft.Xna.Framework;
using static CentrED.Application;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class FilterWindow : Window
{
    public override string Name => "Filter";
    private static readonly Vector2 StaticDimensions = new(44, 44);
    private float _tableWidth;
    internal int SelectedId;

    private SortedSet<int> StaticFilterIds => CEDGame.MapManager.StaticFilterIds;

    protected override void InternalDraw()
    {
        ImGui.SliderInt("Max Z render", ref CEDGame.MapManager.maxZ, -127, 127);
        ImGui.SliderInt("Min Z render", ref CEDGame.MapManager.minZ, -127, 127);
        ImGui.Text("Draw: ");
        ImGui.Checkbox("Land", ref CEDGame.MapManager.ShowLand);
        ImGui.SameLine();
        ImGui.Checkbox("Statics", ref CEDGame.MapManager.ShowStatics);
        ImGui.BeginChild("Filters");
        if(ImGui.BeginTabBar("FiltersTabs")){
            if (ImGui.BeginTabItem("Statics"))
            {
                ImGui.Checkbox("Enabled", ref CEDGame.MapManager.StaticFilterEnabled);
                ImGui.SameLine();
                ImGui.Checkbox("Inclusive", ref CEDGame.MapManager.StaticFilterInclusive);
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    StaticFilterIds.Clear();
                }
                ImGui.BeginChild("TilesTable");
                if (ImGui.BeginTable("TilesTable", 3) && CEDClient.Initialized)
                {
                    unsafe
                    {
                        ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0x0000").X);
                        ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, StaticDimensions.X);
                        _tableWidth = ImGui.GetContentRegionAvail().X;
                        clipper.Begin(StaticFilterIds.Count, StaticDimensions.Y + ImGui.GetStyle().ItemSpacing.Y);
                        while (clipper.Step())
                        {
                            for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                            {
                                if(row < StaticFilterIds.Count)
                                    DrawStatic(StaticFilterIds.ElementAt(row));
                            }
                        }
                        clipper.End();
                    }
                    ImGui.EndTable();
                }
                ImGui.EndChild();
                if (ImGui.BeginDragDropTarget())
                {
                    var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Static_DragDrop_Target_Type);
                    unsafe
                    {
                        if (payloadPtr.NativePtr != null)
                        {
                            var dataPtr = (int*)payloadPtr.Data;
                            int id = dataPtr[0];
                            StaticFilterIds.Add(id);
                        }
                    }
                    ImGui.EndDragDropTarget();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Hues"))
            {
                ImGui.Text("Not implemented :)");
                ImGui.Text("Let me know if you want it to be!");
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        ImGui.EndChild();
    }
    
    private void DrawStatic(int index)
    {
        var realIndex = index + TilesWindow.MaxLandIndex;
        var texture = ArtLoader.Instance.GetStaticTexture((uint)index, out var fakeBounds);
        var realBounds = ArtLoader.Instance.GetRealArtBounds(index);
        var bounds = new Rectangle
            (fakeBounds.X + realBounds.X, fakeBounds.Y + realBounds.Y, realBounds.Width, realBounds.Height);
        var name = TileDataLoader.Instance.StaticData[index].Name;
        ImGui.TableNextRow(ImGuiTableRowFlags.None, StaticDimensions.Y);
        if (ImGui.TableNextColumn())
        {
            var startPos = ImGui.GetCursorPos();
            var selectableSize = new Vector2(_tableWidth, StaticDimensions.Y);
            if (ImGui.Selectable
                (
                    $"##tile{realIndex}",
                    SelectedId == realIndex,
                    ImGuiSelectableFlags.SpanAllColumns,
                    selectableSize
                ))
            {
                SelectedId = realIndex;
            }
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Button("Remove"))
                {
                    StaticFilterIds.Remove(index);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
            ImGui.SetCursorPos
            (
                startPos with
                {
                    Y = startPos.Y + (StaticDimensions.Y - ImGui.GetFontSize()) / 2
                }
            );
            ImGui.Text($"0x{index:X4}");
        }

        if (ImGui.TableNextColumn())
        {
            CEDGame.UIManager.DrawImage(texture, bounds, StaticDimensions);
        }

        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (StaticDimensions.Y - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(name);
        }
    }
}