namespace CentrED.Tests;

public class ConfigTests : IDisposable
{
    private enum Fruit
    {
        Apple,
        Banana
    }

    private record Counted(string Text)
    {
        public static int ParseCount;
    }

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"confignew_{Guid.NewGuid():N}.cfg");

    public void Dispose()
    {
        File.Delete(_path);
    }

    private Config Create(string? content = null)
    {
        if (content != null)
            File.WriteAllText(_path, content);
        var config = new Config(_path);
        config.Load();
        return config;
    }

    private Config SaveAndReopen(Config config)
    {
        config.Save();
        return Create();
    }

    [Fact]
    public void RoundTripsValuesSortedByKey()
    {
        var config = Create();
        config.Set("map.flat_view", true);
        config.Set("virtual_layer.z", 5);
        config.Set("a.scale", 1.5f);
        config.Set("tiles.filter", "wood");
        config.Set("fruit", Fruit.Banana);
        config.Set("ids", new ushort[] { 1, 0x200 });
        config.Set("names", new List<string> { "a", "b" });
        config.Save();

        Assert.Equal
        (
            "a.scale=1.5\nfruit=Banana\nids=1,512\nmap.flat_view=true\nnames=a,b\ntiles.filter=wood\nvirtual_layer.z=5\n",
            File.ReadAllText(_path)
        );

        var reopened = Create();
        Assert.True(reopened.GetOrDefault("map.flat_view", false));
        Assert.Equal(5, reopened.GetOrDefault("virtual_layer.z", 0));
        Assert.Equal(1.5f, reopened.GetOrDefault("a.scale", 0f));
        Assert.Equal("wood", reopened.GetOrDefault("tiles.filter", ""));
        Assert.Equal(Fruit.Banana, reopened.GetOrDefault("fruit", Fruit.Apple));
        Assert.Equal(new ushort[] { 1, 512 }, reopened.GetOrDefault("ids", Array.Empty<ushort>()));
        Assert.Equal(new List<string> { "a", "b" }, reopened.GetOrDefault("names", new List<string>()));
    }

    [Fact]
    public void ParsesHexIntegers()
    {
        var config = Create("id=0x1F\n");
        Assert.Equal((ushort)0x1F, config.GetOrDefault("id", (ushort)0));
    }

    [Fact]
    public void EscapesSpecialCharacters()
    {
        var config = Create();
        config.Set("value", "a=b\\c\nd");
        config.Set("list", new[] { "x,y", "z\\" });
        config.Save();

        Assert.Equal("list=x\\\\,y,z\\\\\\\\\nvalue=a\\=b\\\\c\\nd\n", File.ReadAllText(_path));

        var reopened = Create();
        Assert.Equal("a=b\\c\nd", reopened.GetOrDefault("value", ""));
        Assert.Equal(new[] { "x,y", "z\\" }, reopened.GetOrDefault("list", Array.Empty<string>()));
    }

    [Fact]
    public void ParsesOnlyOnce()
    {
        ConfigConverters.Register
        (
            s =>
            {
                Counted.ParseCount++;
                return new Counted(s);
            },
            c => c.Text
        );
        var config = Create("counted=hello\n");
        Counted.ParseCount = 0;
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal("hello", config.GetOrDefault("counted", new Counted("")).Text);
        }
        Assert.Equal(1, Counted.ParseCount);
    }

    [Fact]
    public void DefaultsAreWrittenAndUnknownKeysPreserved()
    {
        var config = Create("# comment\nunknown.key=keep\n\n");
        config.GetOrDefault("known", 3);
        config.Save();
        Assert.Equal("known=3\nunknown.key=keep\n", File.ReadAllText(_path));
    }

    [Fact]
    public void InvalidValueFallsBackToDefault()
    {
        var config = Create("number=abc\nbad key=1\n");
        Assert.Equal(7, config.GetOrDefault("number", 7));
    }

    [Fact]
    public void InvalidKeyThrows()
    {
        var config = Create();
        Assert.Throws<ArgumentException>(() => config.GetOrDefault("bad key", 1));
        Assert.Throws<ArgumentException>(() => config.GetOrDefault("bad=key", 1));
    }

    [Fact]
    public void TypeMismatchThrows()
    {
        var config = Create();
        config.GetOrDefault("value", 1);
        Assert.Throws<InvalidOperationException>(() => config.GetOrDefault("value", "1"));
    }

    [Fact]
    public void DictionaryIsFlattened()
    {
        var config = Create("keymap.move_up=W\nkeymap.move_down=S\nother=1\n");
        var keymap = config.GetDictionary<string>("keymap");
        Assert.Equal("W", keymap["move_up"]);
        Assert.Equal("S", keymap["move_down"]);

        keymap.Remove("move_down");
        keymap["move_left"] = "A";
        config.Save();
        Assert.Equal("keymap.move_left=A\nkeymap.move_up=W\nother=1\n", File.ReadAllText(_path));
    }

    [Fact]
    public void DictionaryOverlapThrows()
    {
        var config = Create();
        config.GetDictionary<int>("layout");
        Assert.Throws<InvalidOperationException>(() => config.GetOrDefault("layout.tiles", 1));
        Assert.Throws<InvalidOperationException>(() => config.GetDictionary<int>("layout.tiles"));
    }

    [Fact]
    public void LoadUpdatesExistingHandles()
    {
        var config = Create("z=1\n");
        var entry = config.Entry("z", 0);
        Assert.Equal(1, entry.Value);

        File.WriteAllText(_path, "z=2\n");
        config.Load();
        Assert.Equal(2, entry.Value);

        File.WriteAllText(_path, "");
        config.Load();
        Assert.Equal(0, entry.Value);
    }

    [Fact]
    public void MutatedListIsSaved()
    {
        var config = Create();
        var list = config.GetOrDefault("list", new List<int>());
        list.Add(4);
        var reopened = SaveAndReopen(config);
        Assert.Equal(new List<int> { 4 }, reopened.GetOrDefault("list", new List<int>()));
    }

    [Fact]
    public void RemoveDeletesKey()
    {
        var config = Create("a=1\nb=2\n");
        config.GetOrDefault("a", 0);
        Assert.True(config.Remove("a"));
        Assert.True(config.Remove("b"));
        config.Save();
        Assert.Equal("", File.ReadAllText(_path));
    }
}
