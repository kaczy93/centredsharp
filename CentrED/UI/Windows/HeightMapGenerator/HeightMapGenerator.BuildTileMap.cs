using System.Linq;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private void BuildTileMap()
    {
        if (heightData == null)
            return;

        var groupsList = tileGroups.Values.Where(g => g.Ids.Count > 0).ToList();
        if (groupsList.Count == 0)
            return;

        tileMap = new Tile[mapSizeX, mapSizeY];
        for (int y = 0; y < mapSizeY; y++)
        {
            for (int x = 0; x < mapSizeX; x++)
            {
                var z = heightData[x, y];
                var candidates = groupsList.Where(g => z >= g.MinHeight && z <= g.MaxHeight).ToList();
                if (candidates.Count == 0)
                    candidates = groupsList;
                var grp = SelectGroup(candidates);
                ushort id = grp.Ids.Count > 0 ? grp.Ids[Random.Shared.Next(grp.Ids.Count)] : (ushort)0;
                tileMap[x, y] = new Tile(GetTerrainType(z), id);
            }
        }

        transitionConverter.ApplyTransitions(tileMap, transitionTiles);
    }

    private static TerrainType GetTerrainType(sbyte z)
    {
        for (int i = 0; i < HeightRanges.Length; i++)
        {
            var r = HeightRanges[i];
            if (z >= r.Min && z <= r.Max)
                return (TerrainType)i;
        }
        return TerrainType.Water;
    }
}
