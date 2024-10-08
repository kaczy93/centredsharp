using System.Globalization;
using System.Numerics;
using System.Xml.Serialization;
using CentrED.IO;
using CentrED.IO.Models;
using CentrED.IO.Models.Centredplus;
using ClassicUO.Assets;
using ImGuiNET;
using static CentrED.Application;
using static CentrED.IO.Models.Direction;
using Vector2 = System.Numerics.Vector2;

namespace CentrED.UI.Windows;

public class LandBrushManagerWindow : Window
{
    public override string Name => "LandBrush Manager";

    public static readonly Vector2 FullSize = new(44, 44);
    public static readonly Vector2 HalfSize = FullSize / 2;

    private string _tilesBrushPath = "TilesBrush.xml";
    private static XmlSerializer _xmlSerializer = new(typeof(TilesBrush));
    private string _importStatusText = "";

    private string _landBrushNewName = "";
    private string _selectedLandBrushName = "";
    private string _selectedTransitionBrushName = "";

    private Dictionary<string, LandBrush> _landBrushes => ProfileManager.ActiveProfile.LandBrush;
    public LandBrush? Selected => _landBrushes.GetValueOrDefault(_selectedLandBrushName);

    private static readonly Vector2 ComboFramePadding = ImGui.GetStyle().FramePadding with{ Y = (float)((HalfSize.Y - ImGui.GetTextLineHeight()) * 0.5) };

    private bool _unsavedChanges;

    protected override void InternalDraw()
    {
        if (!CEDGame.MapManager.Client.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        
        DrawImport();

        ImGui.BeginDisabled(!_unsavedChanges);
        if (ImGui.Button("Save"))
        {
            ProfileManager.Save();
            _unsavedChanges = false;
        }
        ImGui.EndDisabled();
        if (_unsavedChanges)
        {
            ImGui.SameLine();
            ImGui.TextColored(UIManager.Green, "Unsaved Changes");
        }
        ImGui.Separator();
        
        ImGui.Columns(2);
        if(ImGui.BeginChild("Brushes"))
        {
            ImGui.Text("Land Brush:");
            if (LandBrushCombo(ref _selectedLandBrushName))
            {
                _selectedTransitionBrushName = Selected?.Transitions.Keys.FirstOrDefault("") ?? "";
            }
            if (ImGui.Button("Add"))
            {
                ImGui.OpenPopup("LandBrushAdd");
            }
            ImGui.SameLine();
            ImGui.BeginDisabled(_landBrushes.Count <= 0);
            if (ImGui.Button("Remove"))
            {
                ImGui.OpenPopup("LandBrushDelete");
            }
            ImGui.EndDisabled();
            ImGui.Separator();
            if (Selected != null)
            {
                DrawFullTiles();
            }
            DrawBrushPopups();
            ImGui.EndChild();
        }
        ImGui.NextColumn();
        if(ImGui.BeginChild("Transitions"))
        {
            if (Selected != null)
            {
                DrawTransitions();
            }
            DrawTransitionPopups();
            ImGui.EndChild();
        }
    }

    public void DrawPreview(string name)
    {
        DrawPreview(name, HalfSize);
    }

    public void DrawPreview(string name, Vector2 size)
    {
        if (_landBrushes.TryGetValue(name, out var brush))
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
        var spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(TileDataLoader.Instance.LandData[id].TexID);
        if (spriteInfo.Texture != null)
        {
            CEDGame.UIManager.DrawImage(spriteInfo.Texture, spriteInfo.UV, size, true);
        }
        else
        {
            ImGui.Dummy(size);
        }
    }

    public bool LandBrushCombo(ref string selectedName)
    {
        return LandBrushCombo("landBrush", _landBrushes, ref selectedName);
    }

    private bool LandBrushCombo<T>(string id, Dictionary<string, T> dictionary, ref string selectedName, ImGuiComboFlags flags = ImGuiComboFlags.HeightLarge)
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
        foreach (var fullTile in Selected.Tiles.ToArray())
        {
            DrawTile(fullTile, FullSize);
            UIManager.Tooltip($"0x{fullTile:X4}");
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
            if (ImGui.SmallButton($"x##{fullTile}"))
            {
                Selected.Tiles.Remove(fullTile);
                CEDGame.MapManager.RemoveLandBrushEntry(fullTile, _selectedLandBrushName, _selectedLandBrushName);
                _unsavedChanges = true;
            }
            ImGui.PopStyleColor(2);
            ImGui.Text($"0x{fullTile:X4}");
            ImGui.EndGroup();
        }
        ImGui.Button("+##AddFullTile", FullSize);
        UIManager.Tooltip("Drag and drop a tile here to add it to the brush");
        if (ImGui.BeginDragDropTarget())
        {
            var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
            unsafe
            {
                if (payloadPtr.NativePtr != null)
                {
                    var dataPtr = (int*)payloadPtr.Data;
                    ushort id = (ushort)dataPtr[0];
                    if(!Selected.Tiles.Contains(id))
                    {
                        Selected.Tiles.Add(id);
                        CEDGame.MapManager.AddLandBrushEntry(id, _selectedLandBrushName, _selectedLandBrushName);
                        _unsavedChanges = true;
                    }
                }
            }
            ImGui.EndDragDropTarget();
        }
    }

    private void DrawTransitions()
    {
        ImGui.Text("Transitions:");
        LandBrushCombo("transitions", Selected.Transitions, ref _selectedTransitionBrushName);
        if (ImGui.Button("Add"))
        {
            ImGui.OpenPopup("TransitionsAdd");
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(Selected.Transitions.Count == 0);
        if (ImGui.Button("Remove"))
        {
            ImGui.OpenPopup("TransitionsDelete");
        }
        ImGui.EndDisabled();
        ImGui.Separator();
        
        if(Selected.Transitions.Count == 0)
            return;
        
        var targetBrush = _landBrushes[_selectedTransitionBrushName];
        if(Selected.Tiles.Count == 0 || targetBrush.Tiles.Count == 0)
        {
            ImGui.Text("Missing full tiles on one of the brushes");
            return;
        }
        var sourceTexture = CalculateButtonTexture(Selected.Tiles[0]);
        var targetTexture = CalculateButtonTexture(targetBrush.Tiles[0]);
        var transitions = Selected.Transitions[_selectedTransitionBrushName];
        foreach (var transition in transitions.ToArray())
        {
            var tileId = transition.TileID;
            DrawTile(tileId, FullSize);
            ImGui.SameLine();
            var type = transition.Direction;
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
            if (ImGui.SmallButton($"x##{transition.TileID}"))
            {
                transitions.Remove(transition);
                CEDGame.MapManager.RemoveLandBrushEntry(transition.TileID, _selectedLandBrushName, _selectedTransitionBrushName);
                _unsavedChanges = true;
            }
            ImGui.PopStyleColor(2);
            ImGui.Text($"0x{transition.TileID:X4}");
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
            ImGui.Image(sourceTexture.texPtr, new Vector2(11,11), sourceTexture.uv0, sourceTexture.uv1, Vector4.One, new Vector4(0,0,0,1));
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
        UIManager.Tooltip("Drag and drop a tile here to add it to the brush");
        if (ImGui.BeginDragDropTarget())
        {
            var payloadPtr = ImGui.AcceptDragDropPayload(TilesWindow.Land_DragDrop_Target_Type);
            unsafe
            {
                if (payloadPtr.NativePtr != null)
                {
                    var dataPtr = (int*)payloadPtr.Data;
                    ushort id = (ushort)dataPtr[0];
                    if(transitions.All(t => t.TileID != id))
                    {
                        transitions.Add(new LandBrushTransition(id));
                        CEDGame.MapManager.AddLandBrushEntry(id, _selectedLandBrushName, _selectedTransitionBrushName);
                        _unsavedChanges = true;
                    }
                }
            }
            ImGui.EndDragDropTarget();
        }
    }

    private void ToggleDirButton(LandBrushTransition transition, Direction dir, (nint texPtr, Vector2 uv0, Vector2 uv1) sourceTexture, (nint texPtr, Vector2 uv0, Vector2 uv1) targetTexture)
    {
        var isSet = transition.Direction.Contains(dir);
        var tex = isSet ? targetTexture : sourceTexture;
        if (ImGui.ImageButton($"{transition.TileID}{dir}", tex.texPtr, new Vector2(11,11), tex.uv0, tex.uv1))
        {
            if (isSet)
            {
                transition.Direction &= ~dir;
            }
            else
            {
                transition.Direction |= dir;
            }
            _unsavedChanges = true;
        }
        UIManager.Tooltip(isSet ? _selectedTransitionBrushName : _selectedLandBrushName);
    }

    private (nint texPtr, Vector2 uv0, Vector2 uv1) CalculateButtonTexture(ushort tileId)
    {
        var spriteInfo = CEDGame.MapManager.Texmaps.GetTexmap(TileDataLoader.Instance.LandData[tileId].TexID);
        var tex = spriteInfo.Texture;
        var bounds = spriteInfo.UV;
        var texPtr = CEDGame.UIManager._uiRenderer.BindTexture(tex);
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
            ImGui.InputText("Name", ref _landBrushNewName, 64);
            ImGui.BeginDisabled(_landBrushes.ContainsKey(_landBrushNewName) || string.IsNullOrWhiteSpace(_landBrushNewName));
            if (ImGui.Button("Add"))
            {
                if (!_landBrushes.ContainsKey(_landBrushNewName))
                {
                    _landBrushes.Add(_landBrushNewName, new LandBrush
                    {
                        Name = _landBrushNewName
                    });
                    _selectedLandBrushName = _landBrushNewName;
                    _selectedTransitionBrushName = Selected.Transitions.Keys.FirstOrDefault("");
                    _landBrushNewName = "";
                    _unsavedChanges = true;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        if (ImGui.BeginPopupModal("LandBrushDelete", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text("Are you sure you want to delete:");
            ImGui.Text($"LandBrush: '{Selected.Name}'");
            if (ImGui.Button("Yes", new Vector2(100, 0)))
            {
                foreach (var landBrushesValue in _landBrushes.Values)
                {
                    landBrushesValue.Transitions.Remove(Selected.Name);
                }
                _landBrushes.Remove(Selected.Name);
                _selectedLandBrushName = _landBrushes.Keys.FirstOrDefault("");
                _selectedTransitionBrushName = Selected?.Transitions.Keys.FirstOrDefault("") ?? "";
                _unsavedChanges = true;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(100, 0)))
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
            var notUsedBruses = _landBrushes.Where(lb => lb.Key != Selected.Name && !Selected.Transitions.Keys.Contains(lb.Key)).ToDictionary();
            if(_transitionAddName == "")
                _transitionAddName = notUsedBruses.Keys.FirstOrDefault("");
            LandBrushCombo("##addTransition", notUsedBruses, ref _transitionAddName);
            ImGui.BeginDisabled(notUsedBruses.Count == 0);
            if (ImGui.Button("Add", new Vector2(100, 0)))
            {
                Selected.Transitions.Add(_transitionAddName, new List<LandBrushTransition>());
                _selectedTransitionBrushName = _transitionAddName;
                _transitionAddName = "";
                _unsavedChanges = true;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
                _transitionAddName = "";
            }
            ImGui.EndPopup();
        }
        if (ImGui.BeginPopupModal("TransitionsDelete", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text("Are you sure you want to delete:");
            ImGui.Text($"Transition: '{_selectedTransitionBrushName}'");
            if (ImGui.Button("Yes", new Vector2(100, 0)))
            {
                Selected!.Transitions.Remove(_selectedTransitionBrushName);
                if(Selected.Transitions.Count > 0)
                    _selectedTransitionBrushName = Selected.Transitions.Keys.FirstOrDefault("");
                else
                    _selectedTransitionBrushName = "";
                _selectedTransitionBrushName = Selected.Transitions.Keys.FirstOrDefault("");
                _unsavedChanges = true;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(100, 0)))
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
            ImGui.InputText("File", ref _tilesBrushPath, 512);
            ImGui.SameLine();
            if (ImGui.Button("..."))
            {
                ImGui.OpenPopup("open-file");
            }
            var isOpen = true;
            if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar))
            {
                var picker = FilePicker.GetFilePicker(this, Environment.CurrentDirectory, ".xml");
                if (picker.Draw())
                {
                    _tilesBrushPath = picker.SelectedFile;
                    FilePicker.RemoveFilePicker(this);
                }
                ImGui.EndPopup();
            }
            if (ImGui.Button("Import"))
            {
                ImportLandBrush();
                _selectedLandBrushName = _landBrushes.Keys.FirstOrDefault("");
            }
            ImGui.TextColored(UIManager.Green, _importStatusText);
        }
    }
    
    private void ImportLandBrush()
    {
        try
        {
            using var reader = new FileStream(_tilesBrushPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var tilesBrush = (TilesBrush)_xmlSerializer.Deserialize(reader)!;
            var target = ProfileManager.ActiveProfile.LandBrush;
            target.Clear();
            foreach (var brush in tilesBrush.Brush)
            {
                var newBrush = new LandBrush();
                newBrush.Name = brush.Name;
                foreach (var land in brush.Land)
                {
                    if (TryParseHex(land.ID, out var newId))
                    {
                        newBrush.Tiles.Add(newId);
                    }
                    else
                    {
                        Console.WriteLine($"Unable to parse land ID {land.ID} in brush {brush.Id}");
                    }
                }
                foreach (var edge in brush.Edge)
                {
                    var to = tilesBrush.Brush.Find(b => b.Id == edge.To);
                    var newList = new List<LandBrushTransition>();
                    foreach (var edgeLand in edge.Land)
                    {
                        if (TryParseHex(edgeLand.ID, out var newId))
                        {
                            var newType = ConvertType(edgeLand.Type);
                            newList.Add
                            (
                                new LandBrushTransition
                                {
                                    TileID = newId,
                                    Direction = newType
                                }
                            );
                        }
                        else
                        {
                            Console.WriteLine($"Unable to parse edgeland ID {edgeLand.ID} in brush {brush.Id}");
                        }
                    }
                    newBrush.Transitions.Add(to.Name, newList);
                }
                target.Add(newBrush.Name, newBrush);
            }
            CEDGame.MapManager.InitLandBrushes();
            ProfileManager.Save();
            _importStatusText = "Import Successful";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private bool TryParseHex(string value, out ushort result)
    {
        //Substring removes 0x from the value
        return ushort.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    private Direction ConvertType(string oldType)
    {
        switch (oldType)
        {
            case "DR": return Up;
            case "DL": return Right;
            case "UL": return Down;
            case "UR": return Left;
            case "LL": return Down | East | Right;
            case "UU": return Left | South | Down;
            //File mentions type FF but it's never used
            // "FF" => 
            default:
                Console.WriteLine("Unknown type " + oldType);
                return 0;
        }
    }
    #endregion
}