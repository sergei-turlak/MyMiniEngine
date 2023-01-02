using System;

namespace MyMiniEngine.Engine
{
    public struct Point3d
    {
        public double x, y, z;

        public Point3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static bool operator ==(Point3d p1, Point3d p2) => Math.Round(p1.x) == Math.Round(p2.x) && Math.Round(p1.y) == Math.Round(p2.y) && Math.Round(p1.z) == Math.Round(p2.z);
        public static bool operator !=(Point3d p1, Point3d p2) => Math.Round(p1.x) != Math.Round(p2.x) || Math.Round(p1.y) != Math.Round(p2.y) || Math.Round(p1.z) != Math.Round(p2.z);
    }
}
