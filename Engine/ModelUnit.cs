namespace MyMiniEngine.Engine
{
    internal struct ModelUnit
    {
        public Point3d Coords { get; set; }
        public Rgb Color { get; set; }
        public ModelUnit(Point3d coords, Rgb color)
        {
            Coords = coords;
            Color = color;
        }

        public ModelUnit(Point3d coords)
        {
            Coords = coords;
            Color = new Rgb(0, 0, 0);
        }
    }
}
