using static CentrED.CentrED;

namespace CentrED;

public class LangManager
{
    public string[] LangNames { get; private set; } = [];

    public int LangIndex
    {
        get;
        set
        {
            LangName.Value = LangNames[value];
            field = value;
        }
    }
    public readonly ConfigValue<string> LangName = App.Config.Init("lang.name", "English");
    
    private Dictionary<string, string?[]> _entries = [];
    private string?[] _Current => _entries[LangNames[LangIndex]];
        
    public LangManager()
    {
        Load();
    }

    private void Load()
    {
        var maxIndex = (int)Enum.GetValues<LangEntry>().Last();
        _entries = [];
        var langFiles = Directory.GetFiles("Languages", "*.txt");
        {
            foreach (var langFile in langFiles)
            {
                var fi = new FileInfo(langFile);
                var langArray = new string[maxIndex + 1];
                
                var lineNumber = 0;
                foreach (var line in File.ReadLines(langFile))
                {
                    if(line.StartsWith('#'))
                        continue;
                    
                    var split = line.Split('=');
                    if (split.Length != 2)
                    {
                        Console.WriteLine($"Invalid line {lineNumber}: '{line}' in language file {langFile}");
                        continue;
                    }
                    var keyText = split[0].Trim();
                    if(!Enum.TryParse(keyText, out LangEntry key))
                    {
                        Console.WriteLine($"Invalid key {keyText} in language file {langFile}");
                        continue;
                    }
                    if (langArray[(int)key] != null && langArray[(int)key] != "")
                    {
                        Console.WriteLine($"Duplicate key {keyText} in language file {langFile}");
                    }
                    var value = split[1].Trim().Replace(@"\n", "\n");//Is it the best way? What about other symbols?
                    langArray[(int)key] = value;
                    lineNumber++;
                }
                FillMissingEntries(langFile, ref langArray);
                _entries.Add(fi.Name.Replace(".txt", ""), langArray);
            }
        }
        LangNames = _entries.Keys.ToArray();
        
        var configLangIndex = Array.FindIndex(LangNames, s => LangName.Value == s);
        if (configLangIndex == -1)
        {
            configLangIndex = Array.FindIndex(LangNames, s => s == "English");
        }
        LangIndex = configLangIndex != -1 ? configLangIndex : 0;
    }

    private static void FillMissingEntries(string langFile, ref string[] langArray)
    {
        foreach (var langEntry in Enum.GetValues<LangEntry>())
        {
            var value = langArray[(int)langEntry];
            if (value == null || value.Length == 0)
            {
                Console.WriteLine($"Missing key {langEntry.ToString()} in language file {langFile}");
                langArray[(int)langEntry] = langEntry.ToString();
            }
        }
    }

    public string Get(LangEntry entry)
    {
        return _Current[(int)entry] ?? "MISSING_" + entry;
    } 
}