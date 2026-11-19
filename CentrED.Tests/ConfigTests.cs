using CentrED.IO.Models;

namespace CentrED.Tests;

public class ConfigTests : IDisposable
{
    private enum Fruit
    {
        Apple,
        Banana
    }

    [Flags]
    private enum Access
    {
        None = 0,
        Read = 1,
        Write = 2
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
        config.Init("map.flat_view", false).Value = true;
        config.Init("virtual_layer.z", 0).Value = 5;
        config.Init("a.scale", 0f).Value = 1.5f;
        config.Init("tiles.filter", "").Value = "wood";
        config.Init("fruit", Fruit.Apple).Value = Fruit.Banana;
        config.Init("ids", Array.Empty<ushort>()).Value = [1, 0x200];
        config.Init("names", new List<string>()).Value = ["a", "b"];
        config.Save();

        Assert.Equal
        (
            "a.scale=1.5\nfruit=Banana\nids=1,512\nmap.flat_view=true\nnames=a,b\ntiles.filter=wood\nvirtual_layer.z=5\n",
            File.ReadAllText(_path)
        );

        var reopened = Create();
        Assert.True(reopened.Init("map.flat_view", false).Value);
        Assert.Equal(5, reopened.Init("virtual_layer.z", 0).Value);
        Assert.Equal(1.5f, reopened.Init("a.scale", 0f).Value);
        Assert.Equal("wood", reopened.Init("tiles.filter", "").Value);
        Assert.Equal(Fruit.Banana, reopened.Init("fruit", Fruit.Apple).Value);
        Assert.Equal(new ushort[] { 1, 512 }, reopened.Init("ids", Array.Empty<ushort>()).Value);
        Assert.Equal(new List<string> { "a", "b" }, reopened.Init("names", new List<string>()).Value);
    }

    [Fact]
    public void ParsesHexIntegers()
    {
        var config = Create("id=0x1F\n");
        Assert.Equal((ushort)0x1F, config.Init("id", (ushort)0).Value);
    }

    [Fact]
    public void WritesValuesVerbatim()
    {
        var config = Create();
        config.Init("value", "").Value = "a=b\\c\nd";
        config.Init("path", "").Value = "C:\\new\\dir\\";
        config.Save();

        Assert.Equal("path=C:\\new\\dir\\\nvalue=a=b\\c d\n", File.ReadAllText(_path));

        var reopened = Create();
        Assert.Equal("a=b\\c d", reopened.Init("value", "").Value);
        Assert.Equal("C:\\new\\dir\\", reopened.Init("path", "").Value);
    }

    [Fact]
    public void CollectionStringsAreEncoded()
    {
        var config = Create();
        config.Init("list", Array.Empty<string>()).Value = ["x,y", "100%", "a\\b"];
        config.Init("map", new Dictionary<string, List<string>>()).Value = new() { ["k:1;\n"] = ["v,w"] };
        var reopened = SaveAndReopen(config);
        Assert.Equal("list=x%2Cy,100%25,a\\b\nmap=k%3A1%3B%0A:v%2Cw\n", File.ReadAllText(_path));

        Assert.Equal(new[] { "x,y", "100%", "a\\b" }, reopened.Init("list", Array.Empty<string>()).Value);
        Assert.Equal(["v,w"], reopened.Init("map", new Dictionary<string, List<string>>()).Value["k:1;\n"]);
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
        var entry = config.Init("counted", new Counted(""));
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal("hello", entry.Value.Text);
        }
        Assert.Equal(1, Counted.ParseCount);
    }

    [Fact]
    public void DefaultsAreWrittenAndUnknownKeysPreserved()
    {
        var config = Create("# comment\nunknown.key=keep\n\n");
        config.Init("known", 3);
        config.Save();
        Assert.Equal("known=3\nunknown.key=keep\n", File.ReadAllText(_path));
    }

    [Fact]
    public void InvalidValueFallsBackToDefault()
    {
        var config = Create("number=abc\nbad key=1\n");
        Assert.Equal(7, config.Init("number", 7).Value);
    }

    [Fact]
    public void InvalidKeyThrows()
    {
        var config = Create();
        Assert.Throws<ArgumentException>(() => config.Init("bad=key", 1));
        Assert.Throws<ArgumentException>(() => config.Init("", 1));
    }

    [Fact]
    public void KeyIsSanitized()
    {
        var config = Create("my_window.open=true\n");
        var entry = config.Init("My Window.open", false);
        Assert.Equal("my_window.open", entry.Key);
        Assert.True(entry.Value);
    }

    [Fact]
    public void DuplicateInitThrows()
    {
        var config = Create();
        config.Init("value", 1);
        Assert.Throws<InvalidOperationException>(() => config.Init("value", 1));
        Assert.Throws<InvalidOperationException>(() => config.Init("value", "1"));
    }

    [Fact]
    public void DictionaryIsInline()
    {
        var config = Create("keymap=move_up:W;move_down:S\n");
        var keymap = config.Init("keymap", new Dictionary<string, string>()).Value;
        Assert.Equal("W", keymap["move_up"]);
        Assert.Equal("S", keymap["move_down"]);

        keymap.Remove("move_down");
        keymap["move_left"] = "A";
        config.Save();
        Assert.Equal("keymap=move_up:W;move_left:A\n", File.ReadAllText(_path));
    }

    [Fact]
    public void DictionaryNestsLists()
    {
        var config = Create();
        config.Init("fruits", new Dictionary<Fruit, int[]>()).Value = new() { [Fruit.Apple] = [1, 2], [Fruit.Banana] = [] };
        config.Init("names", new Dictionary<string, string>()).Value = new() { ["a:b"] = "c,d" };
        var reopened = SaveAndReopen(config);
        Assert.Equal("fruits=Apple:1,2;Banana:\nnames=a%3Ab:c%2Cd\n", File.ReadAllText(_path));

        var fruits = reopened.Init("fruits", new Dictionary<Fruit, int[]>()).Value;
        Assert.Equal([1, 2], fruits[Fruit.Apple]);
        Assert.Empty(fruits[Fruit.Banana]);
        Assert.Equal("c,d", reopened.Init("names", new Dictionary<string, string>()).Value["a:b"]);
    }

    [Fact]
    public void MalformedDictionaryFallsBackToDefault()
    {
        var config = Create("map=a:1;b\n");
        Assert.Empty(config.Init("map", new Dictionary<string, int>()).Value);
    }

    [Fact]
    public void LoadUpdatesExistingHandles()
    {
        var config = Create("z=1\n");
        var entry = config.Init("z", 0);
        Assert.Equal(1, entry.Value);

        File.WriteAllText(_path, "z=2\n");
        config.Load();
        Assert.Equal(2, entry.Value);

        File.WriteAllText(_path, "");
        config.Load();
        Assert.Equal(0, entry.Value);
        Assert.True(entry.IsDefault);
    }

    [Fact]
    public void MutatedListIsSaved()
    {
        var config = Create();
        var list = config.Init("list", new List<int>()).Value;
        list.Add(4);
        var reopened = SaveAndReopen(config);
        Assert.Equal(new List<int> { 4 }, reopened.Init("list", new List<int>()).Value);
    }

    [Fact]
    public void SetsRoundTrip()
    {
        var config = Create();
        config.Init("sorted", new SortedSet<ushort>()).Value = [3, 1, 2];
        config.Init("hash", new HashSet<string>()).Value = ["a,b"];
        config.Init("hues", new Dictionary<string, SortedSet<ushort>>()).Value = new() { ["x"] = [5, 4] };
        config.Save();
        Assert.Contains("sorted=1,2,3\n", File.ReadAllText(_path));

        var reopened = SaveAndReopen(config);
        Assert.Equal([1, 2, 3], reopened.Init("sorted", new SortedSet<ushort>()).Value);
        Assert.Equal(["a,b"], reopened.Init("hash", new HashSet<string>()).Value);
        Assert.Equal([4, 5], reopened.Init("hues", new Dictionary<string, SortedSet<ushort>>()).Value["x"]);
    }

    [Fact]
    public void UshortPairsRoundTrip()
    {
        var config = Create("bad=1:70000\n");
        config.Init("pairs", new List<(ushort, ushort)>()).Value = [(1, 2), (65535, 0)];
        config.Save();
        Assert.Contains("pairs=1:2,65535:0\n", File.ReadAllText(_path));

        var reopened = SaveAndReopen(config);
        Assert.Equal([(1, 2), (65535, 0)], reopened.Init("pairs", new List<(ushort, ushort)>()).Value);
        Assert.Equal(((ushort)7, (ushort)8), config.Init("bad", ((ushort)7, (ushort)8)).Value);
    }

    [Fact]
    public void UppercaseKeyInFileIsMatched()
    {
        var config = Create("Zoom=5\n");
        Assert.Equal(5, config.Init("Zoom", 1).Value);
        config.Save();
        Assert.Equal("zoom=5\n", File.ReadAllText(_path));
    }

    [Fact]
    public void MutatingValueDoesNotChangeDefault()
    {
        var config = Create("list=1\n");
        var entry = config.Init("list", new List<int>());
        File.WriteAllText(_path, "");
        config.Load();
        entry.Value.Add(9);
        config.Load();
        Assert.Empty(entry.Value);
    }

    [Fact]
    public void IsDefaultComparesContent()
    {
        var config = Create();
        var entry = config.Init("list", new List<int>());
        entry.Value = [];
        Assert.True(entry.IsDefault);
        entry.Value.Add(1);
        Assert.False(entry.IsDefault);
    }

    [Fact]
    public void EmptyListElementIsInvalid()
    {
        var config = Create("a=x,,y\nb=,\nc=\n");
        Assert.Equal(["d"], config.Init("a", new List<string> { "d" }).Value);
        Assert.Equal(["d"], config.Init("b", new[] { "d" }).Value);
        Assert.Empty(config.Init("c", new List<string> { "d" }).Value);
    }

    [Fact]
    public void EnumValuesAreValidated()
    {
        var config = Create("undefined=42\nnumeric=1\nname=banana\n");
        Assert.Equal(Fruit.Apple, config.Init("undefined", Fruit.Apple).Value);
        Assert.Equal(Fruit.Banana, config.Init("numeric", Fruit.Apple).Value);
        Assert.Equal(Fruit.Banana, config.Init("name", Fruit.Apple).Value);
    }

    [Fact]
    public void FlagsEnumValuesAreValidated()
    {
        var config = Create("names=read, write\nnumeric=3\nundefined=4\n");
        Assert.Equal(Access.Read | Access.Write, config.Init("names", Access.None).Value);
        Assert.Equal(Access.Read | Access.Write, config.Init("numeric", Access.None).Value);
        Assert.Equal(Access.None, config.Init("undefined", Access.None).Value);
    }

    [Fact]
    public void LoadWithInvalidValueResetsToDefault()
    {
        var config = Create("z=2\n");
        var entry = config.Init("z", 0);
        File.WriteAllText(_path, "z=abc\n");
        config.Load();
        Assert.Equal(0, entry.Value);
    }

    [Fact]
    public void SaveCreatesFileForEmptyConfig()
    {
        var config = Create();
        config.Save();
        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void SaveRecreatesDeletedFile()
    {
        var config = Create();
        config.Init("number", 1);
        config.Save();
        File.Delete(_path);
        config.Save();
        Assert.Equal("number=1\n", File.ReadAllText(_path));
    }

    [Fact]
    public void JsonConverterRoundTrips()
    {
        var json = ConfigConverters.Json<Dictionary<string, LandBrush>>();
        var config = Create("bad={not json\n");
        config.Init("brushes", new Dictionary<string, LandBrush>(), json).Value = new()
        {
            ["Grass, wet"] = new LandBrush
            {
                Tiles = [3, 4],
                To = { ["Sand"] = [new() { TileID = 10, Direction = Direction.North | Direction.East }, new(11)] }
            }
        };
        var reopened = SaveAndReopen(config);
        Assert.Contains
        (
            "brushes={\"Grass, wet\":{\"Tiles\":[3,4],\"To\":{\"Sand\":[{\"TileID\":10,\"Direction\":5},{\"TileID\":11,\"Direction\":0}]}}}\n",
            File.ReadAllText(_path)
        );

        var brush = reopened.Init("brushes", new Dictionary<string, LandBrush>(), json).Value["Grass, wet"];
        Assert.Equal([3, 4], brush.Tiles);
        Assert.Equal
        (
            [(10, Direction.North | Direction.East), (11, Direction.None)],
            brush.To["Sand"].Select(t => (t.TileID, t.Direction))
        );
        Assert.Empty(reopened.Init("bad", new Dictionary<string, LandBrush>(), json).Value);
    }

    [Fact]
    public void UnsupportedTypeThrowsOnInit()
    {
        var config = Create();
        Assert.Throws<NotSupportedException>(() => config.Init("obj", new object()));
        Assert.Throws<NotSupportedException>(() => config.Init("brushes", new Dictionary<string, LandBrush>()));
    }
}
