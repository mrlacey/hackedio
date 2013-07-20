namespace Hacked.ViewModels
{
    using System.Runtime.CompilerServices;

    using GalaSoft.MvvmLight;

    using Hacked.Extensions;

    public class HackedBaseViewModel : ViewModelBase
    {
        private bool SetProperty<T>(ref T property, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (object.Equals(property, newValue))
            {
                return false;
            }

            property = newValue;

            this.RaisePropertyChanged(propertyName);

            return true;
        }

        private new void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (!propertyName.IsNullOrWhiteSpace())
            {
                base.RaisePropertyChanged(propertyName);
            }
        }
    }
}