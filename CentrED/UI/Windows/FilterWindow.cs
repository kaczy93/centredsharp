using CentrED.IO.Models;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;
using CentrED.IO;


namespace CentrED.UI.Windows;

public class FilterWindow : Window
{
    public FilterWindow()
    {
        CEDClient.Connected += OnConnected;
        CEDClient.Disconnected += OnDisconnected;
    }
    
    public override string Name => LangManager.Get(FILTER_WINDOW) + "###Filter";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };

    private Vector2 StaticDimensions => TilesWindow.TilesDimensions;

    private SortedSet<int> ObjectIdFilter => CEDGame.MapManager.ObjectIdFilter;
    private SortedSet<int> ObjectHueFilter => CEDGame.MapManager.ObjectHueFilter;

    private TilesWindow _tilesWindow => CEDGame.UIManager.GetWindow<TilesWindow>(); 
    private HuesWindow _huesWindow => CEDGame.UIManager.GetWindow<HuesWindow>(); 

    private static void OnDisconnected()
    {
        ProfileManager.ActiveProfile.StaticFilter = CEDGame.MapManager.ObjectIdFilter.ToList();
        ProfileManager.SaveStaticFilter();
    }

    private static void OnConnected()
    {
        CEDGame.MapManager.ObjectIdFilter = new SortedSet<int>(ProfileManager.ActiveProfile.StaticFilter);
    }

    protected override void InternalDraw()
    {
        if (!CEDClient.Running)
        {
            ImGui.Text(LangManager.Get(NOT_CONNECTED));
            return;
        }
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8);
        ImGui.BeginGroup();
        if (ImGuiEx.DragInt(LangManager.Get(MAX) + " Z", ref CEDGame.MapManager.MaxZ, 1, CEDGame.MapManager.MinZ, 127))
        {
            CEDGame.MapManager.UpdateLights();
        }
        if (ImGuiEx.DragInt(LangManager.Get(MIN) + " Z", ref CEDGame.MapManager.MinZ, 1, -128, CEDGame.MapManager.MaxZ))
        {
            CEDGame.MapManager.UpdateLights();
        }
        ImGui.EndGroup();
        ImGui.Text(LangManager.Get(GLOBAL_FILTER));
        ImGui.Checkbox(LangManager.Get(LAND), ref CEDGame.MapManager.ShowLand);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(OBJECTS), ref CEDGame.MapManager.ShowStatics);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(NODRAW), ref CEDGame.MapManager.ShowNoDraw);

        ImGui.Checkbox(LangManager.Get(WALL), ref CEDGame.MapManager.ShowWall);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(WINDOW), ref CEDGame.MapManager.ShowWindow);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(ROOF), ref CEDGame.MapManager.ShowRoof);

        ImGui.Checkbox(LangManager.Get(SURFACE), ref CEDGame.MapManager.ShowSurface);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(WATER), ref CEDGame.MapManager.ShowWater);
        ImGui.SameLine();
        ImGui.Checkbox(LangManager.Get(FOLIAGE), ref CEDGame.MapManager.ShowFoliage);


        ImGui.Checkbox(LangManager.Get(STAIRS), ref CEDGame.MapManager.ShowStairs);

        if (ImGui.BeginChild("Filters"))
        {
            if (ImGui.BeginTabBar("FiltersTabs"))
            {
                if (ImGui.BeginTabItem(LangManager.Get(OBJECTS)))
                {
                    ImGui.Checkbox(LangManager.Get(ENABLED), ref CEDGame.MapManager.ObjectIdFilterEnabled);
                    ImGui.Checkbox(LangManager.Get(REVERSED), ref CEDGame.MapManager.ObjectIdFilterInclusive);
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
                    ImGui.Checkbox(LangManager.Get(ENABLED), ref CEDGame.MapManager.ObjectHueFilterEnabled);
                    ImGui.Checkbox(LangManager.Get(REVERSED), ref CEDGame.MapManager.ObjectHueFilterInclusive);
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