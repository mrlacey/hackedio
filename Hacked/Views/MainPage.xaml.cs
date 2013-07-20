namespace Hacked.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;

    using Hacked.Extensions;

    using Microsoft.Phone.Applications.Common;
    using Microsoft.Phone.Controls;
    using Microsoft.Xna.Framework.Media;

    using Nokia.Music.Types;

    using Q42.HueApi;
    using Q42.HueApi.Interfaces;

    using GestureEventArgs = System.Windows.Input.GestureEventArgs;

    public partial class MainPage : PhoneApplicationPage
    {
        private readonly object delayLock = new object();

        private readonly object colorsLock = new object();

        private readonly ObservableCollection<string> debugOut = new ObservableCollection<string>();

        private int bulbLoopCounter;

        private int colorLoopCounter;

        private Simple3DVector accHelperReference;

        private int accHelperCounter;

        private List<Color> colorsToDisplay;

        private int currentDelay = 3000;

        private HueClient hubClient;

        public MainPage()
        {
            this.InitializeComponent();

            this.DataContext = this.debugOut;

            AccelerometerHelper.Instance.ReadingChanged += this.AccelerometerReadingChanged;

            //this.SetUpBridge();

            this.SetUpColorTimer();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.LoadCurrentlyPlayingSong();
        }

        private void SetUpBridge()
        {
            ThreadPool.QueueUserWorkItem(
                state => this.Dispatcher.BeginInvoke(
                    async () =>
                    {
                        ////IBridgeLocator locator = new HttpBridgeLocator();

                        //////For Windows 8 and .NET45 projects you can use the SSDPBridgeLocator which actually scans your network. 
                        //////See the included BridgeDiscoveryTests and the specific .NET and .WinRT projects
                        ////IEnumerable<string> bridgeIPs = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));

                        ////foreach (var bridgeIP in bridgeIPs)
                        ////{
                        ////    this.ToDebugOut("found bridge: " + bridgeIP);
                        ////}

                        try
                        {

                            //hubClient = new HueClient("ip");
                            //hubClient.RegisterAsync("mypersonalappname", "mypersonalappkey");
                            //this.hubClient = new HueClient(this.BridgeIp.Text);
                            //await this.hubClient.RegisterAsync("mrlacey-lightshow", "07970925252");

                            //client.Initialize("mypersonalappkey");

                            RequestHelper.ExecuteRequest(
                                "api",
                                "{\"devicetype\":\"test user\",\"username\":\"mrltestlightshow\"}",
                                (exc, resp) =>
                                {
                                    this.ToDebugOut(exc != null ? exc.ToString() : "no error");
                                    this.ToDebugOut("resp: " + resp);

                                    Dispatcher.BeginInvoke(
                                        async () =>
                                        {
                                            MessageBox.Show("press button");

                                            var lights = await new HttpClient().GetStringAsync("http://192.168.2.203/api/mrltestlightshow/lights");

                                            this.ToDebugOut("lights: " + lights);
                                        });
                                });



                        }
                        catch (Exception exc)
                        {
                            this.ToDebugOut(exc.Message);
                        }
                    }));
        }

        private void SetUpColorTimer()
        {
            ThreadPool.QueueUserWorkItem(
                async state =>
                {
                    while (true)
                    {
                        var getColor = this.GetNextColor();

                        if (getColor != null)
                        {
                            this.SetBulbColor(this.bulbLoopCounter++, getColor.Value);
                        }

                        await Task.Delay(this.GetCurrentDelay());
                    }
                });
        }

        private Color? GetNextColor()
        {
            Color? result = null;

            lock (this.colorsLock)
            {
                if (this.colorsToDisplay != null)
                {
                    result = this.colorsToDisplay[this.colorLoopCounter++ % this.colorsToDisplay.Count];
                }
            }

            return result;
        }

        private void SetBulbColor(long bulb, Color newColor)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var command = new LightCommand { On = true };
                command.TurnOn().SetColor(newColor.ToHexString());

                switch (bulb % 3)
                {
                    case 0:
                        this.FakeBulb1.Fill = new SolidColorBrush(newColor);
                        this.SendColorToHueBulb(newColor, 1);
                        break;
                    case 1:
                        this.FakeBulb2.Fill = new SolidColorBrush(newColor);
                        this.SendColorToHueBulb(newColor, 2);
                        break;
                    case 2:
                        this.FakeBulb3.Fill = new SolidColorBrush(newColor);
                        this.SendColorToHueBulb(newColor, 3);
                        break;
                }
            });
        }

        private void SendColorToHueBulb(Color newColor, int bulbId)
        {
            var hlscolor = HslColor.FromColor(newColor);

            var cgp = HueColorConverter.XyFromColor(newColor.R, newColor.G, newColor.B);

            RequestHelper.ExecuteRequest(
                string.Format("api/mrltestlightshow/lights/{0}/state", bulbId),
                "{\"on\":true, \"xy\":[" + cgp.x + "," + cgp.y + "]}",
                (exc, resp) =>
                {
                    this.ToDebugOut(exc != null ? exc.ToString() : "no error");
                    this.ToDebugOut("resp: " + resp);
                });
        }

        private void SendToHueBulb(LightCommand command, int bulbId)
        {
            if (this.hubClient != null)
            {
                this.hubClient.SendCommandAsync(command, new List<string> { bulbId.ToString() });
            }
        }

        private int GetCurrentDelay()
        {
            lock (this.delayLock)
            {
                return this.currentDelay;
            }
        }

        private void IncreaseCurrentDelay()
        {
            lock (this.delayLock)
            {
                if (this.currentDelay < 5000)
                {
                    this.currentDelay += 100;
                    this.ToDebugOut("now slower");
                }
            }
        }

        private void DecreaseCurrentDelay()
        {
            lock (this.delayLock)
            {
                if (this.currentDelay > 300)
                {
                    this.currentDelay -= 100;
                    this.ToDebugOut("now faster");
                }
            }
        }

        private void AccelerometerReadingChanged(object sender, AccelerometerHelperReadingEventArgs e)
        {
            if (this.accHelperCounter++ == 0)
            {
                this.accHelperReference = e.LowPassFilteredAcceleration;
            }
            else if (this.accHelperCounter % 5 == 0)
            {
                if (Math.Abs(this.accHelperReference.X - e.LowPassFilteredAcceleration.X) > 0.1
                    || Math.Abs(this.accHelperReference.Y - e.LowPassFilteredAcceleration.Y) > 0.1
                    || Math.Abs(this.accHelperReference.Z - e.LowPassFilteredAcceleration.Z) > 0.1)
                {
                    this.DecreaseCurrentDelay();
                }
                else
                {
                    this.IncreaseCurrentDelay();
                }

                // reset reference position
                this.accHelperReference = e.LowPassFilteredAcceleration;
            }
        }

        private void ToDebugOut(string messasge)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.debugOut.Add(messasge);
                Debug.WriteLine(messasge);
            });
        }

        private void DoItTapped(object sender, GestureEventArgs e)
        {
            this.SetUpBridge();

        }

        private async void LoadCurrentlyPlayingSong()
        {
            var activeSong = MediaPlayer.Queue.ActiveSong;

            if (activeSong == null)
            {
                this.ToDebugOut("no song playing");

                await Task.Delay(10.Seconds());
                this.LoadCurrentlyPlayingSong();

                return;
            }
            else
            {
                ThreadPool.QueueUserWorkItem(async obj =>
                    {
                        var timeUntilSongFinishes = activeSong.Duration - MediaPlayer.PlayPosition;

                        await Task.Delay(timeUntilSongFinishes.Add(5.Seconds()));

                        this.LoadCurrentlyPlayingSong();
                    });
            }

            const string MyAppId = "JVjj4FSqPAAx9utAouQg";

            var client = new Nokia.Music.MusicClient(MyAppId);

            this.ToDebugOut(activeSong.Artist.Name);

            client.SearchArtists(
                artists =>
                {
                    if (artists != null)
                    {
                        var artist = artists.FirstOrDefault();

                        if (artist != null)
                        {
                            client.GetArtistProducts(
                                albums =>
                                {
                                    var album = albums.FirstOrDefault(a => a.Name == activeSong.Album.Name);

                                    if (album != null)
                                    {
                                        this.ToDebugOut(album.Name);

                                        this.SetFirstOf(
                                            album.Thumb50Uri,
                                            album.Thumb100Uri,
                                            album.Thumb200Uri,
                                            album.Thumb320Uri,
                                            artist.Thumb50Uri,
                                            artist.Thumb100Uri,
                                            artist.Thumb200Uri,
                                            artist.Thumb320Uri);
                                    }
                                    else
                                    {
                                        this.SetFirstOf(
                                            artist.Thumb50Uri,
                                            artist.Thumb100Uri,
                                            artist.Thumb200Uri,
                                            artist.Thumb320Uri);
                                    }
                                },
                                artist,
                                Category.Album);
                        }
                    }
                    else
                    {
                        this.ToDebugOut("artist not found");
                    }
                },
                activeSong.Artist.Name);
        }

        private void SetFirstOf(params Uri[] uri)
        {
            var toSet = uri.FirstOrDefault(u => u != null);

            if (toSet != null)
            {
                this.SetArtwork(toSet);
            }
            else
            {
                this.ToDebugOut("no artwork");
            }
        }

        private void SetArtwork(Uri imageUri)
        {
            this.ToDebugOut(imageUri.ToString());

            Dispatcher.BeginInvoke(() => { this.ArtworkImage.Source = new BitmapImage(imageUri); });
        }

        private void ArtworkOpened(object sender, RoutedEventArgs e)
        {
            var bitmap = new WriteableBitmap(this.ArtworkImage, null);

            var interestingPixels = new List<Color>();

            var lastPixel = Color.FromArgb(255, 0, 0, 0);

            for (int w = 5; w < bitmap.PixelWidth - 5; w += 5)
            {
                for (int h = 5; h < bitmap.PixelHeight - 5; h += 5)
                {
                    var nextPixel = bitmap.GetPixel(w, h);

                    if (this.AreColorsDifferentEnough(nextPixel, lastPixel))
                    {
                        interestingPixels.Add(nextPixel);
                        lastPixel = nextPixel;
                    }
                }
            }

            this.SetColors(interestingPixels);
        }

        private bool AreColorsDifferentEnough(Color nextPixel, Color lastPixel)
        {
            const int DifferenceThreshold = 40;

            return (Math.Abs(nextPixel.R - lastPixel.R) > DifferenceThreshold)
                || (Math.Abs(nextPixel.G - lastPixel.G) > DifferenceThreshold)
                || (Math.Abs(nextPixel.B - lastPixel.B) > DifferenceThreshold);
        }

        private void SetColors(List<Color> newColors)
        {
            lock (this.colorsLock)
            {
                this.colorsToDisplay = newColors;
            }
        }

        private void ArtworkFailed(object sender, ExceptionRoutedEventArgs e)
        {
            this.ToDebugOut("loading artwork failed");
        }

        private void TempoStarted(object sender, ManipulationStartedEventArgs e)
        {
            this.ToDebugOut("TempoStarted");

            this.accHelperCounter = 0;
            AccelerometerHelper.Instance.Active = true;
        }

        private void TempoCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.ToDebugOut("TempoCompleted");
            AccelerometerHelper.Instance.Active = false;
        }
    }

    /// <summary>
    /// Color as Hue, Saturation and Luminosity rather than RGB values
    /// </summary>
    struct HslColor
    {
        /// <summary>
        /// The alpha value
        ///  from 0 to 1
        /// </summary>
        private double alpha;

        /// <summary>
        /// The hue value
        ///  from 0 to 360
        /// </summary>
        private double hue;

        /// <summary>
        /// The saturation value
        ///  from 0 to 1
        /// </summary>
        private double saturation;

        /// <summary>
        /// The luminosity value
        ///  from 0 to 1
        /// </summary>
        private double luminosity;

        /// <summary>
        /// Create the HSL representation of the color.
        /// </summary>
        /// <param name="color">The color to convert from.</param>
        /// <returns>The HSLColor</returns>
        public static HslColor FromColor(Color color)
        {
            var hslc = new HslColor();
            hslc.alpha = color.A;

            double red = ByteToPercent(color.R);
            double green = ByteToPercent(color.G);
            double blue = ByteToPercent(color.B);

            double max = Math.Max(blue, Math.Max(red, green));
            double min = Math.Min(blue, Math.Min(red, green));

            if (max == min)
            {
                hslc.hue = 0;
            }
            else if (max == red && green >= blue)
            {
                hslc.hue = 60 * ((green - blue) / (max - min));
            }
            else if (max == red && green < blue)
            {
                hslc.hue = (60 * ((green - blue) / (max - min))) + 360;
            }
            else if (max == green)
            {
                hslc.hue = (60 * ((blue - red) / (max - min))) + 120;
            }
            else if (max == blue)
            {
                hslc.hue = (60 * ((red - green) / (max - min))) + 240;
            }

            hslc.luminosity = .5 * (max + min);

            if (max == min)
            {
                hslc.saturation = 0;
            }
            else if (hslc.luminosity <= .5)
            {
                hslc.saturation = (max - min) / (2 * hslc.luminosity);
            }
            else if (hslc.luminosity > .5)
            {
                hslc.saturation = (max - min) / (2 - (2 * hslc.luminosity));
            }

            return hslc;
        }

        public int Saturation
        {
            get
            {
                return Convert.ToInt32(this.saturation * 255);
            }
        }

        /// <summary>
        /// Convert the color to it's compliment (opposite position on the color wheel).
        /// </summary>
        public void ConvertToCompliment()
        {
            this.hue += 180;

            if (this.hue > 360)
            {
                this.hue -= 360;
            }
        }

        /// <summary>
        /// Convert the H, L and S values back to RGB as a System.Color.
        /// </summary>
        /// <returns>The H, S and L values converted back to RGB</returns>
        public Color ToColor()
        {
            double q = 0;

            if (this.luminosity < .5)
            {
                q = this.luminosity * (1 + this.saturation);
            }
            else
            {
                q = this.luminosity + this.saturation - (this.luminosity * this.saturation);
            }

            double p = (2 * this.luminosity) - q;
            double hk = this.hue / 360;
            double r = GetComponent(Normalize(hk + (1.0 / 3.0)), p, q);
            double g = GetComponent(Normalize(hk), p, q);
            double b = GetComponent(Normalize(hk - (1.0 / 3.0)), p, q);

            return Color.FromArgb(PercentToByte(this.alpha), PercentToByte(r), PercentToByte(g), PercentToByte(b));
        }

        /// <summary>
        /// Convert byte to percentage.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The byte as a percentage (between 0 and 1)</returns>
        private static double ByteToPercent(byte value)
        {
            double d = value;
            d /= 255;
            return d;
        }

        /// <summary>
        /// Convert percent to byte.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The percentage (0 to 1) value to a byte</returns>
        private static byte PercentToByte(double value)
        {
            value *= 255;
            value += .5;

            if (value > 255)
            {
                value = 255;
            }
            else if (value < 0)
            {
                value = 0;
            }

            return (byte)value;
        }

        /// <summary>
        /// Normalizes the specified value between 0 and 1.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        /// <returns>The normalized value</returns>
        private static double Normalize(double value)
        {
            if (value < 0)
            {
                value += 1;
            }
            else if (value > 1)
            {
                value -= 1;
            }

            return value;
        }

        /// <summary>
        /// Gets the component.
        /// </summary>
        /// <param name="tc">The t c.</param>
        /// <param name="p">The p.</param>
        /// <param name="q">The q.</param>
        /// <returns>The component value</returns>
        private static double GetComponent(double tc, double p, double q)
        {
            if (tc < (1.0 / 6.0))
            {
                return p + ((q - p) * 6 * tc);
            }
            else if (tc < .5)
            {
                return q;
            }
            else if (tc < (2.0 / 3.0))
            {
                return p + ((q - p) * 6 * ((2.0 / 3.0) - tc));
            }

            return p;
        }
    }

    /// <summary>
    /// Internal helper class, holds XY
    /// </summary>
    internal struct CGPoint
    {
        public double x;
        public double y;

        public CGPoint(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// Used to convert colors between XY and RGB
    /// internal: Do not expose
    /// </summary>
    internal static partial class HueColorConverter
    {
        private static CGPoint Red = new CGPoint(0.675F, 0.322F);
        private static CGPoint Lime = new CGPoint(0.4091F, 0.518F);
        private static CGPoint Blue = new CGPoint(0.167F, 0.04F);
        private static float factor = 10000.0f;
        private static int maxX = 452;
        private static int maxY = 302;

        /// <summary>
        /// Get XY from red,green,blue strings / ints
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        public static CGPoint XyFromColor(string red, string green, string blue)
        {
            return XyFromColor(int.Parse(red), int.Parse(green), int.Parse(blue));
        }

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
                cx = closestPoint.x;
                cy = closestPoint.y;
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
            CGPoint v1 = new CGPoint(Lime.x - Red.x, Lime.y - Red.y);
            CGPoint v2 = new CGPoint(Blue.x - Red.x, Blue.y - Red.y);

            CGPoint q = new CGPoint(p.x - Red.x, p.y - Red.y);

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
            return (p1.x * p2.y - p1.y * p2.x);
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
            CGPoint AP = new CGPoint(P.x - A.x, P.y - A.y);
            CGPoint AB = new CGPoint(B.x - A.x, B.y - A.y);
            double ab2 = AB.x * AB.x + AB.y * AB.y;
            double ap_ab = AP.x * AB.x + AP.y * AB.y;

            double t = ap_ab / ab2;

            if (t < 0.0f)
                t = 0.0f;
            else if (t > 1.0f)
                t = 1.0f;

            CGPoint newPoint = new CGPoint(A.x + AB.x * t, A.y + AB.y * t);
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
            double dx = one.x - two.x; // horizontal difference
            double dy = one.y - two.y; // vertical difference
            double dist = Math.Sqrt(dx * dx + dy * dy);

            return dist;
        }

        /// <summary>
        /// Returns hexvalue from Light State
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static string HexFromState(State state)
        {
            if (state == null)
                throw new ArgumentNullException("state");
            if (state.On == false || state.Brightness <= 5)
                return "000000";
            return HexFromXy(state.ColorCoordinates[0], state.ColorCoordinates[1]);
        }

        /// <summary>
        /// Get the HEX color from an XY value
        /// </summary>
        /// <param name="xNumber"></param>
        /// <param name="yNumber"></param>
        /// <returns></returns>
        public static string HexFromXy(double xNumber, double yNumber)
        {
            if (xNumber == 0 && yNumber == 0)
            {
                return "ffffff";
            }

            int closestValue = Int32.MaxValue;
            int closestX = 0, closestY = 0;

            double fX = xNumber;
            double fY = yNumber;

            int intX = (int)(fX * factor);
            int intY = (int)(fY * factor);

            for (int y = 0; y < maxY; y++)
            {
                for (int x = 0; x < maxX; x++)
                {
                    int differenceForPixel = 0;
                    differenceForPixel += Math.Abs(xArray[x, y] - intX);
                    differenceForPixel += Math.Abs(yArray[x, y] - intY);

                    if (differenceForPixel < closestValue)
                    {
                        closestX = x;
                        closestY = y;
                        closestValue = differenceForPixel;
                    }
                }
            }

            int color = cArray[closestX, closestY];
            int red = (color >> 16) & 0xFF;
            int green = (color >> 8) & 0xFF;
            int blue = color & 0xFF;

            return string.Format("{0}{1}{2}", red.ToString("X2"), green.ToString("X2"), blue.ToString("X2"));
        }

    }
}