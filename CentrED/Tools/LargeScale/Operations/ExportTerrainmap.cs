using CentrED.Client;
using CentrED.Network;
using Hexa.NET.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using static CentrED.Application;
using static CentrED.LangEntry;

namespace CentrED.Tools.LargeScale.Operations;

public class ExportTerrainmap : LocalLargeScaleTool
{
    public override string Name => "Export Terrainmap";

    private string _exportFilePath = "";
    private Image<Rgb24>? _exportFile;

    private int xOffset;
    private int yOffset;

    private bool _coloredMode = true;
    private static readonly string[] _validFileFormats = [".png", ".bmp"];
    private static readonly string[] _validFileGlobPatterns = _validFileFormats.Select(t => "*" + t).ToArray();

    protected override bool DrawToolUI()
    {
        var changed = ImGui.InputText(LangManager.Get(FILE_PATH), ref _exportFilePath, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TrySaveFile
                    (LangManager.Get(SELECT_FILE), Environment.CurrentDirectory, _validFileGlobPatterns, null, out var newPath))
            {
                _exportFilePath = newPath;
                changed = true;
            }
        }
        return !changed;
    }

    public override bool CanSubmit(RectU16 area)
    {
        if (string.IsNullOrEmpty(_exportFilePath) || !_validFileFormats.Any(_exportFilePath.EndsWith))
        {
            _submitStatus = LangManager.Get(INVALID_FILE_FORMAT);
            return false;
        }

        try
        {
            using var file = File.OpenWrite(_exportFilePath);
        }
        catch (Exception e)
        {
            _submitStatus = string.Format(LangManager.Get(OPEN_FILE_ERROR_1INFO), e.Message);
            return false;
        }

        return true;
    }

    protected override void PreProcessArea(CentrEDClient client, RectU16 area)
    {
        base.PreProcessArea(client, area);
        _exportFile = new Image<Rgb24>(area.Width, area.Height);
        xOffset = area.X1;
        yOffset = area.Y1;
    }

    protected override void ProcessTile(CentrEDClient client, ushort x, ushort y)
    {
        var landTile = client.GetLandTile(x, y);

        var color = GetBiomeColor(landTile);
        _exportFile![x - xOffset, y - yOffset] = color;
    }

    protected override void PostProcessArea(CentrEDClient client, RectU16 area)
    {
        using var fileStream = File.OpenWrite(_exportFilePath);

        if (_exportFilePath.EndsWith(".png"))
            _exportFile!.Save(fileStream, new PngEncoder());
        else
            _exportFile!.Save(fileStream, new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel24 });
        _exportFile.Dispose();
        _exportFile = null;
    }

    private Rgb24 GetBiomeColor(LandTile tile)
    {
        var tileId = tile.Id;
        var z = tile.Z;
        var tileName = CEDGame.MapManager.UoFileManager.TileData.LandData[tileId].Name?.ToLowerInvariant() ?? "";

        // Normalize altitude from -128..127 to 0..1 for brightness calculation
        var altitudeFactor = (z + 128) / 255f;

        var biome = ClassifyBiome(tileName);
        return biome switch
        {
            Biome.Water => ApplyAltitude(new Rgb24(0, 50, 180), altitudeFactor, 0.3f),
            Biome.Sand => ApplyAltitude(new Rgb24(210, 180, 120), altitudeFactor, 0.4f),
            Biome.Grass => ApplyAltitude(new Rgb24(60, 140, 60), altitudeFactor, 0.4f),
            Biome.Dirt => ApplyAltitude(new Rgb24(140, 100, 60), altitudeFactor, 0.4f),
            Biome.Jungle => ApplyAltitude(new Rgb24(120, 180, 40), altitudeFactor, 0.4f),
            Biome.Forest => ApplyAltitude(new Rgb24(80, 120, 50), altitudeFactor, 0.4f),
            Biome.Swamp => ApplyAltitude(new Rgb24(70, 90, 50), altitudeFactor, 0.3f),
            Biome.Rock => GetRockGrayscale(altitudeFactor),
            Biome.Snow => GetSnowGrayscale(altitudeFactor),
            Biome.Lava => ApplyAltitude(new Rgb24(200, 60, 20), altitudeFactor, 0.3f),
            Biome.Cave => ApplyAltitude(new Rgb24(60, 50, 45), altitudeFactor, 0.3f),
            Biome.Void => new Rgb24(0, 0, 0),
            _ => GetDefaultGrayscale(altitudeFactor)
        };
    }

    private enum Biome
    {
        Water,
        Sand,
        Grass,
        Dirt,
        Jungle,
        Forest,
        Swamp,
        Rock,
        Snow,
        Lava,
        Cave,
        Void,
        Unknown
    }

    private static Biome ClassifyBiome(string tileName)
    {
        if (string.IsNullOrEmpty(tileName) || tileName == "nodraw")
            return Biome.Void;

        // Water
        if (tileName.Contains("water") || tileName.Contains("ocean") || tileName.Contains("lake") ||
            tileName.Contains("river") || tileName.Contains("pond") || tileName.Contains("sea"))
            return Biome.Water;

        // Snow (check before rock)
        if (tileName.Contains("snow") || tileName.Contains("ice") || tileName.Contains("frozen"))
            return Biome.Snow;

        // Rock/Stone/Mountain
        if (tileName.Contains("rock") || tileName.Contains("stone") || tileName.Contains("mountain") ||
            tileName.Contains("cliff") || tileName.Contains("boulder") || tileName.Contains("granite") ||
            tileName.Contains("marble") || tileName.Contains("cobble"))
            return Biome.Rock;

        // Cave/Dungeon
        if (tileName.Contains("cave") || tileName.Contains("dungeon") || tileName.Contains("brick") ||
            tileName.Contains("floor") || tileName.Contains("tile"))
            return Biome.Cave;

        // Sand/Desert/Beach
        if (tileName.Contains("sand") || tileName.Contains("desert") || tileName.Contains("beach") ||
            tileName.Contains("dune"))
            return Biome.Sand;

        // Jungle (before forest - more specific)
        if (tileName.Contains("jungle"))
            return Biome.Jungle;

        // Forest
        if (tileName.Contains("forest") || tileName.Contains("wood") || tileName.Contains("leaf") ||
            tileName.Contains("leaves"))
            return Biome.Forest;

        // Swamp
        if (tileName.Contains("swamp") || tileName.Contains("marsh") || tileName.Contains("bog") ||
            tileName.Contains("mud"))
            return Biome.Swamp;

        // Lava
        if (tileName.Contains("lava") || tileName.Contains("magma") || tileName.Contains("volcanic"))
            return Biome.Lava;

        // Dirt/Earth
        if (tileName.Contains("dirt") || tileName.Contains("earth") || tileName.Contains("soil") ||
            tileName.Contains("farmland") || tileName.Contains("field") || tileName.Contains("furrow"))
            return Biome.Dirt;

        // Grass (default for vegetation)
        if (tileName.Contains("grass") || tileName.Contains("lawn") || tileName.Contains("meadow") ||
            tileName.Contains("plain") || tileName.Contains("green"))
            return Biome.Grass;

        return Biome.Unknown;
    }

    private static Rgb24 ApplyAltitude(Rgb24 baseColor, float altitudeFactor, float variationStrength)
    {
        // Altitude factor: 0 = lowest (-128), 1 = highest (127)
        // Higher altitude = brighter, lower = darker
        var brightness = 1f - variationStrength + (altitudeFactor * variationStrength * 2f);
        brightness = Math.Clamp(brightness, 0.3f, 1.5f);

        return new Rgb24(
            (byte)Math.Clamp(baseColor.R * brightness, 0, 255),
            (byte)Math.Clamp(baseColor.G * brightness, 0, 255),
            (byte)Math.Clamp(baseColor.B * brightness, 0, 255)
        );
    }

    private static Rgb24 GetRockGrayscale(float altitudeFactor)
    {
        // Rock: black (0) to dark gray (127) based on altitude
        var value = (byte)(altitudeFactor * 127);
        return new Rgb24(value, value, value);
    }

    private static Rgb24 GetSnowGrayscale(float altitudeFactor)
    {
        // Snow: light gray (128) to white (255) based on altitude
        var value = (byte)(128 + altitudeFactor * 127);
        return new Rgb24(value, value, value);
    }

    private static Rgb24 GetDefaultGrayscale(float altitudeFactor)
    {
        // Unknown biomes: full grayscale range
        var value = (byte)(altitudeFactor * 255);
        return new Rgb24(value, value, value);
    }
}