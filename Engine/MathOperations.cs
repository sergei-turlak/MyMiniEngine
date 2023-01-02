using System;

namespace MyMiniEngine.Engine
{
    public static class MathOperations
    {
        public static double GetDistance(this Point3d p1, Point3d p2)
        {
            return Math.Sqrt(Math.Pow(p2.x - p1.x, 2) +
                        Math.Pow(p2.y - p1.y, 2) +
                        Math.Pow(p2.z - p1.z, 2));
        }
        /*
        kernel double GetDistance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            return sqrt(pow(x2 - x1, 2) + pow(y2 - y1, 2) + pow(z2 - z1, 2));
        }
         */

        public static double GetTriangleHeight(double heightSide, double side2, double side3)
        {
            return 2 * GetTriangleArea(heightSide, side2, side3) / heightSide;
        }

        public static double GetTriangleArea(double side1, double side2, double side3)
        {
            double p = (side1 + side2 + side3) / 2;
            return Math.Sqrt(p * (p - side1) * (p - side2) * (p - side3));
        }

        public static bool EqualsApprox(double a, double b, double approximationRange)
        {
            return Math.Abs(a - b) <= approximationRange;
        }
    }
}
