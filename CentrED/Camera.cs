using Microsoft.Xna.Framework;

namespace CentrED;

public class Camera
{
    public Camera()
    {
    }

    // Where the camera is looking, in "game/world" coordinates
    public Vector3 LookAt;

    // 1.0 is standard. Large zooms in, smaller zooms out.
    public float Zoom = 1.0f;

    // Camera rotation around the Z axis, in degrees
    public float Rotation = 0;

    public Rectangle ScreenSize;

    /* Game Y goes from top to bottom. Drawing Y from bottom to top. This just flips it over. */
    private Matrix _reflection = Matrix.CreateReflection(new Plane(0, -1, 0, 0));

    private Vector3 _up = new Vector3(-1, -1, 0);

    /* This takes the coordinates (x, y, z) and turns it into the screen point (x, y + z, z) */
    private Matrix _oblique = new Matrix(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 1, 1, 0,
                                0, 0, 0, 1);

    private Vector3 _position = new Vector3(0, 0, 128 * 4);

    public Matrix world;
    public Matrix view;
    public Matrix proj;
    

    public Matrix WorldViewProj { get; private set; }

    public void Update()
    {
        world = Matrix.CreateTranslation(-LookAt);
        world = Matrix.Multiply(world, _reflection);

        var up = Vector3.Transform(_up, Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation)));
        up = Vector3.Transform(up, _reflection);

        view = Matrix.CreateLookAt(_position, Vector3.Zero, up);

        Matrix ortho = Matrix.CreateOrthographic(ScreenSize.Width, ScreenSize.Height, -128 * 6, 128 * 6);

        Matrix scale = Matrix.CreateScale(Zoom, Zoom, 1f);

        Matrix translation = Matrix.CreateTranslation(new Vector3(0, 128 * 4, 0));

        proj = _oblique * translation * ortho * scale;

        Matrix worldViewProj;
        Matrix worldView;

        Matrix.Multiply(ref world, ref view, out worldView);
        Matrix.Multiply(ref worldView, ref proj, out worldViewProj);

        WorldViewProj = worldViewProj;
    }
}