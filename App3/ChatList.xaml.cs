using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using App3.Data;
using App3.ViewModels;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatList : MessengerLightPage
    {
        DispatcherTimer dispatcherTimer;
        public ChatList()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0,0,Consts.DispatchTimerInterval);
            this.InitializeComponent();
            listView.ItemsSource = AccountViewModel.Instance.Chats;
            NoInternetRibon.Visibility = ShouldShowInternetConectivityRibon;
        }
        private void DispatcherTimer_Tick(object sender, object e)
        {
            if (!AccountViewModel.Instance.RefreshInProgress)
            {
                AccountViewModel.Instance.NextRefreshInXMs -= Consts.DispatchTimerInterval;
                if (AccountViewModel.Instance.NextRefreshInXMs <= 0)
                {
                    AccountViewModel.Instance.RefreshChatList(DataSource.RequestType.Refresh);
                    AccountViewModel.Instance.NextRefreshInXMs = 3000;
                }
            }
        }

        protected override void OnInternetConnectivityChanged(bool newHasInternet)
        {
            base.OnInternetConnectivityChanged(newHasInternet);
            NoInternetRibon.Visibility = ShouldShowInternetConectivityRibon;
            if (newHasInternet)
            {
                AccountViewModel.Instance.RefreshChatList(DataSource.RequestType.Start);
            }
        }
        public override bool OnBackPressed()
        {
            if (WebPopUp.Visibility == Visibility.Visible)
            {
                WebPopUp.HidePopUp();
                return true;
            }

            return false;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Frame.BackStack.Clear();
            dispatcherTimer.Start();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            AccountViewModel.Instance.RefreshChatList(DataSource.RequestType.Start);

        }

        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ChatViewModel chat)
            {
                this.Frame.Navigate(typeof(ChatPage), chat);
            }

        }

        private void CommandBar_Opening(object sender, object e)
        {
            BarButtonDisconect.Visibility = Visibility.Visible;
            BarButtonSync.Visibility = Visibility.Visible;
        }

        private void CommandBar_Closing(object sender, object e)
        {
            BarButtonDisconect.Visibility = Visibility.Collapsed;
            BarButtonSync.Visibility = Visibility.Collapsed;
        }

        private void BarButtonDisconect_Click(object sender, RoutedEventArgs e)
        {
            DataSource.Disconect();
            Frame.Navigate(typeof(MainPage));
        }

        private void BarButtonSync_Click(object sender, RoutedEventArgs e)
        {
            DataSource.SyncContacts();
        }

        private void BarButtonAddConv_Click(object sender, RoutedEventArgs e)
        {
            WebPopUp.RequestPage(DataSource.requestUriString + DataSource.NewMessageLink);
        }
    }
}
