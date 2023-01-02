using System.Drawing;

namespace MyMiniEngine.Engine
{
    internal struct Rgb
    {
        public byte r, g, b;

        public Rgb(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static explicit operator Color(Rgb color)
        {
            return Color.FromArgb(color.r, color.g, color.b);
        }
    }
}
