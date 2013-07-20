namespace Hacked
{
    using Hacked.Resources;

    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources actualLocalizedResources = new AppResources();

        public AppResources LocalizedResources
        {
            get
            {
                return actualLocalizedResources;
            }
        }
    }
}