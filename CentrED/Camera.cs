using Microsoft.Xna.Framework;

namespace CentrED;

public class Camera
{
    // 1.0 is standard. Large zooms in, smaller zooms out.
    public float Zoom = 1.0f;

    // Camera rotation around the Z axis, in degrees
    public float Rotation = 0;

    public Rectangle ScreenSize;

    private Matrix _mirrorX = Matrix.CreateReflection(new Plane(-1, 0, 0, 0));

    private Vector3 _up = new(-1, -1, 0);

    /* This takes the coordinates (x, y, z) and turns it into the screen point (x, y + z, z) */
    private Matrix _oblique = new Matrix(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 1, 1, 0,
                                0, 0, 0, 1);

    private Matrix _translation = Matrix.CreateTranslation(new Vector3(0, 128 * 4, 0));

    public Vector3 Position = new(0, 0, 128 * 4);
    
    //Look directly below camera
    public Vector3 LookAt => new(Position.X, Position.Y, 0);

    public Matrix world;
    public Matrix view;
    public Matrix proj;

    public Matrix WorldViewProj { get; private set; }

    public bool Moved;

    public void Move(float xDelta, float yDelta) {
        Position.X += xDelta;
        Position.Y += yDelta;
        Moved = true;
    }

    public void ZoomIn(float delta) {
        Zoom += delta;
        Moved = true;
    }

    public void Update()
    {
        //Tiles are in world coordinates
        world = Matrix.Identity;

        var up = Vector3.Transform(_up, Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation)));

        view = Matrix.CreateLookAt(Position, LookAt, up);

        Matrix ortho = Matrix.CreateOrthographic(ScreenSize.Width, ScreenSize.Height, 0, 128 * 8);

        Matrix scale = Matrix.CreateScale(Zoom, Zoom, 1f);
        
        proj = _mirrorX * _oblique * _translation * ortho * scale;

        Matrix.Multiply(ref world, ref view, out var worldView);
        Matrix.Multiply(ref worldView, ref proj, out var worldViewProj);

        WorldViewProj = worldViewProj;
        Moved = false;
    }
}