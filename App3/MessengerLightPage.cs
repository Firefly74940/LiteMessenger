using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using App3.Data;

namespace App3
{
    public class MessengerLightPage : Page
    {
        protected virtual void OnInternetConnectivityChanged(bool newHasInternet)
        {

        }
        private void OnInternetConnectivityChangedCaller(bool newHasInternet)
        {
            Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                OnInternetConnectivityChanged(newHasInternet);
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            DataSource.NetworkStatusChanged -= OnInternetConnectivityChangedCaller;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DataSource.NetworkStatusChanged += OnInternetConnectivityChangedCaller;
        }

        bool HasInternetConectivity
        {
            get
            {
                return DataSource.HasInternet;
            }
        }

       public Visibility ShouldShowInternetConectivityRibon
        {
            get
            {
                return DataSource.HasInternet ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}
