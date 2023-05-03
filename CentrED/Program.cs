using CentrED;
using CentrED.Client;

public class Program {
    public static byte[,] array = {
        { 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1 },
        { 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1 },
        { 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1 },
        { 1, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1 },
        { 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 0, 0, 1, 1, 1, 1 },
    };
    public static void Main(string[] args) {
        using CentrEDClient client = new CentrEDClient("127.0.0.1", 2597, "autouser", "autopass");
        for(ushort y = 0; y < array.GetLength(0); y++)
            for(ushort x = 0; x < array.GetLength(1); x++)
                if(array[y,x] == 1)
                    client.AddStaticTile(new StaticTile(0xEEF, x,  y, 5,  0));
    }
}

