namespace Hacked.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using Microsoft.Phone.Applications.Common;
    using Microsoft.Phone.Controls;
    using Microsoft.Xna.Framework.Media;

    using Nokia.Music.Types;

    using GestureEventArgs = System.Windows.Input.GestureEventArgs;

    public partial class MainPage : PhoneApplicationPage
    {
        private readonly ObservableCollection<string> debugOut = new ObservableCollection<string>();

        public MainPage()
        {
            this.InitializeComponent();

            this.DataContext = this.debugOut;

            this.debugOut.Add("test");
            this.ToDebugOut("test2");


            AccelerometerHelper.Instance.ReadingChanged += this.InstanceOnReadingChanged;
        }

        private Simple3DVector lastAccHelperValue = null;
        private Simple3DVector accHelperReference = null;

        private int accHelperCounter;

        private void InstanceOnReadingChanged(object sender, AccelerometerHelperReadingEventArgs e)
        {
            //this.ToDebugOut(e.LowPassFilteredAcceleration.ToString());

            if (accHelperCounter++ == 0)
            {
                accHelperReference = e.LowPassFilteredAcceleration;
            }
            else if (accHelperCounter % 10 == 0)
            {
                if (Math.Abs(accHelperReference.X - e.LowPassFilteredAcceleration.X) > 0.1
                    || Math.Abs(accHelperReference.Y - e.LowPassFilteredAcceleration.Y) > 0.1
                    || Math.Abs(accHelperReference.Z - e.LowPassFilteredAcceleration.Z) > 0.1)
                {
                    this.ToDebugOut("faster");
                }
                else
                {
                    this.ToDebugOut("slower");
                }

                // reset reference position?
                accHelperReference = e.LowPassFilteredAcceleration;
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

        private void PlayLightShow(Uri artwork)
        {
            // TODO: get image
            // TODO: get pixels from image
            // TODO: loop through pixels passing them to the HUE


            // TODO: Update speed of passing to HUE based on accelerometer
        }

        private void DoItTapped(object sender, GestureEventArgs e)
        {
            var activeSong = MediaPlayer.Queue.ActiveSong;



            string myAppId = "JVjj4FSqPAAx9utAouQg";

            var client = new Nokia.Music.MusicClient(myAppId);

            ToDebugOut(activeSong.Artist.Name);

            client.SearchArtists(artists =>
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

                                        if (album.Thumb50Uri != null)
                                        {
                                            this.SetArtwork(album.Thumb50Uri);
                                        }
                                        else if (album.Thumb100Uri != null)
                                        {
                                            this.SetArtwork(album.Thumb100Uri);
                                        }
                                        else if (album.Thumb200Uri != null)
                                        {
                                            this.SetArtwork(album.Thumb100Uri);
                                        }
                                        else if (album.Thumb320Uri != null)
                                        {
                                            this.SetArtwork(album.Thumb100Uri);
                                        }
                                        else if (artist.Thumb50Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb50Uri);
                                        }
                                        else if (artist.Thumb100Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb100Uri);
                                        }
                                        else if (artist.Thumb200Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb100Uri);
                                        }
                                        else if (artist.Thumb320Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb100Uri);
                                        }
                                        else
                                        {
                                            this.ToDebugOut("no artwork");
                                        }
                                    }
                                    else
                                    {
                                        if (artist.Thumb50Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb50Uri);
                                        }
                                        else if (artist.Thumb100Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb100Uri);
                                        }
                                        else if (artist.Thumb200Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb100Uri);
                                        }
                                        else if (artist.Thumb320Uri != null)
                                        {
                                            this.SetArtwork(artist.Thumb100Uri);
                                        }
                                        else
                                        {
                                            this.ToDebugOut("no artwork");
                                        }
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

            ////var curSong = new CurrentSong(activeSong);

            ////ToDebugOut(curSong.Album);
            ////ToDebugOut(curSong.Artist);
            ////ToDebugOut(curSong.Genre);
            ////ToDebugOut(curSong.Title);

            ////ToDebugOut(MediaPlayer.PlayPosition + " of " + activeSong.Duration);

            ////try
            ////{
            ////    if (curSong.TryAndGetArtwork().Result)
            ////    {
            ////        this.PlayLightShow(curSong.Artwork);
            ////    }
            ////    else
            ////    {
            ////        // TODO: use fallback
            ////    }
            ////}
            ////catch (Exception exc)
            ////{
            ////    // TODO: use fallback
            ////}
        }

        private void SetArtwork(Uri imageUri)
        {
            this.ToDebugOut(imageUri.ToString());

            Dispatcher.BeginInvoke(() => { this.ArtworkImage.Source = new BitmapImage(imageUri); });
        }

        private async void ArtworkOpened(object sender, RoutedEventArgs e)
        {
            var bitmap = new WriteableBitmap(this.ArtworkImage, null);

            var interestingPixels = new List<Color>();

            var lastPixel = Color.FromArgb(255, 0, 0, 0);

            for (int w = 5; w < bitmap.PixelWidth- 5; w += 5)
            {
                for (int h = 5; h < bitmap.PixelHeight - 5; h += 5)
                {
                    var nextPixel = bitmap.GetPixel(w, h);

                    if (nextPixel != lastPixel)
                    {
                        interestingPixels.Add(nextPixel);
                        lastPixel = nextPixel;
                    }
                }
            }

            foreach (var interestingPixel in interestingPixels)
            {
                this.FakeBulb.Fill = new SolidColorBrush(interestingPixel);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private void ArtworkFailed(object sender, ExceptionRoutedEventArgs e)
        {
            this.ToDebugOut("loading artwork failed");
        }


        private void TempoStarted(object sender, ManipulationStartedEventArgs e)
        {
            this.ToDebugOut("TempoStarted");

            accHelperCounter = 0;
            AccelerometerHelper.Instance.Active = true;
        }

        private void TempoCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.ToDebugOut("TempoCompleted");
            AccelerometerHelper.Instance.Active = false;
        }
    }


    ////public class CurrentSong
    ////{
    ////    private Uri artwork;

    ////    public CurrentSong(Song song)
    ////    {
    ////        // note. Album may have artwork we could use as a fallback
    ////        this.Album = song.Album.Name;
    ////        this.Artist = song.Artist.Name;
    ////        this.Genre = song.Genre.Name;
    ////        this.Title = song.Name;
    ////    }

    ////    public string Album { get; set; }

    ////    public string Artist { get; set; }

    ////    public string Genre { get; set; }

    ////    public string Title { get; set; }

    ////    public Uri Artwork
    ////    {
    ////        get
    ////        {
    ////            return this.artwork;
    ////        }
    ////        set
    ////        {
    ////            this.artwork = value;
    ////        }
    ////    }

    ////    public async Task<bool> TryAndGetArtwork()
    ////    {
    ////        string myAppId = "JVjj4FSqPAAx9utAouQg";

    ////        var client = new Nokia.Music.MusicClient(myAppId);


    ////        client.SearchArtists(artists =>
    ////            {
    ////                var result = artists.FirstOrDefault();

    ////                if (result != null)
    ////                {
    ////                    if (result.Thumb50Uri != null)
    ////                    {
    ////                        this.Artwork = result.Thumb50Uri;
    ////                    }

    ////                    if (result.Thumb100Uri != null)
    ////                    {
    ////                        this.Artwork = result.Thumb100Uri;
    ////                    }

    ////                    if (result.Thumb200Uri != null)
    ////                    {
    ////                        this.Artwork = result.Thumb100Uri;
    ////                    }

    ////                    if (result.Thumb320Uri != null)
    ////                    {
    ////                        this.Artwork = result.Thumb100Uri;
    ////                    }
    ////                }
    ////            },
    ////            this.Artist);

    ////        return false;
    ////    }
    ////}
}