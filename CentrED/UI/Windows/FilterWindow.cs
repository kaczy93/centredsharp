using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class FilterWindow() : Window(true)
{
    public override string Name => LangManager.Get(FILTER_WINDOW) + "###Filter";

    private Vector2 StaticDimensions => TilesWindow.TilesDimensions;

    private SortedSet<int> ObjectIdFilter => App.MapManager.ObjectIdFilter.Value;
    private SortedSet<int> ObjectHueFilter => App.MapManager.ObjectHueFilter.Value;

    private TilesWindow _tilesWindow => App.UIManager.GetWindow<TilesWindow>(); 
    private HuesWindow _huesWindow => App.UIManager.GetWindow<HuesWindow>(); 

    protected override void InternalDraw()
    {
        if (!App.Client.Running)
        {
            ImGui.Text(LangManager.Get(NOT_CONNECTED));
            return;
        }
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8);
        ImGui.BeginGroup();
        if (ImGuiEx.DragInt(LangManager.Get(MAX) + " Z", ref App.MapManager.MaxZ.Value, 1, App.MapManager.MinZ.Value, 127))
        {
            App.MapManager.UpdateLights();
        }
        if (ImGuiEx.DragInt(LangManager.Get(MIN) + " Z", ref App.MapManager.MinZ.Value, 1, -128, App.MapManager.MaxZ.Value))
        {
            App.MapManager.UpdateLights();
        }
        ImGui.EndGroup();
        ImGui.Text(LangManager.Get(GLOBAL_FILTER));
        ImGui.Checkbox(LangManager.Get(LAND), ref App.MapManager.ShowLand.Value);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(OBJECTS), ref App.MapManager.ShowStatics.Value);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(NODRAW), ref App.MapManager.ShowNoDraw.Value);

        ImGui.Checkbox(LangManager.Get(WALL), ref App.MapManager.ShowWall.Value);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(WINDOW), ref App.MapManager.ShowWindow.Value);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(ROOF), ref App.MapManager.ShowRoof.Value);

        ImGui.Checkbox(LangManager.Get(SURFACE), ref App.MapManager.ShowSurface.Value);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(WATER), ref App.MapManager.ShowWater.Value);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(FOLIAGE), ref App.MapManager.ShowFoliage.Value);
        
        ImGui.Checkbox(LangManager.Get(STAIRS), ref App.MapManager.ShowStairs.Value);

        if (ImGui.BeginChild("Filters"))
        {
            if (ImGui.BeginTabBar("FiltersTabs"))
            {
                if (ImGui.BeginTabItem(LangManager.Get(OBJECTS)))
                {
                    ImGui.Checkbox(LangManager.Get(ENABLED), ref App.MapManager.ObjectIdFilterEnabled.Value);
                    ImGui.Checkbox(LangManager.Get(REVERSED), ref App.MapManager.ObjectIdFilterInclusive.Value);
                    if (ImGui.Button(LangManager.Get(CLEAR)))
                    {
                        ObjectIdFilter.Clear();
                    }
                    if (ImGui.BeginChild("TilesTable"))
                    {
                        if (ImGui.BeginTable("TilesTable", 3))
                        {
                            var tileToRemove = -1;
                            var clipper = ImGui.ImGuiListClipper();
                            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(0xFFFF.FormatId()).X);
                            ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, StaticDimensions.X);
                            clipper.Begin(ObjectIdFilter.Count);
                            while (clipper.Step())
                            {
                                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                                {
                                    var tileIndex = ObjectIdFilter.ElementAt(i);
                                    var tileInfo = _tilesWindow.GetObjectInfo(tileIndex);
                                    _tilesWindow.DrawTileRow(i, (ushort)tileIndex, tileInfo);
                                    if (ImGui.BeginPopupContextItem())
                                    {
                                        if (ImGui.Button(LangManager.Get(REMOVE)))
                                        {
                                            tileToRemove = tileIndex;
                                            ImGui.CloseCurrentPopup();
                                        }
                                        ImGui.EndPopup();
                                    }
                                }
                            }
                            if (tileToRemove != -1)
                                ObjectIdFilter.Remove(tileToRemove);
                            ImGui.EndTable();
                        }
                    }
                    ImGui.EndChild();
                    if (ImGuiEx.DragDropTarget(TilesWindow.OBJECT_DRAG_DROP_TYPE, out var ids))
                    {
                        foreach (var id in ids)
                        {
                            ObjectIdFilter.Add(id);
                        }
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem(LangManager.Get(HUES)))
                {
                    ImGui.Checkbox(LangManager.Get(ENABLED), ref App.MapManager.ObjectHueFilterEnabled.Value);
                    ImGui.Checkbox(LangManager.Get(REVERSED), ref App.MapManager.ObjectHueFilterInclusive.Value);
                    if (ImGui.Button(LangManager.Get(CLEAR)))
                    {
                        ObjectHueFilter.Clear();
                    }
                    if (ImGui.BeginChild("HuesTable"))
                    {
                        if (ImGui.BeginTable("HuesTable", 2))
                        {
                            var hueToRemove = -1;
                            var clipper = ImGui.ImGuiListClipper();
                            var textSize = ImGui.CalcTextSize(0xFFFF.FormatId());
                            var columnHeight = textSize.Y;
                            ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, textSize.X);
                            clipper.Begin(ObjectHueFilter.Count);
                            while (clipper.Step())
                            {
                                for (var rowIndex = clipper.DisplayStart; rowIndex < clipper.DisplayEnd; rowIndex++)
                                {
                                    var hueIndex = ObjectHueFilter.ElementAt(rowIndex);
                                    _huesWindow.DrawHueRow(rowIndex, (ushort)hueIndex, columnHeight);
                                    if (ImGui.BeginPopupContextItem())
                                    {
                                        if (ImGui.Button(LangManager.Get(REMOVE)))
                                        {
                                            hueToRemove = hueIndex;
                                            ImGui.CloseCurrentPopup();
                                        }
                                        ImGui.EndPopup();
                                    }
                                }
                            }
                            if(hueToRemove != -1)
                                ObjectHueFilter.Remove(hueToRemove);
                            ImGui.EndTable();
                        }
                    }
                    ImGui.EndChild();

                    if (ImGuiEx.DragDropTarget(HuesWindow.Hue_DragDrop_Target_Type, out var ids))
                    {
                        foreach (var id in ids)
                        {
                            ObjectHueFilter.Add(id);
                        }
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        ImGui.EndChild();
    }
}
