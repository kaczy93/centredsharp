using CentrED.UI;
using ClassicUO.Assets;

namespace CentrED.Blueprints;

public class BlueprintManager(MultiLoader multiLoader)
{
    public const string BLUEPRINTS_DIR = "Blueprints";
    
    public BlueprintTreeEntry Root = new("Root", true, []);

    public void Load()
    {
        Root = new("Root", true, []);
        LoadMultis();
        LoadBlueprints();
    }

    private void LoadMultis()
    {
        Dictionary<uint, string> multiNames = MultiNamesReader.Read(BLUEPRINTS_DIR);
        var multisEntry = new BlueprintTreeEntry("multi.mul", true, []);
        for (uint i = 0; i < MultiLoader.MAX_MULTI_DATA_INDEX_COUNT; i++)
        {
            var info = multiLoader.GetMultis(i);
            if (info != null && info.Count > 0)
            {
                if (info.All(x => x.ID == 0))
                    continue;

                var path = $"{multisEntry.Path}/{i.FormatId()}:{multiNames.GetValueOrDefault(i, "Unknown")}";
                var entry = new BlueprintTreeEntry(path, true, []);
                entry.Tiles = info.Select(tile => new BlueprintTile(tile)).ToList();
                multisEntry.Children.Add(entry);
            }
        }
        Root.Children.Add(multisEntry);
    }

    public void LoadBlueprints()
    {
        if (!Directory.Exists(BLUEPRINTS_DIR))
            Directory.CreateDirectory(BLUEPRINTS_DIR);

        var blueprints = LoadBlueprintDirectory(BLUEPRINTS_DIR);
        Root.Children.AddRange(blueprints.Children);
    }

    private BlueprintTreeEntry LoadBlueprintDirectory(string path)
    {
        var result = new BlueprintTreeEntry(path, true, []);
        var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        foreach (var dir in dirs)
        {
            result.Children.Add(LoadBlueprintDirectory(dir));
        }
        var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            if(file.EndsWith(MultiNamesReader.FILE_NAME))
                continue;
            
            result.Children.Add(new BlueprintTreeEntry(file, false, []));
        }
        return result;
    }
}