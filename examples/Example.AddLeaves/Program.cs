using CentrED;
using CentrED.Client;
using CentrED.Network;

var start = DateTime.Now;

Dictionary<ushort, ushort> treeToLeaves = new Dictionary<ushort, ushort>
{
    {0x0CCD, 0x0CCE}, {0x0CD0, 0x0CD1}, {0x0CD3, 0x0CD4}
};
ushort x1 = 0;
ushort y1 = 0;
ushort x2 = 100;
ushort y2 = 100;

CentrEDClient client = new CentrEDClient();
client.Connect("127.0.0.1", 2597, "user", "password");

client.LoadBlocks(new AreaInfo(x1, y1, x2, y2));

foreach (var (x,y) in new TileRange(x1,y1,x2,y2))
{
    StaticTile tree = null;
    StaticTile leaves = null;
    foreach (var tile in client.GetStaticTiles(x, y))
    {
        if (treeToLeaves.ContainsKey(tile.Id))
        {
            tree = tile;
        }else if (treeToLeaves.Values.Contains(tile.Id))
        {
            leaves = tile;
        }
    }
    if (tree != null && leaves == null)
    {
        client.Add(new StaticTile(treeToLeaves[tree.Id], tree.X, tree.Y, tree.Z, 0));
    }
    client.Update();
}
client.Disconnect();

Console.WriteLine($"Elapsed: {(DateTime.Now - start).TotalMilliseconds}ms");