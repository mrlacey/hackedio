namespace Hacked.HueColors
{
    /// <summary>
    /// Internal helper class, holds XY
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