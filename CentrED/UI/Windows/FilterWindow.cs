using CentrED.IO.Models;
using Hexa.NET.ImGui;
using static CentrED.Application;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;
using Rectangle = System.Drawing.Rectangle;
using CentrED.IO;


namespace CentrED.UI.Windows;

public class FilterWindow : Window
{

    public FilterWindow()
    {
        CEDClient.Connected += LoadStaticFilterFromProfile;
        CEDClient.Disconnected+= SaveStaticFilterToProfile;
    }
    
    public override string Name => LangManager.Get(FILTER_WINDOW) + "###Filter";
    public override WindowState DefaultState => new()
    {
        IsOpen = true
    };
    private static readonly Vector2 StaticDimensions = new(44, 44);
    private float _tableWidth;
    internal int SelectedId;

    private SortedSet<int> StaticFilterIds => CEDGame.MapManager.StaticFilterIds;

    public static void SaveStaticFilterToProfile()
    {
        ProfileManager.ActiveProfile.StaticFilter = CEDGame.MapManager.StaticFilterIds.ToList();
        ProfileManager.SaveStaticFilter();
    }

    private static void LoadStaticFilterFromProfile()
    {
        var saved = ProfileManager.ActiveProfile.StaticFilter ?? new List<int>();
        CEDGame.MapManager.StaticFilterIds = new SortedSet<int>(saved);
    }

    protected override void InternalDraw()
    {
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
        if (ImGui.BeginChild("Filters"))
        {
            if (ImGui.BeginTabBar("FiltersTabs"))
            {
                if (ImGui.BeginTabItem(LangManager.Get(OBJECTS) + "###StaticsFilter"))
                {
                    ImGui.Checkbox(LangManager.Get(ENABLED), ref CEDGame.MapManager.StaticFilterEnabled);
                    ImGui.Checkbox(LangManager.Get(REVERSED), ref CEDGame.MapManager.StaticFilterInclusive);
                    if (ImGui.Button(LangManager.Get(CLEAR)))
                    {
                        StaticFilterIds.Clear();
                    }
                    if (ImGui.BeginChild("TilesTable"))
                    {
                        if (CEDClient.Running && ImGui.BeginTable("TilesTable", 3))
                        {
                            var clipper = ImGui.ImGuiListClipper();
                            ImGui.TableSetupColumn
                                ("Id", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize(0xFFFF.FormatId()).X);
                            ImGui.TableSetupColumn("Graphic", ImGuiTableColumnFlags.WidthFixed, StaticDimensions.X);
                            _tableWidth = ImGui.GetContentRegionAvail().X;
                            clipper.Begin
                                (StaticFilterIds.Count, StaticDimensions.Y + ImGui.GetStyle().ItemSpacing.Y);
                            while (clipper.Step())
                            {
                                for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                                {
                                    if (row < StaticFilterIds.Count)
                                        DrawStatic(StaticFilterIds.ElementAt(row));
                                }
                            }
                            clipper.End();
                            ImGui.EndTable();
                        }
                    }
                    ImGui.EndChild();

                    if (ImGui.BeginDragDropTarget())
                    {
                        var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.OBJECT_DRAG_DROP_TYPE);
                        unsafe
                        {
                            if (payloadPtr != ImGuiPayloadPtr.Null)
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
                if (ImGui.BeginTabItem(LangManager.Get(HUES)))
                {
                    ImGui.Text("Not implemented :)u8");
                    ImGui.Text("Let me know if you want it to be!u8");
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        ImGui.EndChild();
    }
    
    private void DrawStatic(int index)
    {
        var realIndex = index + TilesWindow.MAX_TERRAIN_INDEX;
        ref var indexEntry = ref CEDGame.MapManager.UoFileManager.Arts.File.GetValidRefEntry(realIndex);
        var arts = CEDGame.MapManager.Arts;
        var spriteInfo = arts.GetArt((uint)(index + indexEntry.AnimOffset));
        var realBounds = arts.GetRealArtBounds((uint)index);
        var bounds = new Rectangle
            (spriteInfo.UV.X + realBounds.X, spriteInfo.UV.Y + realBounds.Y, realBounds.Width, realBounds.Height);
        var name = CEDGame.MapManager.UoFileManager.TileData.StaticData[index].Name;
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
                if (ImGui.Button(LangManager.Get(REMOVE)))
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
            ImGui.Text(index.FormatId());
        }

        if (ImGui.TableNextColumn())
        {
            if (!CEDGame.UIManager.DrawImage(spriteInfo.Texture, bounds, StaticDimensions) && CEDGame.MapManager.DebugLogging)
            {
                ImGui.TextColored(ImGuiColor.Red, LangManager.Get(TEXTURE_NOT_FOUND));
            }
            if (ImGui.IsItemHovered() && (bounds.Width > StaticDimensions.X || bounds.Height > StaticDimensions.Y))
            {
                ImGui.BeginTooltip();
                CEDGame.UIManager.DrawImage(spriteInfo.Texture, bounds);
                ImGui.EndTooltip();
            }
        }

        if (ImGui.TableNextColumn())
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (StaticDimensions.Y - ImGui.GetFontSize()) / 2);
            ImGui.TextUnformatted(name);
        }
    }
}