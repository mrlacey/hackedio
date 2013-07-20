namespace Hacked.HueColors
{
    using System;

    /// <summary>
    /// Used to convert colors between XY and RGB
    /// internal: Do not expose
    /// </summary>
    internal static partial class HueColorConverter
    {
        private static CGPoint Red = new CGPoint(0.675F, 0.322F);
        private static CGPoint Lime = new CGPoint(0.4091F, 0.518F);
        private static CGPoint Blue = new CGPoint(0.167F, 0.04F);

        /// <summary>
        ///  Get XY from red,green,blue ints
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        public static CGPoint XyFromColor(int red, int green, int blue)
        {
            double r = (red > 0.04045f) ? Math.Pow((red + 0.055f) / (1.0f + 0.055f), 2.4f) : (red / 12.92f);
            double g = (green > 0.04045f) ? Math.Pow((green + 0.055f) / (1.0f + 0.055f), 2.4f) : (green / 12.92f);
            double b = (blue > 0.04045f) ? Math.Pow((blue + 0.055f) / (1.0f + 0.055f), 2.4f) : (blue / 12.92f);

            double X = r * 0.4360747f + g * 0.3850649f + b * 0.0930804f;
            double Y = r * 0.2225045f + g * 0.7168786f + b * 0.0406169f;
            double Z = r * 0.0139322f + g * 0.0971045f + b * 0.7141733f;

            double cx = X / (X + Y + Z);
            double cy = Y / (X + Y + Z);

            if (Double.IsNaN(cx))
            {
                cx = 0.0f;
            }

            if (Double.IsNaN(cy))
            {
                cy = 0.0f;
            }

            //Check if the given XY value is within the colourreach of our lamps.
            CGPoint xyPoint = new CGPoint(cx, cy);
            bool inReachOfLamps = HueColorConverter.CheckPointInLampsReach(xyPoint);

            if (!inReachOfLamps)
            {
                //It seems the colour is out of reach
                //let's find the closes colour we can produce with our lamp and send this XY value out.

                //Find the closest point on each line in the triangle.
                CGPoint pAB = HueColorConverter.GetClosestPointToPoint(Red, Lime, xyPoint);
                CGPoint pAC = HueColorConverter.GetClosestPointToPoint(Blue, Red, xyPoint);
                CGPoint pBC = HueColorConverter.GetClosestPointToPoint(Lime, Blue, xyPoint);

                //Get the distances per point and see which point is closer to our Point.
                double dAB = HueColorConverter.GetDistanceBetweenTwoPoints(xyPoint, pAB);
                double dAC = HueColorConverter.GetDistanceBetweenTwoPoints(xyPoint, pAC);
                double dBC = HueColorConverter.GetDistanceBetweenTwoPoints(xyPoint, pBC);

                double lowest = dAB;
                CGPoint closestPoint = pAB;

                if (dAC < lowest)
                {
                    lowest = dAC;
                    closestPoint = pAC;
                }
                if (dBC < lowest)
                {
                    lowest = dBC;
                    closestPoint = pBC;
                }

                //Change the xy value to a value which is within the reach of the lamp.
                cx = closestPoint.X;
                cy = closestPoint.Y;
            }

            return new CGPoint(cx, cy);
        }

        /// <summary>
        ///  Method to see if the given XY value is within the reach of the lamps.
        /// </summary>
        /// <param name="p">p the point containing the X,Y value</param>
        /// <returns>true if within reach, false otherwise.</returns>
        private static bool CheckPointInLampsReach(CGPoint p)
        {
            CGPoint v1 = new CGPoint(Lime.X - Red.X, Lime.Y - Red.Y);
            CGPoint v2 = new CGPoint(Blue.X - Red.X, Blue.Y - Red.Y);

            CGPoint q = new CGPoint(p.X - Red.X, p.Y - Red.Y);

            double s = HueColorConverter.CrossProduct(q, v2) / HueColorConverter.CrossProduct(v1, v2);
            double t = HueColorConverter.CrossProduct(v1, q) / HueColorConverter.CrossProduct(v1, v2);

            if ((s >= 0.0f) && (t >= 0.0f) && (s + t <= 1.0f))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Calculates crossProduct of two 2D vectors / points.
        /// </summary>
        /// <param name="p1"> p1 first point used as vector</param>
        /// <param name="p2">p2 second point used as vector</param>
        /// <returns>crossProduct of vectors</returns>
        private static double CrossProduct(CGPoint p1, CGPoint p2)
        {
            return (p1.X * p2.Y - p1.Y * p2.X);
        }

        /// <summary>
        /// Find the closest point on a line.
        /// This point will be within reach of the lamp.
        /// </summary>
        /// <param name="A">A the point where the line starts</param>
        /// <param name="B">B the point where the line ends</param>
        /// <param name="P">P the point which is close to a line.</param>
        /// <returns> the point which is on the line.</returns>
        private static CGPoint GetClosestPointToPoint(CGPoint A, CGPoint B, CGPoint P)
        {
            CGPoint AP = new CGPoint(P.X - A.X, P.Y - A.Y);
            CGPoint AB = new CGPoint(B.X - A.X, B.Y - A.Y);
            double ab2 = AB.X * AB.X + AB.Y * AB.Y;
            double ap_ab = AP.X * AB.X + AP.Y * AB.Y;

            double t = ap_ab / ab2;

            if (t < 0.0f)
                t = 0.0f;
            else if (t > 1.0f)
                t = 1.0f;

            CGPoint newPoint = new CGPoint(A.X + AB.X * t, A.Y + AB.Y * t);
            return newPoint;
        }

        /// <summary>
        /// Find the distance between two points.
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns>the distance between point one and two</returns>
        private static double GetDistanceBetweenTwoPoints(CGPoint one, CGPoint two)
        {
            double dx = one.X - two.X; // horizontal difference
            double dy = one.Y - two.Y; // vertical difference
            double dist = Math.Sqrt(dx * dx + dy * dy);

            return dist;
        }

    }
}