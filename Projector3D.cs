using System.Globalization;

class Projector3D
{
    // Eye (camera) setup for 3D rendering
    // Eye always looks toward to a fix point (e.g. center point)
    // eyeOutDir is the direction from this point to the camera (unit vector).
    // eyeBaseDistance is the distance from this point
    double[] fixPoint = { 0.5, 0.5, 0.5 };
    double[] eyeOutDirRaw = { 1.0, 0.7, 1.4 };
    double eyeBaseDistance = 2.0; //enough large distance for starting
    double focalLength = 1.2;   // perspective "field of view" tuning

    // Pre-computed orthonormal eye (camera) basis (setup in InitViewpoint)
    double[] eyeForward; // eye's forward axis (toward fixed point, e.g. center)
    double[] eyeDir;     // oposite (direction of eye from fixed point)
    double[] eyeRight;   // eye's right axis
    double[] eyeUp;      // eye's up axis


    // ---------------------------------------------------------------
    // Constuctor
    // ---------------------------------------------------------------

    /// <summary>
    ///  Initialize the 3D porjector object with an optional setup
    /// </summary>
    /// <param name="optional_setup">A string which conatins raw numbers for setup.
    /// E.g. "0.5 0.5 0.5 1.0 0.7 1.4 2.0 1.2". See the code to understand it!
    /// </param>
    public Projector3D(string optional_setup = "")
    {
        //decompose the optional input string into setup variables (if possible)
        double[] numbers;
        try 
        { 
            numbers = optional_setup
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture))
            .ToArray();
        }
        catch (Exception e)
        {
            numbers = Array.Empty<double>();
        }
        if (numbers.Length == 8)
        {
            Array.Copy(numbers, 0, fixPoint, 0, 3);
            Array.Copy(numbers, 3, eyeOutDirRaw, 0, 3);
            eyeBaseDistance = numbers[6];
            focalLength = numbers[7];
        }
        // Do initialization
        InitViewpoint();
    }

    // ---------------------------------------------------------------
    // Viewpoint (eye, camera) initialisation
    // ---------------------------------------------------------------
    void InitViewpoint()
    {
        eyeDir = Normalize(eyeOutDirRaw); // outward unit vector from fix-point
        eyeForward = Neg(eyeDir); // and toward fix-point

        // Select a world "up" direction that is not parallel to eyeForward
        // First, try to set it so that the "up" direction is parallel to the Z-axis;
        // if that is not possible, then parallel to the Y-axis.
        double[] worldUp = { 0.0, 0.0, 1.0 };
        if (Math.Abs(Dot(eyeForward, worldUp)) > 0.99)
            worldUp = new double[] { 0.0, 1.0, 0.0 };
        // So viewer’s "upward" and "rightward" directions are necessarily:
        eyeRight = Normalize(Cross(eyeForward, worldUp));
        eyeUp = Cross(eyeRight, eyeForward);
    }

    // ---------------------------------------------------------------
    // Vector helpers
    // ---------------------------------------------------------------
    static double[] Normalize(double[] v)
    {
        double len = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        return new double[] { v[0] / len, v[1] / len, v[2] / len };
    }
    static double[] Neg(double[] v) => new double[] { -v[0], -v[1], -v[2] };
    static double Dot(double[] a, double[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    static double[] Cross(double[] a, double[] b) => new double[]
    {
            a[1]*b[2] - a[2]*b[1],
            a[2]*b[0] - a[0]*b[2],
            a[0]*b[1] - a[1]*b[0]
    };

    // ---------------------------------------------------------------
    // Perspective projection of a 3D point -> 2D screen
    // Returns (NaN, NaN) if the point is behind the eye (camera)
    // ---------------------------------------------------------------
    public (double sx, double sy) Project3D(double wx, double wy, double wz, int screenW, int screenH)
    {
        // Eye position
        double dist = eyeBaseDistance;
        double eyeX = fixPoint[0] + eyeDir[0] * dist;
        double eyeY = fixPoint[1] + eyeDir[1] * dist;
        double eyeZ = fixPoint[2] + eyeDir[2] * dist;

        // Vector from eye to world point
        double rx = wx - eyeX;
        double ry = wy - eyeY;
        double rz = wz - eyeZ;

        // Transform to eye-sight axes (rotate world 3D into viewer 3D space)
        double x = eyeRight[0] * rx + eyeRight[1] * ry + eyeRight[2] * rz;
        double y = eyeUp[0] * rx + eyeUp[1] * ry + eyeUp[2] * rz;
        double z = eyeForward[0] * rx + eyeForward[1] * ry + eyeForward[2] * rz;

        if (z <= 1e-4) return (double.NaN, double.NaN);   // behind eye

        // Perspective; scale so the view fills the shorter screen dimension
        double s = focalLength * Math.Min(screenW, screenH);
        double sx = x / z * s + screenW * 0.5;
        double sy = -y / z * s + screenH * 0.5; //screen Y is flipped

        return (sx, sy);
    }



}
