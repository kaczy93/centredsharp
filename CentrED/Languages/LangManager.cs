namespace CentrED;

public static class LangManager
{
    public static string[] LangNames { get; private set; } = [];
    public static int LangIndex = 0;
    private static Dictionary<string, Dictionary<string, string>> _entries = [];
    private static Dictionary<string, string> _Current => _entries[LangNames[LangIndex]];
        

    public static void Load()
    {
        var langFiles = Directory.GetFiles("Languages", "*.txt");
        {
            foreach (var langFile in langFiles)
            {
                var fi = new FileInfo(langFile);
                var dict = new Dictionary<string, string>();
                
                var lineNumber = 0;
                foreach (var line in File.ReadLines(langFile))
                {
                    if(line.StartsWith('#'))
                        continue;
                    
                    var split = line.Split('=');
                    if (split.Length != 2)
                    {
                        Console.WriteLine($"Invalid line {lineNumber} in language file {langFile}, skipping");
                        continue;
                    }
                    var key = split[0].Trim();
                    var value = split[1].Trim();
                    dict.Add(key, value);
                    lineNumber++;
                }
                _entries.Add(fi.Name.Replace(".txt", ""), dict);
            }
        }
        LangNames = _entries.Keys.ToArray();
    }

    public static string Get(string entry)
    {
        if(_Current.TryGetValue(entry, out var result))
        {
            return result;
        }
        Console.WriteLine($"Unable to find entry {entry} for language {LangNames[LangIndex]}");
        return entry;
    } 
}