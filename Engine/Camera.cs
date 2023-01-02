namespace MyMiniEngine.Engine
{
    internal class Camera
    {
        public Point3d Coords { get; }
        public Point3d DirectionVector { get; }///coordinates of point that characterizes direction of camera  
        public short ResolWidth { get; }
        public short ResolHeight { get; }
        public Point3d[,] Grid { get; }
        public double[,] CameraDistancesToGrid { get; } /// distance from camera to grid[i, j]
        public short Frequency { get; }

        public Camera(Point3d coords, Point3d directionVector, short resolWidth, short resolHeight, short frequency)
        {
            Coords = coords;
            DirectionVector = directionVector;
            ResolWidth = resolWidth;
            ResolHeight = resolHeight;
            Frequency = frequency;
            Grid = new Point3d[ResolHeight, ResolWidth];
            CameraDistancesToGrid = new double[ResolHeight, ResolWidth];
            for (short i = 0; i < ResolHeight; i++)  //trying to go over rectangle that describes and contains the grid
                for (short j = 0; j < ResolWidth; j++)
                {
                    Grid[i, j] = new Point3d
                        (Coords.x + DirectionVector.x,
                        Coords.y + DirectionVector.y - ResolWidth / 2 * Frequency + j * Frequency,
                        Coords.z + DirectionVector.z + ResolHeight / 2 * Frequency - i * Frequency);
                    CameraDistancesToGrid[i, j] = Coords.GetDistance(Grid[i, j]);
                }
        }
    }
}
