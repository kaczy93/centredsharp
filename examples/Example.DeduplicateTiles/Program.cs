using CentrED;
using CentrED.Client;
using CentrED.Network;

var start = DateTime.Now;

ushort[] duplicatedTiles = [0x1CD9, 0x1CDA, 0x1CDB, 0x1CDC];
ushort x1 = 0;
ushort y1 = 0;
ushort x2 = 100;
ushort y2 = 100;

CentrEDClient client = new CentrEDClient();
client.Connect("127.0.0.1", 2597, "user", "password");

client.LoadBlocks(new RectU16(x1, y1, x2, y2));

foreach (var (x,y) in new TileRange(x1,y1,x2,y2))
{
    if(client.TryGetStaticTiles(x,y, out var statics))
    {
        var filtered = statics.Where(tile => duplicatedTiles.Contains(tile.Id)).ToArray();
        for (var i = 1; i < filtered.Length; i++)
        {
            client.Remove(filtered[i]);
        }
    }
    client.Update();
}
client.Disconnect();

Console.WriteLine($"Elapsed: {(DateTime.Now - start).TotalMilliseconds}ms");