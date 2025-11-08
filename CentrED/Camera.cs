using System.Numerics;
using Microsoft.Xna.Framework;
using Plane = System.Numerics.Plane;
using Rectangle = System.Drawing.Rectangle;
using Vector3 = System.Numerics.Vector3;

namespace CentrED;

public class Camera
{
    // 1.0 is standard. Large zooms in, smaller zooms out.
    public float Zoom = 1.0f;

    public Rectangle ScreenSize;

    private Matrix4x4 _mirrorX = Matrix4x4.CreateReflection(new Plane(-1, 0, 0, 0));

    private Vector3 _up = new(-1, -1, 0);

    /* This takes the coordinates (x, y, z) and turns it into the screen point (x, y + z, z) */
    private Matrix4x4 _oblique = new (1, 0, 0, 0, 
                                      0, 1, 0, 0, 
                                      0, 1, 1, 0, 
                                      0, 0, 0, 1);

    private Matrix4x4 _translation = Matrix4x4.CreateTranslation(new Vector3(0, 128 * 6, 0));

    public Vector3 Position = new(0, 0, 128 * 6);

    //Look directly below camera
    public Vector3 LookAt => new(Position.X, Position.Y, 0);
    
    public float Yaw;
    public float Pitch;
    public float Roll;

    public Matrix4x4 world;
    public Matrix4x4 view;
    public Matrix4x4 proj;

    public Matrix4x4 WorldViewProj { get; private set; }
    public Matrix FnaWorldViewProj { get; private set; } //We need this in few places
    
    public void ResetCamera()
    {
        Zoom = 1.0f;
        Yaw = 0f;
        Pitch = 0f;
        Roll = 0f;
    }

    public void ZoomIn(float delta)
    {
        Zoom = Math.Clamp(Zoom + delta, 0.2f, 4f);
    }

    public void Update()
    {
        //Tiles are in world coordinates
        world = Matrix4x4.Identity;

        view = Matrix4x4.CreateLookAt(Position, LookAt, _up);
        var ypr = Matrix4x4.CreateFromYawPitchRoll(float.DegreesToRadians(Yaw), float.DegreesToRadians(Pitch), float.DegreesToRadians(Roll));
        view = Matrix4x4.Multiply(view, ypr);

        Matrix4x4 ortho = Matrix4x4.CreateOrthographic(ScreenSize.Width, ScreenSize.Height, 0, 128 * 12);

        Matrix4x4 scale = Matrix4x4.CreateScale(Zoom, Zoom, 1f);

        proj = _mirrorX * _oblique * _translation * ortho * scale;
        
        var worldView = Matrix4x4.Multiply(world, view);
        WorldViewProj = Matrix4x4.Multiply(worldView, proj);
        FnaWorldViewProj = new Matrix(
            WorldViewProj.M11, WorldViewProj.M12, WorldViewProj.M13, WorldViewProj.M14,
            WorldViewProj.M21, WorldViewProj.M22, WorldViewProj.M23, WorldViewProj.M24,
            WorldViewProj.M31, WorldViewProj.M32, WorldViewProj.M33, WorldViewProj.M34,
            WorldViewProj.M41, WorldViewProj.M42, WorldViewProj.M43, WorldViewProj.M44
            );
    }
}