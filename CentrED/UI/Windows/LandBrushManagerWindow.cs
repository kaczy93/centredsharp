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
    public static readonly Vector2 HalfSize = new(22, 22);

    private string _tilesBrushPath = "TilesBrush.xml";
    private static XmlSerializer _xmlSerializer = new(typeof(TilesBrush));
    private string _importStatusText = "";
    
    private int _landBrushIndex;
    private string _landBrushNewName = "";
    private int _transitionIndex;
    
    public LandBrush? Selected;
    private Dictionary<string, LandBrush> _landBrushes => ProfileManager.ActiveProfile.LandBrush;
    private string[] _landBrushNames => new[] { String.Empty }.Concat(_landBrushes.Keys).ToArray();
    private string[] _transitionNames => Selected?.Transitions.Keys.ToArray() ?? [];

    protected override void InternalDraw()
    {
        if (!CEDGame.MapManager.Client.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
        DrawImport();
        if (ImGui.Button("Save"))
        {
            Config.Save();
        }
        LandBrushCombo();
        ImGui.SameLine();
        if(_landBrushIndex == 0) 
            ImGui.BeginDisabled();
        if (ImGui.Button("Add"))
        {
            ImGui.OpenPopup("LandBrushAdd");
        }
        if (ImGui.BeginPopupModal("LandBrushAdd", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.InputText("Name", ref _landBrushNewName, 64);
            if (ImGui.Button("Add"))
            {
                if (!_landBrushes.ContainsKey(_landBrushNewName))
                {
                    _landBrushes.Add(_landBrushNewName, new LandBrush
                    {
                        Name = _landBrushNewName
                    });
                    _landBrushIndex = _landBrushes.Count;
                    Selected = _landBrushes[_landBrushNewName];
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Remove"))
        {
            ImGui.OpenPopup("LandBrushDelete");
        }
        if (ImGui.BeginPopupModal("LandBrushDelete", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration))
        {
            ImGui.Text("Are you sure you want to delete:");
            ImGui.Text($"'{Selected.Name}'");
            if (ImGui.Button("Yes", new Vector2(100, 0)))
            {
                _landBrushes.Remove(Selected.Name);
                _landBrushIndex--;
                Selected = _landBrushIndex != 0 ? _landBrushes[Selected.Name] : null;
                _transitionIndex = 0;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No", new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
        if(_landBrushIndex == 0) 
            ImGui.EndDisabled();
        
        if (Selected != null)
        {
            DrawFullTiles();
           
            ImGui.Text("Transitions:");
            DrawPreview(_transitionNames[_transitionIndex]);
            ImGui.SameLine();
            if (ImGui.BeginCombo("##transitions", _transitionNames[_transitionIndex], ImGuiComboFlags.HeightLarge))
            {
                for (var i = 0; i < _transitionNames.Length; i++)
                {
                    var is_selected = _transitionIndex == i;
                    DrawPreview(_transitionNames[i]);
                    ImGui.SameLine();
                    if (ImGui.Selectable
                        (
                            _transitionNames[i],
                            is_selected,
                            ImGuiSelectableFlags.None,
                            new Vector2(ImGui.GetContentRegionAvail().X, HalfSize.Y)
                        ))
                    {
                        _transitionIndex = i;
                    }
                    if (is_selected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }
            DrawTransitions();
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

    public void LandBrushCombo()
    {
        if(ImGui.BeginCombo("##landBrush", _landBrushNames[_landBrushIndex], ImGuiComboFlags.HeightLarge))
        {
            for (var i = 0; i < _landBrushNames.Length; i++)
            {
                var is_selected = _landBrushIndex == i;
                DrawPreview(_landBrushNames[i]);
                ImGui.SameLine();
                if (ImGui.Selectable(_landBrushNames[i], is_selected, ImGuiSelectableFlags.None,  new Vector2(ImGui.GetContentRegionAvail().X, HalfSize.Y)))
                {
                    _landBrushIndex = i;
                    Selected = _landBrushIndex == 0 ? null : _landBrushes[_landBrushNames[i]];
                    _transitionIndex = 0;
                }
                if (is_selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
    }

    private void DrawFullTiles()
    {
        ImGui.Text("Full tiles:");
        foreach (var fullTile in Selected.Tiles.ToArray())
        {
            DrawTile(fullTile, FullSize);
            ImGui.SameLine();
            UIManager.Tooltip($"0x{fullTile:X4}");
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, .2f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
            if (ImGui.SmallButton($"x##{fullTile}"))
            {
                Selected.Tiles.Remove(fullTile);
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
                        Selected.Tiles.Add(id);
                }
            }
            ImGui.EndDragDropTarget();
        }
    }

    private void DrawTransitions()
    {
        var sourceTexture = CalculateButtonTexture(Selected.Tiles[0]);
        var targetBrush = _landBrushes[_transitionNames[_transitionIndex]];
        var targetTexture = CalculateButtonTexture(targetBrush.Tiles[0]);
        var transitions = Selected.Transitions[_transitionNames[_transitionIndex]];
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
            }
            ImGui.PopStyleColor(2);
            ImGui.Text($"0x{transition.TileID:X4}");
            ImGui.EndGroup();
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.One);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ToggleDirButton(transition, Up, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, North, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, Right, sourceTexture, targetTexture);
            ToggleDirButton(transition, West, sourceTexture, targetTexture);
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(13, 13));
            ImGui.SameLine();
            ToggleDirButton(transition, East, sourceTexture, targetTexture);
            ToggleDirButton(transition, Left, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, South, sourceTexture, targetTexture);
            ImGui.SameLine();
            ToggleDirButton(transition, Down, sourceTexture, targetTexture);
            ImGui.PopStyleVar();
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
                        transitions.Add(new LandBrushTransition(id));
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
        }
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
    #region Import
    private void DrawImport()
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
            _landBrushIndex = 1;
            Selected = _landBrushes[_landBrushes.Keys.First()];
        }
        ImGui.TextColored(UIManager.Green, _importStatusText);
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