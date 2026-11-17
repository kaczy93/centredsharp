using Hexa.NET.ImGui;
using static CentrED.CentrED;
using static CentrED.LangEntry;

namespace CentrED.UI.Windows;

public class ImageOverlayWindow() : Window(true)
{
    public override string Name => LangManager.Get(IMAGE_OVERLAY_WINDOW) + "###ImageOverlay";
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;
    
    protected override void InternalDraw()
    {
        var overlay = App.MapManager.ImageOverlay;

        ImGui.InputText(LangManager.Get(FILE_PATH), ref overlay.FilePath.Value, 512);
        ImGui.SameLine();
        if (ImGui.Button("..."))
        {
            if (TinyFileDialogs.TryOpenFile(
                LangManager.Get(SELECT_FILE),
                Environment.CurrentDirectory,
                ["*.png", "*.jpg", "*.jpeg", "*.bmp"],
                "Image files",
                false,
                out var newPath))
            {
                overlay.FilePath.Value = newPath;
            }
        }

        var hasTexture = overlay.Texture != null;
        ImGui.BeginDisabled(string.IsNullOrEmpty(overlay.FilePath.Value));
        if (ImGui.Button(LangManager.Get(IMAGE_OVERLAY_LOAD)))
        {
            overlay.LoadImage(App.GraphicsDevice);
        }
        ImGui.EndDisabled();

        ImGui.SameLine();
        ImGui.BeginDisabled(!hasTexture);
        if (ImGui.Button(LangManager.Get(IMAGE_OVERLAY_UNLOAD)))
        {
            overlay.UnloadImage();
        }
        ImGui.EndDisabled();

        ImGui.Separator();

        if (hasTexture)
        {
            ImGui.Text($"{LangManager.Get(IMAGE_OVERLAY_SIZE)}: {overlay.WidthInTiles:F1} x {overlay.HeightInTiles:F1}");
            ImGui.Text($"Image: {overlay.ImageWidth} x {overlay.ImageHeight} px");
        }
        else
        {
            ImGui.TextDisabled("No image loaded");
        }

        ImGui.Separator();

        ImGui.Checkbox(LangManager.Get(ENABLED), ref overlay.Enabled.Value);
        ImGui.Checkbox(LangManager.Get(IMAGE_OVERLAY_DRAW_ABOVE), ref overlay.DrawAboveTerrain.Value);

        var changed = false;
        var pos = new int[2];
        pos[0] = overlay.WorldX.Value;
        pos[1] = overlay.WorldY.Value;

        //TODO: Do we need inputInt2 or should we do two inputInt?
        if (ImGui.InputInt2(LangManager.Get(IMAGE_OVERLAY_POSITION), ref pos[0]))
        {
            overlay.WorldX.Value = pos[0];
            overlay.WorldY.Value = pos[1];
            changed = true;
        }
        changed |= ImGui.SliderFloat
            (LangManager.Get(IMAGE_OVERLAY_SCALE), ref overlay.Scale.Value, 0.1f, 10.0f, "%.2f");
        changed |= ImGui.SliderFloat
            (LangManager.Get(IMAGE_OVERLAY_OPACITY), ref overlay.Opacity.Value, 0.0f, 1.0f, "%.2f");
        changed |= ImGui.SliderFloat
            (LangManager.Get(IMAGE_OVERLAY_SCREEN), ref overlay.Screen.Value, 0.0f, 1.0f, "%.2f");

        if (ImGui.Button("Set to View Center"))
        {
            var tilePos = App.MapManager.TilePosition;
            overlay.WorldX.Value = tilePos.X;
            overlay.WorldY.Value = tilePos.Y;
            changed = true;
        }
        
        if(changed)
            overlay.UpdateVertices();
    }
}
