namespace MyMiniEngine.Engine
{
    internal class SphereModel
    {
        public Point3d Center { get; set; }
        public double Radius { get; set; }
        public Rgb GeneralColor { get; set; }


        public SphereModel(Point3d center, double radius, Rgb generalColor)
        {
            Center = center;
            Radius = radius;
            GeneralColor = generalColor;
        }
    }
}
