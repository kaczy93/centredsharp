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

namespace CentrED.UI.Windows;

public class LandBrushWindow : Window
{
    public override string Name => "LandBrush";

    private static readonly Vector2 TexSize = new(44, 44);
    
    private string _tilesBrushPath = "TilesBrush.xml";
    private static XmlSerializer _xmlSerializer = new(typeof(TilesBrush));
    
    private int _landBrushIndex;
    private string _landBrushName;
    public LandBrush? Selected;
    protected override void InternalDraw()
    {
        if (!CEDGame.MapManager.Client.Initialized)
        {
            ImGui.Text("Not connected");
            return;
        }
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
        }
        ImGui.NewLine();
        var landBrushes = ProfileManager.ActiveProfile.LandBrush;
        var names = new[] { String.Empty }.Concat(landBrushes.Keys).ToArray();
        if (ImGui.Combo("", ref _landBrushIndex, names, names.Length))
        {
            _landBrushName = names[_landBrushIndex];
            if (_landBrushIndex == 0)
            {
                Selected = null;
            }
            else
            {
                Selected = landBrushes[_landBrushName];
            }
        }
        if (Selected != null)
        {
            ImGui.Text(Selected.Name);
            ImGui.Text("Full tiles:");
            foreach (var fullTile in Selected.Tiles)
            {
                var tex = TexmapsLoader.Instance.GetLandTexture(fullTile, out var bounds);
                CEDGame.UIManager.DrawImage(tex, bounds, TexSize);
                ImGui.SameLine();
                ImGui.Text($"0x{fullTile:X4}");
            }
            ImGui.Text("Transitions:");
            foreach (var (name, transitions) in Selected.Transitions)
            {
                ImGui.Text("To " + name);
                foreach (var transition in transitions)
                {
                    Draw(transition);
                }
            }
        }
    }
    
    private void Draw(LandBrushTransition transition)
    {
        var tex = TexmapsLoader.Instance.GetLandTexture(transition.TileID, out var bounds);
        if (tex != null)
        {
            CEDGame.UIManager.DrawImage(tex, bounds, TexSize);
            ImGui.SameLine();
        }
        var type = transition.Direction;
        ImGui.Text($"0x{transition.TileID:X4}");
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text($"{f(type, Up)} {f(type, North)} {f(type, Right)}");
        ImGui.Text($"{f(type, West)}   {f(type, East)}");
        ImGui.Text($"{f(type, Left)} {f(type, South)} {f(type, Down)}");
        ImGui.EndGroup();
        ImGui.SameLine();

        ImGui.Text($"{type:F}");
    }

    private byte f(Direction t, Direction f)
    {
        return (byte)((t & f) > 0 ? 1 : 0);
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
                            newList.Add(new LandBrushTransition{TileID =  newId, Direction = newType});
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
            case "LL": return Down | East | Right ;
            case "UU": return Left | South | Down;
            //File mentions type FF but it's never used
            // "FF" => 
            default:
                Console.WriteLine("Unknown type " + oldType);
                return 0;
        }
    }
}