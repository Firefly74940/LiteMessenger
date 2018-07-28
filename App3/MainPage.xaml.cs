using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using App3.Data;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App3
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>

    public sealed partial class MainPage : MessengerLightPage
    {
        private bool _skipFrist;
        private NavigationEventArgs eventToForward;
        public MainPage()
        {

            this.InitializeComponent();
            ShowStatusBar();


            LoadPage();
            NoInternetRibon.Visibility = ShouldShowInternetConectivityRibon;
        }

        private void LoadPage()
        {
            HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get,
                    new Uri("https://mbasic.facebook.com/messages/"));

            _skipFrist = true;
            HttpRequestMessage.Headers.Add("User-Agent", DataSource.CustomUserAgent);
            LoginView.NavigateWithHttpRequestMessage(HttpRequestMessage);
        }

        protected override void OnInternetConnectivityChanged(bool newHasInternet)
        {
            base.OnInternetConnectivityChanged(newHasInternet);
            NoInternetRibon.Visibility = ShouldShowInternetConectivityRibon;
            if (newHasInternet)
            {
                LoadPage();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            eventToForward = e;
            Frame.BackStack.Clear();
        }

        private async void ShowStatusBar()
        {
            // turn on SystemTray for mobile
            // don't forget to add a Reference to Windows Mobile Extensions For The UWP
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusbar.ShowAsync();

                statusbar.BackgroundColor = Windows.UI.Colors.Black;
                statusbar.BackgroundOpacity = 1;
                statusbar.ForegroundColor = Windows.UI.Colors.White;
            }
        }

       private void LoginView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.ToString().StartsWith("https://mbasic.facebook.com/messages/"))
            {
                if (!_skipFrist)
                {
                    args.Cancel = true;
                    DataSource.IsLogedIn = true;

                    Frame.Navigate(typeof(ChatList), eventToForward);

                }
                _skipFrist = false;
            }
        }

    }
}
