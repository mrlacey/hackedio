namespace Hacked.HueColors
{
    /// <summary>
    /// Internal helper class, holds XY
    /// Originally from https://github.com/Q42/Q42.HueApi
    /// </summary>
    internal class CGPoint
    {
        public double X { get; set; }

        public double Y { get; set; }

        public CGPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}