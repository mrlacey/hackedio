namespace Hacked.Views
{
    using Microsoft.Phone.Controls;

    public partial class About : PhoneApplicationPage
    {
        private const string Version = "Version: {0}";

        public About()
        {
            this.InitializeComponent();

            this.DisplayedVersion.Text = this.ShortVersion();
        }

        private void VersionTapped(object sender, GestureEventArgs e)
        {
            this.DisplayedVersion.Text = this.DisplayedVersion.Text == this.ShortVersion() ? this.LongVersion() : this.ShortVersion();
        }

        private string ShortVersion()
        {
            return string.Format(Version, App.Version());
        }

        private string LongVersion()
        {
            return string.Format(Version, App.FullVersion());
        }
    }
}