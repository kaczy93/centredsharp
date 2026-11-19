using System.Numerics;
using CentrED.IO.Models;
using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.IO.Models.Direction;
using static CentrED.LangEntry;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class LandBrushManagerWindow : Window
{
    public override string Name => App.Lang.Get(LANDBRUSH_MANAGER_WINDOW) + "###LandBrush Manager";

    public static readonly Vector2 FullSize = new(44, 44);
    public static readonly Vector2 HalfSize = FullSize / 2;

    private string? _tilesBrushPath = "TilesBrush.xml";
    private string _importStatusText = "";
    private bool _importSuccess;

    private string _landBrushNewName = "";
    private string _selectedTransitionBrushName = "";
    private LandBrush? _Active => App.LandBrush.Active;
    private string _ActiveName => App.LandBrush.ActiveName;
    private IReadOnlyDictionary<string, LandBrush> _brushes => App.LandBrush.Brushes;

    private static readonly Vector2 ComboFramePadding = ImGui.GetStyle().FramePadding with{ Y = (float)((HalfSize.Y - ImGui.GetTextLineHeight()) * 0.5) };

    protected override void InternalDraw()
    {
        if (!App.Client.Running)
        {
            ImGui.Text(App.Lang.Get(NOT_CONNECTED));
            return;
        }
        
        DrawImport();

        ImGui.Separator();
        
        ImGui.Columns(2);
        if(ImGui.BeginChild("Brushes"))
        {
            ImGui.Text(App.Lang.Get(LAND_BRUSH));
            if (LandBrushCombo(ref App.LandBrush.ActiveName))
            {
                _selectedTransitionBrushName = _Active.To.Keys.FirstOrDefault("") ?? "";
            }
            if (ImGui.Button(App.Lang.Get(NEW)))
            {
                ImGui.OpenPopup("LandBrushAdd");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(_brushes.Count <= 0);
            if (ImGui.Button(App.Lang.Get(DELETE)))
            {
                ImGui.OpenPopup("LandBrushDelete");
            }
            ImGui.EndDisabled();
            ImGui.Separator();
            DrawFullTiles();
            DrawBrushPopups();
        }
        ImGui.EndChild();
        ImGui.NextColumn();
        if(ImGui.BeginChild("Transitions"))
        {
            DrawTransitions();
            DrawTransitionPopups();
        }
        ImGui.EndChild();
    }

    public void DrawPreview(string name)
    {
        DrawPreview(name, HalfSize);
    }

    public void DrawPreview(string name, Vector2 size)
    {
        if (_brushes.TryGetValue(name, out var brush))
        {
            if (brush.Tiles.Count > 0)
            {
                DrawTile(brush.Tiles[0], size);
            }
            else
            {
                ImGui.Dummy(size);
            } 
        }
        else
        {
            ImGui.Dummy(size);
        }
    }

    private void DrawTile(int id, Vector2 size)
    {
        var spriteInfo = App.MapManager.Texmaps.GetTexmap(App.MapManager.UoFileManager.TileData.LandData[id].TexID);
        if (spriteInfo.Texture != null)
        {
            App.UIManager.DrawImage(spriteInfo.Texture, spriteInfo.UV, size, true);
        }
        else
        {
            ImGui.Dummy(size);
        }
    }

    public bool LandBrushCombo(ref string selectedName)
    {
        return LandBrushCombo("##landBrush", _brushes, ref selectedName);
    }

    private bool LandBrushCombo<T>(string id, IReadOnlyDictionary<string, T> dictionary, ref string selectedName, ImGuiComboFlags flags = ImGuiComboFlags.HeightLarge)
    {
        var result = false;
        var names = dictionary.Keys.ToArray();
        DrawPreview(selectedName);
        ImGui.SameLine();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ComboFramePadding);
        if(ImGui.BeginCombo(id, selectedName, flags))
        {
            foreach (var name in names)
            {
                var is_selected = name == selectedName;
                DrawPreview(name);
                ImGui.SameLine();
                if (ImGui.Selectable(name, is_selected, ImGuiSelectableFlags.None, HalfSize with { X = 0 }))
                {
                    result = true;
                    selectedName = name;
                }
                if (is_selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.PopStyleVar();
        ImGui.PopItemWidth();
        return result;
    }

    private void DrawFullTiles()
    {
        if (_Active == null)
            return;
        foreach (var fullTile in _Active.Tiles.ToArray())
        {
            DrawTile(fullTile, FullSize);
            ImGuiEx.Tooltip(fullTile.FormatId());
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
            if (ImGui.SmallButton($"x##{fullTile}"))
            {
                _Active.Tiles.Remove(fullTile);
                App.LandBrush.RemoveLandBrushEntry(fullTile, _ActiveName, _ActiveName);
            }
            ImGui.PopStyleColor(2);
            ImGui.Text(fullTile.FormatId());
            ImGui.EndGroup();
        }
        ImGui.Button("+##AddFullTile", FullSize);
        ImGuiEx.Tooltip(App.Lang.Get(DRAG_AND_DROP_TILE_HERE));
        if (ImGuiEx.DragDropTarget(TilesWindow.TERRAIN_DRAG_DROP_TYPE, out var ids))
        {
            foreach (var id in ids)
            {
                if(!_Active.Tiles.Contains(id))
                {
                    _Active.Tiles.Add(id);
                    App.LandBrush.AddLandBrushEntry(id, _ActiveName, _ActiveName);
                }
            }
        }
    }

    private void DrawTransitions()
    {
        if (_Active == null)
            return;
        if (!_Active.To.ContainsKey(_selectedTransitionBrushName))
            _selectedTransitionBrushName = _Active.To.Keys.FirstOrDefault("");
        ImGui.Text(App.Lang.Get(TRANSITIONS));
        LandBrushCombo("transitions", _Active.To, ref _selectedTransitionBrushName);
        if (ImGui.Button(App.Lang.Get(NEW)))
        {
            ImGui.OpenPopup("TransitionsAdd");
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(_Active.To.Count == 0);
        if (ImGui.Button(App.Lang.Get(DELETE)))
        {
            ImGui.OpenPopup("TransitionsDelete");
        }
        ImGui.EndDisabled();
        ImGui.Separator();
        
        if(_Active.To.Count == 0)
            return;
        
        var targetBrush = _brushes[_selectedTransitionBrushName];
        if(_Active.Tiles.Count == 0 || targetBrush.Tiles.Count == 0)
        {
            ImGui.Text(App.Lang.Get(MISSING_FULL_TILES_WARNING));
            return;
        }
        var sourceTexture = CalculateButtonTexture(_Active.Tiles[0]);
        var targetTexture = CalculateButtonTexture(targetBrush.Tiles[0]);
        var transitions = _Active.To[_selectedTransitionBrushName];
        foreach (var transition in transitions.ToArray())
        {
            var tileId = transition.TileID;
            DrawTile(tileId, FullSize);
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
            if (ImGui.SmallButton($"x##{transition.TileID}"))
            {
                transitions.Remove(transition);
                App.LandBrush.RemoveLandBrushEntry(transition.TileID, _ActiveName, _selectedTransitionBrushName);
            }
            ImGui.PopStyleColor(2);
            ImGui.Text(tileId.FormatId());            
            ImGui.EndGroup();
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.One);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ToggleDirButton(transition, Up, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, North, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, Right, sourceTexture, targetTexture);
            ToggleDirButton(transition, West, sourceTexture, targetTexture);
            ImGui.SameLine();
            unsafe
            {
                ImGui.Image(new ImTextureRef(null, sourceTexture.texPtr), new Vector2(11, 11), sourceTexture.uv0, sourceTexture.uv1);
            }
            ImGui.SameLine();
            ToggleDirButton(transition, East, sourceTexture, targetTexture);
            ToggleDirButton(transition, Left, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, South, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, Down, sourceTexture, targetTexture);
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(2);
            ImGui.EndGroup();
        }
        ImGui.Button("+##AddTransition", FullSize);
        ImGuiEx.Tooltip(App.Lang.Get(DRAG_AND_DROP_TILE_HERE));
        if (ImGuiEx.DragDropTarget(TilesWindow.TERRAIN_DRAG_DROP_TYPE, out var ids))
        {
            foreach (var id in ids)
            {
                if(transitions.All(t => t.TileID != id))
                {
                    transitions.Add(new LandBrushTransition(id));
                    App.LandBrush.AddLandBrushEntry(id, _ActiveName, _selectedTransitionBrushName);
                }
            }
        }
    }

    private unsafe void ToggleDirButton(LandBrushTransition transition, Direction dir, (ImTextureID texPtr, Vector2 uv0, Vector2 uv1) sourceTexture, (ImTextureID texPtr, Vector2 uv0, Vector2 uv1) targetTexture)
    {
        var isSet = transition.Direction.Contains(dir);
        var tex = isSet ? targetTexture : sourceTexture;
        if (ImGui.ImageButton($"{transition.TileID}{dir}", new ImTextureRef(null, tex.texPtr), new Vector2(11,11), tex.uv0, tex.uv1))
        {
            if (isSet)
            {
                transition.Direction &= ~dir;
            }
            else
            {
                transition.Direction |= dir;
            }
        }
        ImGuiEx.Tooltip(isSet ? _selectedTransitionBrushName : _ActiveName);
    }

    private (ImTextureID texPtr, Vector2 uv0, Vector2 uv1) CalculateButtonTexture(ushort tileId)
    {
        var spriteInfo = App.MapManager.Texmaps.GetTexmap(App.MapManager.UoFileManager.TileData.LandData[tileId].TexID);
        if (spriteInfo.Texture == null)
        {
            //Fallback to VOID
            spriteInfo = App.MapManager.Texmaps.GetTexmap(0x0001);
        }
        var tex = spriteInfo.Texture;
        var bounds = spriteInfo.UV;
        var texPtr = App.UIManager._uiRenderer.BindTexture(tex);
        var fWidth = (float)tex.Width;
        var fHeight = (float)tex.Height;
        var uv0 = new Vector2(bounds.X / fWidth, bounds.Y / fHeight);
        var uv1 = new Vector2((bounds.X + bounds.Width) / fWidth, (bounds.Y + bounds.Height) / fHeight);
        return (texPtr, uv0, uv1);
    }

    private void DrawBrushPopups()
    {
        if (ImGui.BeginPopupModal("LandBrushAdd", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.InputText(App.Lang.Get(NAME), ref _landBrushNewName, 64);
            ImGui.BeginDisabled(_brushes.ContainsKey(_landBrushNewName) || string.IsNullOrWhiteSpace(_landBrushNewName));
            if (ImGui.Button(App.Lang.Get(CREATE)))
            {
                if (!_brushes.ContainsKey(_landBrushNewName))
                {
                    App.LandBrush.AddNew(_landBrushNewName);
                    App.LandBrush.ActiveName = _landBrushNewName;
                    _selectedTransitionBrushName = _Active!.To.Keys.FirstOrDefault("");
                    _landBrushNewName = "";
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(CANCEL)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        if (ImGui.BeginPopupModal("LandBrushDelete", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text(string.Format(App.Lang.Get(DELETE_WARNING_1TYPE_2NAME), App.Lang.Get(LAND_BRUSH), _ActiveName));
            if (ImGui.Button(App.Lang.Get(YES), new Vector2(100, 0)))
            {
                App.LandBrush.Remove(_ActiveName);
                _selectedTransitionBrushName = _Active?.To.Keys.FirstOrDefault("") ?? "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(NO), new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }

    private string _transitionAddName = "";
    private void DrawTransitionPopups()
    {
        if (ImGui.BeginPopupModal("TransitionsAdd", ImGuiWindowFlags.NoDecoration))
        {
            var notUsedBruses = _brushes.Where(lb => lb.Key != _ActiveName && !_Active.To.Keys.Contains(lb.Key)).ToDictionary();
            if(_transitionAddName == "")
                _transitionAddName = notUsedBruses.Keys.FirstOrDefault("");
            LandBrushCombo("##addTransition", notUsedBruses, ref _transitionAddName);
            ImGui.BeginDisabled(notUsedBruses.Count == 0);
            if (ImGui.Button(App.Lang.Get(CREATE), new Vector2(100, 0)))
            {
                _Active.To.Add(_transitionAddName, new List<LandBrushTransition>());
                _selectedTransitionBrushName = _transitionAddName;
                _transitionAddName = "";
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(CANCEL), new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
                _transitionAddName = "";
            }
            ImGui.EndPopup();
        }
        if (ImGui.BeginPopupModal("TransitionsDelete", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text(string.Format(App.Lang.Get(DELETE_WARNING_1TYPE_2NAME), App.Lang.Get(TRANSITION), _selectedTransitionBrushName));
            if (ImGui.Button(App.Lang.Get(YES), new Vector2(100, 0)))
            {
                //Remove all entries that have removed brush as to-transition
                if (_Active!.To.Remove(_selectedTransitionBrushName, out var removed))
                {
                    removed.ForEach(t => App.LandBrush.RemoveLandBrushEntry(t.TileID, _ActiveName, _selectedTransitionBrushName));
                }
                if(_Active.To.Count > 0)
                    _selectedTransitionBrushName = _Active.To.Keys.FirstOrDefault("");
                else
                    _selectedTransitionBrushName = "";
                _selectedTransitionBrushName = _Active.To.Keys.FirstOrDefault("");
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button(App.Lang.Get(NO), new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
    
    

    #region Import
    private void DrawImport()
    {
        if(ImGui.CollapsingHeader("Import CED+ TileBrush.xml"))
        {
            _tilesBrushPath ??= "";
            ImGui.InputText("File", ref _tilesBrushPath, 512);
            ImGui.SameLine();
            if (ImGui.Button("..."))
            {
                if (TinyFileDialogs.TryOpenFile
                        ("Select TilesBrush file", Environment.CurrentDirectory, ["*.xml"], null, false, out var newPath))
                {
                    _tilesBrushPath = newPath;
                }
            }
            if (ImGui.Button("Import"))
            {
                _importSuccess = App.LandBrush.ImportTilesBrush(_tilesBrushPath, out _importStatusText);
                App.LandBrush.ActiveName = _brushes.Keys.FirstOrDefault("");
            }
            ImGui.TextColored(_importSuccess ? ImGuiColor.Green : ImGuiColor.Red, _importStatusText);
        }
    }
    
    
    #endregion
}