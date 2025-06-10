using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CentrED.Client;
using CentrED.Network;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

if (args.Length < 5)
{
    Console.WriteLine("Usage: HeightMapCLI <host> <port> <user> <pass> <heightmap> [groups.json] [quadrant]");
    return;
}

string host = args[0];
int port = int.Parse(args[1]);
string user = args[2];
string pass = args[3];
string heightMapPath = args[4];
string groupsPath = args.Length > 5 ? args[5] : "heightmap_groups.json";
int quadrant = args.Length > 6 ? int.Parse(args[6]) : 0;

Console.WriteLine($"Connecting to {host}:{port}...");
CentrEDClient client = new();
client.Connect(host, port, user, pass);

Console.WriteLine($"Loading heightmap: {heightMapPath}");
using var image = Image.Load<Rgba32>(heightMapPath);

Dictionary<string, Group> groups = new();
if (File.Exists(groupsPath))
{
    Console.WriteLine($"Loading groups: {groupsPath}");
    groups = JsonSerializer.Deserialize<Dictionary<string, Group>>(File.ReadAllText(groupsPath), new JsonSerializerOptions { IncludeFields = true }) ?? new();
}

var groupsList = groups.Values.Where(g => g.Ids.Count > 0).ToList();
if (groupsList.Count == 0)
{
    Console.WriteLine("No groups configured. Aborting.");
    client.Disconnect();
    return;
}

HeightMapGeneratorCLI generator = new(client, image, groups, quadrant);
Console.WriteLine("Generating...");
generator.Generate();

client.Disconnect();
Console.WriteLine("Done.");

internal class HeightMapGeneratorCLI
{
    private readonly CentrEDClient _client;
    private readonly Image<Rgba32> _image;
    private readonly Dictionary<string, Group> _groups;
    private readonly int _quadrant;

    private const int MapSize = 4096;
    private const int BlockSize = 256;
    private const int MaxTiles = 16 * 1024 * 1024;
    private static readonly (sbyte Min, sbyte Max)[] HeightRanges =
    {
        (-127, -126),
        (-125, -100),
        (-99, -74),
        (-73, -48),
        (-47, -23),
        (-22, 3)
    };
    private const int NUM_CHANNELS = 6;
    private const float NOISE_SCALE = 0.05f;
    private const float NOISE_ROUGHNESS = 0.5f;
    private const int SMOOTH_RADIUS = 64;

    private sbyte[,]? _heightData;
    private readonly Perlin _noise = new(Environment.TickCount);

    public HeightMapGeneratorCLI(CentrEDClient client, Image<Rgba32> image, Dictionary<string, Group> groups, int quadrant)
    {
        _client = client;
        _image = image;
        _groups = groups;
        _quadrant = quadrant;
    }

    public void Generate()
    {
        UpdateHeightData();
        if (_heightData == null)
            return;

        var groupsList = _groups.Values.Where(g => g.Ids.Count > 0).ToList();
        if (groupsList.Count == 0)
        {
            Console.WriteLine("No groups configured. Aborting.");
            return;
        }

        var total = MapSize * MapSize;
        if (total > MaxTiles)
            return;

        _client.BulkMode = true;
        try
        {
            GenerateFractalRegion(0, 0, MapSize, MapSize, groupsList, total);
        }
        finally
        {
            _client.BulkMode = false;
            _client.Flush();
            _client.Update();
        }
    }

    private void UpdateHeightData()
    {
        int quadWidth = _image.Width / 3;
        int quadHeight = _image.Height / 3;
        int qx = _quadrant % 3;
        int qy = _quadrant / 3;

        _heightData = new sbyte[MapSize, MapSize];
        int[,] idxMap = new int[MapSize, MapSize];
        for (int y = 0; y < MapSize; y++)
        {
            int sy = qy * quadHeight + (int)(y / (float)MapSize * quadHeight);
            for (int x = 0; x < MapSize; x++)
            {
                int sx = qx * quadWidth + (int)(x / (float)MapSize * quadWidth);
                var c = _image[sx, sy];
                int rawIndex = c.R / (256 / NUM_CHANNELS);
                idxMap[x, y] = Math.Clamp(rawIndex, 0, NUM_CHANNELS - 1);
            }
        }

        int[,] distMap = new int[MapSize, MapSize];
        var queue = new Queue<(int X, int Y)>();
        for (int y = 0; y < MapSize; y++)
        {
            for (int x = 0; x < MapSize; x++)
            {
                if (idxMap[x, y] == 0)
                {
                    distMap[x, y] = 0;
                    queue.Enqueue((x, y));
                }
                else
                {
                    distMap[x, y] = int.MaxValue;
                }
            }
        }

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            int nd = distMap[cx, cy] + 1;
            if (nd > SMOOTH_RADIUS)
                continue;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = cy + dy;
                if (ny < 0 || ny >= MapSize) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    if (nx < 0 || nx >= MapSize) continue;
                    if (nd < distMap[nx, ny])
                    {
                        distMap[nx, ny] = nd;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        for (int y = 0; y < MapSize; y++)
        {
            for (int x = 0; x < MapSize; x++)
            {
                int idx = idxMap[x, y];
                var range = HeightRanges[idx];
                int z;

                bool isEdge = false;
                for (int dy = -1; dy <= 1 && !isEdge; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= MapSize) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        if (nx < 0 || nx >= MapSize) continue;
                        if (idxMap[nx, ny] != idx)
                        {
                            isEdge = true;
                            break;
                        }
                    }
                }

                if (idx == 0)
                {
                    z = -127; // água plana e constante
                }
                else
                {
                    float n = _noise.Fractal(x * NOISE_SCALE, y * NOISE_SCALE, NOISE_ROUGHNESS);
                    float t = (n + 1f) * 0.5f;
                    z = (int)MathF.Round(range.Min + t * (range.Max - range.Min));
                    if (isEdge)
                    {
                        float edgePerturb = _noise.Noise(x * 0.3f, y * 0.3f);
                        z += (int)(edgePerturb * 3);
                    }

                    int dist = distMap[x, y];
                    if (dist <= SMOOTH_RADIUS)
                    {
                        if (dist <= 1)
                            z = -126;
                        else if (dist == 2)
                            z = -125;
                        else
                        {
                            float lerpT = (dist - 2) / (float)(SMOOTH_RADIUS - 2);
                            z = (int)MathF.Round(Lerp(-125, z, lerpT));
                        }
                    }
                }

                _heightData[x, y] = (sbyte)Math.Clamp(z, -127, 127);
            }
        }
    }

    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private void GenerateFractalRegion(int startX, int startY, int width, int height, List<Group> groupsList, float total)
    {
        if (width <= BlockSize && height <= BlockSize)
        {
            GenerateArea(startX, startY, width, height, groupsList, total);
            return;
        }

        int stepX = width / 3;
        int stepY = height / 3;
        int remX = width % 3;
        int remY = height % 3;

        int offY = startY;
        for (int qy = 0; qy < 3; qy++)
        {
            int h = stepY + (qy < remY ? 1 : 0);
            int offX = startX;
            for (int qx = 0; qx < 3; qx++)
            {
                int w = stepX + (qx < remX ? 1 : 0);
                if (w == 0 || h == 0)
                {
                    Console.WriteLine($"Skipping zero-sized region {offX},{offY} size {w}x{h}");
                    continue;
                }
                GenerateFractalRegion(offX, offY, w, h, groupsList, total);
                offX += w;
            }
            offY += h;
        }
    }

    private void GenerateArea(int startX, int startY, int width, int height, List<Group> groupsList, float total)
    {
        int endX = Math.Min(MapSize - 1, startX + width - 1);
        int endY = Math.Min(MapSize - 1, startY + height - 1);

        for (int bx = startX; bx <= endX; bx += BlockSize)
        {
            int ex = Math.Min(endX, bx + BlockSize - 1);
            for (int by = startY; by <= endY; by += BlockSize)
            {
                int ey = Math.Min(endY, by + BlockSize - 1);
                _client.LoadBlocks(new AreaInfo((ushort)bx, (ushort)by, (ushort)ex, (ushort)ey));
                for (int x = bx; x <= ex; x++)
                {
                    for (int y = by; y <= ey; y++)
                    {
                        if (!_client.TryGetLandTile(x, y, out var landTile))
                            continue;
                        var z = _heightData![x, y];
                        var candidates = groupsList.Where(g => z >= g.MinHeight && z <= g.MaxHeight).ToList();
                        if (candidates.Count == 0)
                            candidates = groupsList;
                        if (candidates.Count > 0)
                        {
                            var grp = SelectGroup(candidates);
                            var id = grp.Ids[Random.Shared.Next(grp.Ids.Count)];
                            landTile.ReplaceLand(id, z);
                        }
                    }
                }
            }
        }

        _client.Flush();
        _client.Update();
    }

    private static Group SelectGroup(List<Group> groups)
    {
        var total = groups.Sum(g => g.Chance);
        var val = Random.Shared.NextDouble() * total;
        float acc = 0f;
        foreach (var g in groups)
        {
            acc += g.Chance;
            if (val <= acc)
                return g;
        }
        return groups[0];
    }
}

internal class Group
{
    public float Chance = 100f;
    public sbyte MinHeight = -128;
    public sbyte MaxHeight = 127;
    public List<ushort> Ids = new();
}

internal class Perlin
{
    private readonly int[] p = new int[512];
    public Perlin(int seed)
    {
        var perm = Enumerable.Range(0, 256).ToArray();
        var rnd = new Random(seed);
        for (int i = 0; i < 256; i++)
        {
            var j = rnd.Next(i, 256);
            (perm[i], perm[j]) = (perm[j], perm[i]);
            p[i] = perm[i];
            p[i + 256] = perm[i];
        }
    }
    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 3;
        float u = h < 2 ? x : y;
        float v = h < 2 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
    public float Noise(float x, float y)
    {
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);
        int a = p[xi] + yi;
        int b = p[xi + 1] + yi;
        float u = Fade(xf);
        float v = Fade(yf);
        return Lerp(v,
            Lerp(u, Grad(p[a], xf, yf), Grad(p[b], xf - 1, yf)),
            Lerp(u, Grad(p[a + 1], xf, yf - 1), Grad(p[b + 1], xf - 1, yf - 1)));
    }
    public float Fractal(float x, float y, float roughness)
    {
        float total = 0;
        float amplitude = 1;
        float frequency = 1;
        float max = 0;
        for (int i = 0; i < 4; i++)
        {
            total += Noise(x * frequency, y * frequency) * amplitude;
            max += amplitude;
            amplitude *= roughness;
            frequency *= 2;
        }
        return total / max;
    }
}
