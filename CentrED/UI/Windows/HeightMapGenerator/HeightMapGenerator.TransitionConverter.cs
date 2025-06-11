using System;
using System.Collections.Generic;

namespace CentrED.UI.Windows;

public partial class HeightMapGenerator
{
    private enum TerrainType
    {
        Water,
        Sand,
        Grass,
        Jungle,
        Rock,
        Snow
    }

    private struct Tile
    {
        public TerrainType Type;
        public ushort Id;

        public Tile(TerrainType type, ushort id)
        {
            Type = type;
            Id = id;
        }
    }

    private class TransitionConverter
    {
        private static readonly Dictionary<(int dx, int dy), int> IndexMap = new()
        {
            {(-1, -1), 0}, // NW
            {(0, -1), 1},  // N
            {(1, -1), 2},  // NE
            {(-1, 0), 3},  // W
            {(0, 0), 4},   // Center
            {(1, 0), 5},   // E
            {(-1, 1), 6},  // SW
            {(0, 1), 7},   // S
            {(1, 1), 8}    // SE
        };

        public void ApplyTransitions(Tile[,] map, Dictionary<string, Tile[]> transitionTiles)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            Tile[,] copy = (Tile[,])map.Clone();

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    var center = copy[x, y];
                    var counts = new Dictionary<TerrainType, int>();

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;
                            var t = copy[x + dx, y + dy];
                            if (t.Type == center.Type)
                                continue;
                            counts.TryGetValue(t.Type, out int c);
                            counts[t.Type] = c + 1;
                        }
                    }

                    if (counts.Count == 0)
                        continue;

                    var bType = TerrainType.Water;
                    int max = 0;
                    foreach (var kv in counts)
                    {
                        if (kv.Value > max)
                        {
                            max = kv.Value;
                            bType = kv.Key;
                        }
                    }

                    if (max == 0)
                        continue;

                    int bestIndex = 4;
                    int bestCount = 0;
                    foreach (var kv in IndexMap)
                    {
                        var (dx, dy) = kv.Key;
                        if (dx == 0 && dy == 0)
                            continue;
                        var t = copy[x + dx, y + dy];
                        if (t.Type == bType)
                        {
                            int idx = kv.Value;
                            int count = 1;
                            if (count > bestCount)
                            {
                                bestCount = count;
                                bestIndex = idx;
                            }
                        }
                    }

                    var key = $"{center.Type.ToString().ToLower()}-{bType.ToString().ToLower()}";
                    if (transitionTiles.TryGetValue(key, out var tiles) && tiles.Length == 9)
                    {
                        map[x, y] = tiles[bestIndex];
                    }
                }
            }
        }
    }

    private readonly TransitionConverter transitionConverter = new();
}
