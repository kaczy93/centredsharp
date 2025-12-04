using CentrED.Client;
using CentrED.IO.Models;
using CentrED.Network;
using CentrED.UI;
using CentrED.Utils;

namespace CentrED.Tools.LargeScale.Operations;

/// <summary>
/// Coastline generation for ImportColoredHeightmap.
/// Pattern: [G|F] → [D(transição)] → [0x0095 + Onda] → [W]
///
/// Instead of shrinking the land, we shrink the water:
/// Find water tiles adjacent to land and replace them with shore (0x0095) + wave.
/// </summary>
public partial class ImportColoredHeightmap
{
    private bool _applyCoastline = true;
    private int _coastlineProcessed = 0;
    private int _coastlineAdded = 0;
    private int _coastlineTerrainModified = 0;

    // Water tiles to process (convert to shore + wave)
    private static readonly HashSet<ushort> CoastWaterTiles = [0x00A8, 0x00A9, 0x00AA, 0x00AB, 0x0136, 0x0137];

    // Tiles to ignore when checking for land (water + shore = not land)
    private static readonly HashSet<ushort> CoastNonLandTiles = [0x00A8, 0x00A9, 0x00AA, 0x00AB, 0x0136, 0x0137, 0x0095];

    // Wave statics mapped by simplified direction (where water is)
    // Cardinal directions
    private static readonly Dictionary<Direction, ushort[]> CardinalWaves = new()
    {
        { Direction.West, [0x179D, 0x179E] },   // Water to West
        { Direction.South, [0x179F, 0x17A0] },  // Water to South
        { Direction.North, [0x17A1, 0x17A2] },  // Water to North
        { Direction.East, [0x17A3, 0x17A4] },   // Water to East
        { Direction.Left, [0x17A9] },           // Water to Left (NW in iso)
        { Direction.Down, [0x17AC] },           // Water to Down (SW in iso)
        { Direction.Up, [0x17A7] },             // Water to Up (NE in iso)
        { Direction.Right, [0x17AB] },          // Water to Right (SE in iso)
    };

    // Corner waves - for when water is in a diagonal direction
    private static readonly ushort[] CornerSW = [0x17A9];  // Water to SW
    private static readonly ushort[] CornerNW = [0x17AC];  // Water to NW (was 0x17AA)
    private static readonly ushort[] CornerNE = [0x17AB];  // Water to NE
    private static readonly ushort[] CornerSE = [0x17AA];  // Water to SE (was 0x17AC)

    // Fallback water object statics
    private static readonly ushort[] FallbackWaveStatics = [0x1797, 0x1798, 0x1799, 0x179A, 0x179B, 0x179C];

    private void ApplyCoastlineToArea(CentrEDClient client, RectU16 area)
    {
        if (!_applyCoastline)
            return;

        Console.WriteLine($"Coastline: Starting processing area {area.X1},{area.Y1} to {area.X2},{area.Y2}");

        foreach (var (x, y) in new TileRange(area))
        {
            ApplyCoastline(client, x, y);

            // Progress update every 1000 tiles
            if (_coastlineProcessed % 1000 == 0)
            {
                Console.WriteLine($"Coastline: processed {_coastlineProcessed}, modified {_coastlineTerrainModified}, waves {_coastlineAdded}");
                client.Update();
            }
        }

        Console.WriteLine($"Coastline: Finished. processed {_coastlineProcessed}, modified {_coastlineTerrainModified}, waves {_coastlineAdded}");
    }

    private void ApplyCoastline(CentrEDClient client, ushort x, ushort y)
    {
        _coastlineProcessed++;

        var landTile = client.GetLandTile(x, y);
        var tileId = landTile.Id;

        // Only process water tiles
        if (!CoastWaterTiles.Contains(tileId))
            return;

        // Get direction where LAND is (opposite of water direction detection)
        var landDirection = GetLandDirection(client, x, y);
        if (landDirection == Direction.None)
            return;

        // Replace water with shore tile (0x0095) at Z=-15
        landTile.ReplaceLand(0x0095, -15);
        _coastlineTerrainModified++;

        // Get appropriate wave static based on land direction
        ushort waveStaticId = GetWaveStaticForDirection(landDirection);

        if (waveStaticId != 0)
        {
            var waveTile = new StaticTile(waveStaticId, x, y, -5, 0);
            client.Add(waveTile);
            _coastlineAdded++;
        }
    }

    /// <summary>
    /// Get direction flags indicating where LAND tiles are relative to this water tile.
    /// </summary>
    private Direction GetLandDirection(CentrEDClient client, ushort x, ushort y)
    {
        var result = Direction.None;

        foreach (var dir in DirectionHelper.All)
        {
            var offset = dir.Offset();
            var nx = x + offset.Item1;
            var ny = y + offset.Item2;

            if (nx < 0 || ny < 0 || nx > ushort.MaxValue || ny > ushort.MaxValue)
                continue;

            try
            {
                var neighbor = client.GetLandTile((ushort)nx, (ushort)ny);
                // If neighbor is NOT water/shore, it's land
                if (!CoastNonLandTiles.Contains(neighbor.Id))
                {
                    result |= dir;
                }
            }
            catch { }
        }

        return result;
    }

    /// <summary>
    /// Get appropriate wave static ID based on direction where land is.
    /// We detect where LAND is, but wave tiles are designed for "where WATER is from land".
    /// So we need to reverse the direction.
    /// </summary>
    private ushort GetWaveStaticForDirection(Direction landDirection)
    {
        // Reverse: land direction -> water direction (opposite)
        var waveDir = landDirection.Reverse();

        // Check for corner cases first (diagonal directions)
        // These corner tiles fill the diagonal gaps
        if (waveDir.HasFlag(Direction.South) && waveDir.HasFlag(Direction.West))
            return CornerSW[Random.Shared.Next(CornerSW.Length)];

        if (waveDir.HasFlag(Direction.West) && waveDir.HasFlag(Direction.North))
            return CornerNW[Random.Shared.Next(CornerNW.Length)];

        if (waveDir.HasFlag(Direction.North) && waveDir.HasFlag(Direction.East))
            return CornerNE[Random.Shared.Next(CornerNE.Length)];

        if (waveDir.HasFlag(Direction.South) && waveDir.HasFlag(Direction.East))
            return CornerSE[Random.Shared.Next(CornerSE.Length)];

        // Check cardinal directions
        foreach (var (dir, tiles) in CardinalWaves)
        {
            if (waveDir.HasFlag(dir))
            {
                return tiles[Random.Shared.Next(tiles.Length)];
            }
        }

        // Fallback to generic water wave
        if (waveDir != Direction.None)
        {
            return FallbackWaveStatics[Random.Shared.Next(FallbackWaveStatics.Length)];
        }

        return 0;
    }
}
