using System.Numerics;
using CentrED.Renderer;
using Microsoft.Xna.Framework.Graphics;
using static CentrED.CentrED;
using static CentrED.Constants;

namespace CentrED.Map;

public class ImageOverlay : MapObject
{
    private const string CONFIG_PREFIX = "image_overlay";
    
    public ImageOverlay(GraphicsDevice gd)
    {
        for (int i = 0; i < 4; i++)
        {
            Vertices[i] = new MapVertex(Vector3.Zero, Vector3.Zero, Vector4.Zero, Vector3.Zero);
        }
        LoadImage(gd);
    }
    
    public readonly ConfigValue<bool> Enabled = App.Config.Init($"{CONFIG_PREFIX}.enabled", false);
    public readonly ConfigValue<string> FilePath = App.Config.Init($"{CONFIG_PREFIX}.file", "");
    public readonly ConfigValue<bool> DrawAboveTerrain = App.Config.Init($"{CONFIG_PREFIX}.draw_above_terrain", false);
    public readonly ConfigValue<int> WorldX = App.Config.Init($"{CONFIG_PREFIX}.world_x", 0);
    public readonly ConfigValue<int> WorldY = App.Config.Init($"{CONFIG_PREFIX}.world_y", 0);
    public readonly ConfigValue<float> Scale = App.Config.Init($"{CONFIG_PREFIX}.scale", 1.0f);
    public readonly ConfigValue<float> Opacity = App.Config.Init($"{CONFIG_PREFIX}.opacity", 1.0f);
    public readonly ConfigValue<float> Screen = App.Config.Init($"{CONFIG_PREFIX}.screen", 0.0f);

    public int ImageWidth => Texture?.Width ?? 0;
    public int ImageHeight => Texture?.Height ?? 0;
    public float WidthInTiles => ImageWidth * Scale.Value;
    public float HeightInTiles => ImageHeight * Scale.Value;

    public void LoadImage(GraphicsDevice gd)
    {
        UnloadImage();

        if (!File.Exists(FilePath.Value))
        {
            Console.WriteLine($"[ImageOverlay] File {FilePath.Value} does not exist");
            return;
        }

        try
        {
            using var fileStream = File.OpenRead(FilePath.Value);
            Texture = Texture2D.FromStream(gd, fileStream);
            TextureBounds = new System.Drawing.Rectangle(0, 0, Texture.Width, Texture.Height);
            UpdateVertices();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Texture = null;
            TextureBounds = default;
        }
    }

    public void UnloadImage()
    {
        if (Texture != null)
        {
            Texture.Dispose();
            Texture = null!;
        }
    }

    public void UpdateVertices()
    {
        if (Texture == null)
            return;

        float width = WidthInTiles * TILE_SIZE;
        float height = HeightInTiles * TILE_SIZE;
        float x = WorldX.Value * TILE_SIZE;
        float y = WorldY.Value * TILE_SIZE;

        Vertices[0].Position = new Vector3(x, y, 0);
        Vertices[0].Texture = new Vector3(0, 0, 0);
        Vertices[1].Position = new Vector3(x + width, y, 0);
        Vertices[1].Texture = new Vector3(1, 0, 0);
        Vertices[2].Position = new Vector3(x, y + height, 0);
        Vertices[2].Texture = new Vector3(0, 1, 0);
        Vertices[3].Position = new Vector3(x + width, y + height, 0);
        Vertices[3].Texture = new Vector3(1, 1, 0);

        for (int i = 0; i < 4; i++)
        {
            Vertices[i].Hue = new Vector4(0, Screen.Value, Opacity.Value, 0);
            Vertices[i].Normal = Vector3.Zero;
        }
    }
}
