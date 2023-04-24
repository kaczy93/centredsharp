using CentrED;
using CentrED.Client;

public class Program {
    public static void Main(string[] args) {
        CentrEDClient client = new CentrEDClient("localhost", 2597, "admin", "admin");
        LandTile landTile = client.GetLandTile(100, 100);
        landTile.Z += 5;
    }
}

