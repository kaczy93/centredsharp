using System.Text.RegularExpressions;
using CentrED.Utility;
using ImGuiNET;

namespace CentrED.UI.Windows;

public class LocalEditWindow : Window
{
    public override string Name => "Local Editing";

    public override void DrawMenuItem()
    {
        base.DrawMenuItem();
        ImGui.Separator();
    }

    private string _uoDirPath = "";
    private bool _isUoDirValid;
    private string[] _mapFilePaths;
    private string[] _mapFileNames;
    private int _mapFileIndex;
    private int _mapIndex;
    private bool _customMapSize;
    
    protected override void InternalDraw()
    {
        if (ImGui.InputText("Config File", ref _uoDirPath, 512))
        {
            ProcessUoDirectory();
        }
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            ImGui.OpenPopup("open-file");
        }
        var isOpen = true;
        if (ImGui.BeginPopupModal("open-file", ref isOpen, ImGuiWindowFlags.NoTitleBar))
        {
            var picker = FilePicker.GetFolderPicker(this, Environment.CurrentDirectory);
            if (picker.Draw())
            {
                _uoDirPath = picker.SelectedFile;
                ProcessUoDirectory();
                FilePicker.RemoveFilePicker(this);
            }
            ImGui.EndPopup();
        }
        if (_isUoDirValid)
        {
            if (ImGui.Combo("Map File", ref _mapFileIndex, _mapFileNames, _mapFilePaths.Length))
            {
                _mapIndex = int.Parse(Regex.Match(_mapFileNames[_mapFileIndex], @"\d+").Value);
                _customMapSize = false;
            }
            ImGui.Checkbox("Custom Map Size", ref _customMapSize);
            if (_customMapSize)
            {
                int temp = 0;
                ImGui.InputInt("Width(blocks)", ref temp);
                ImGui.InputInt("Height(blocks)", ref temp);
            }
            else
            {
                var staidxFileName = $"staidx{_mapIndex}.mul"; 
                MapSizeHelper.StaidxSizeHint(Path.Combine(_uoDirPath, staidxFileName), out var width, out var height, out _);
                ImGui.Text($"Size: {width}x{height}");
            }
        }
        else
        {
            ImGui.Text("Dir invalid");
        }
        
    }

    private void ProcessUoDirectory()
    {
        _isUoDirValid = false;
        
        var dirExists = Directory.Exists(_uoDirPath);
        if (!dirExists)
            return;
        
        var clientExists = File.Exists(Path.Combine(_uoDirPath, "client.exe"));
        if (!clientExists)
            return;

        var isUop = File.Exists(Path.Combine(_uoDirPath, "map0LegacyMUL.uop"));
        var searchPattern = isUop ? "map?LegacyMUL.uop" : "map?.mul";
        _mapFilePaths = Directory.EnumerateFiles(_uoDirPath, "map?.mul").ToArray();
        _mapFileNames = _mapFilePaths.Select(Path.GetFileName).ToArray()!;
        
        _isUoDirValid = true;
    }
}