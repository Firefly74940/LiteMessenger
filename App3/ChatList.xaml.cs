using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using App3.Data;
using App3.ViewModels;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatList : Page
    {
        public ChatList()
        {
            this.InitializeComponent();
            listView.ItemsSource = AccountViewModel.Instance.Chats;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame.BackStack.Clear();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            AccountViewModel.Instance.RefreshChatList();

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
           // DataSource.Names.Add(new ChatHeader(){ Order = 4,Name = "2222222"});
            DataSource.SyncContacts();
        }
    }
}
