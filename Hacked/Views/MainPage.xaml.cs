namespace Hacked.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO.IsolatedStorage;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Hacked.Extensions;
    using Hacked.HueColors;

    using Microsoft.Phone.Applications.Common;
    using Microsoft.Phone.Controls;
    using Microsoft.Xna.Framework.Media;

    using Nokia.Music.Types;

    using GestureEventArgs = System.Windows.Input.GestureEventArgs;

    public partial class MainPage : PhoneApplicationPage
    {
        private const string IpStoreKey = "IpStoreKey";

        private readonly object delayLock = new object();

        private readonly object colorsLock = new object();

        private readonly ObservableCollection<string> debugOut = new ObservableCollection<string>();

        private int bulbLoopCounter;

        private int colorLoopCounter;

        private Simple3DVector accHelperReference;

        private int accHelperCounter;

        private List<Color> colorsToDisplay;

        private int currentDelay = 3000;

        public MainPage()
        {
            this.InitializeComponent();

            this.DataContext = this.debugOut;

            AccelerometerHelper.Instance.ReadingChanged += this.AccelerometerReadingChanged;

            try
            {
                this.BridgeIp.Text = IsolatedStorageSettings.ApplicationSettings[IpStoreKey].ToString();
            }
            catch (KeyNotFoundException)
            {
                // If have never saved setting previously the above will fail
            }

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
                    () =>
                    {
                        try
                        {
                            RequestHelper.ExecuteRequest(
                                string.Format("http://{0}/api", this.BridgeIp.Text),
                                "{\"devicetype\":\"LightShow\",\"username\":\"mrltestlightshow\"}",
                                (exc, resp) =>
                                {
                                    this.ToDebugOut(exc != null ? exc.ToString() : resp);

                                    Dispatcher.BeginInvoke(
                                        async () =>
                                        {
                                            MessageBox.Show("press button");

                                            var lights = await new HttpClient().GetStringAsync("http://{0}/api/mrltestlightshow/lights", this.BridgeIp.Text);

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
            var cgp = HueColorConverter.XyFromColor(newColor.R, newColor.G, newColor.B);

            var brightness = this.GetBrightness(newColor);

            RequestHelper.ExecuteRequest(
                string.Format("http://{0}/api/mrltestlightshow/lights/{1}/state", this.BridgeIp.Text, bulbId),
                "{\"on\":true, \"xy\":[" + cgp.X + "," + cgp.Y + "], \"bri\":" + brightness + "}",
                (exc, resp) => this.ToDebugOut(exc != null ? exc.ToString() : resp));
        }

        private string GetBrightness(Color newColor)
        {
            return (((newColor.R + newColor.G + newColor.B) / 3) / 2).ToString();
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
                    this.currentDelay += 200;
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

        private async void LoadCurrentlyPlayingSong()
        {
            var activeSong = MediaPlayer.Queue.ActiveSong;

            if (activeSong == null)
            {
                this.ToDebugOut("no song detected");

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
                if (MediaPlayer.Queue.ActiveSong.Album.HasArt)
                {
                    this.ToDebugOut("artwork from zune");
                    Dispatcher.BeginInvoke(() =>
                        {
                            var streamSource = new BitmapImage();
                            streamSource.SetSource(MediaPlayer.Queue.ActiveSong.Album.GetThumbnail());

                            this.ArtworkImage.Source = streamSource;
                        });
                   
                }
                else
                {
                    this.ToDebugOut("no artwork");
                }
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

        private void ConnectTapped(object sender, GestureEventArgs e)
        {
            this.SetUpBridge();
        }

        private void BridgeIpLostFocus(object sender, RoutedEventArgs e)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;

            if (settings.Contains(IpStoreKey))
            {
                settings[IpStoreKey] = this.BridgeIp.Text;
            }
            else
            {
                settings.Add(IpStoreKey, this.BridgeIp.Text);
            }

            settings.Save();
        }

        private void ImageDoubleTapped(object sender, GestureEventArgs e)
        {
            this.LoadCurrentlyPlayingSong();
        }

        private void ListDoubleTapped(object sender, GestureEventArgs e)
        {
            this.debugOut.Clear();
        }
    }
}