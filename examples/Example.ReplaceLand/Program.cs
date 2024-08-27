using CentrED.Client;
using CentrED.Network;

var start = DateTime.Now;

ushort[] grassTiles = [0x3, 0x4, 0x5, 0x6];
ushort x1 = 0;
ushort y1 = 0;
ushort x2 = 100;
ushort y2 = 100;

CentrEDClient client = new CentrEDClient();
client.Connect("127.0.0.1", 2597, "user", "password");

client.LoadBlocks(new AreaInfo(x1, y1, x2, y2));
for (var x = x1; x < x2; x++)
{
    for (var y = y1; y < y2; y++)
    {
        if(client.TryGetLandTile(x,y, out var landTile))
        {
            landTile.Id = grassTiles[Random.Shared.Next(grassTiles.Length)];
        }
        client.Update();
    }
}
client.Disconnect();

Console.WriteLine($"Elapsed: {(DateTime.Now - start).TotalMilliseconds}ms");