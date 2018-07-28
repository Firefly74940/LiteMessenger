using System;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using App3.Data;
using App3.ViewModels;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
{


    public class MessageDataTemplateSelector : DataTemplateSelector
    {

        public DataTemplate InfoMessage { get; set; }

        public DataTemplate SelfMessage { get; set; }
        public DataTemplate SelfLinkMessage { get; set; }
        public DataTemplate SelfImgMessage { get; set; }
        public DataTemplate SelfFileMessage { get; set; }

        public DataTemplate OtherMessage { get; set; }
        public DataTemplate OtherLinkMessage { get; set; }
        public DataTemplate OtherImgMessage { get; set; }
        public DataTemplate OtherFileMessage { get; set; }

        //public DataTemplate ReceivedTemplate
        //{
        //    get;
        //    set;
        //}
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (!(item is ChatMessageViewModel message)) return null;
            bool fromSelf = message.MessageSource == MessageSources.Self;
            switch (message.MessageType)
            {
                case MessageTypes.Text:
                    return fromSelf ? SelfMessage : OtherMessage;

                case MessageTypes.Info:
                    return InfoMessage;

                case MessageTypes.Img:
                    return fromSelf ? SelfImgMessage : OtherImgMessage;

                case MessageTypes.Link:
                    return fromSelf ? SelfLinkMessage : OtherLinkMessage;
                case MessageTypes.File:
                    return fromSelf ? SelfFileMessage : OtherFileMessage;
            }

            return InfoMessage;
        }
    }

    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatPage : MessengerLightPage
    {
        DispatcherTimer dispatcherTimer;
        public ChatPage()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            this.InitializeComponent();
            NoInternetRibon.Visibility = ShouldShowInternetConectivityRibon;

        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            if (ShouldRefreshOldMessages())
            {
                _currentChat.RefreshOlderMessages();
            }
            else
            {
                _currentChat.RefreshConversation();
            }
        }

        private ChatViewModel _currentChat;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ChatViewModel chat)
            {
                _currentChat = chat;
                ChatName.Text = _currentChat.Name;
                listView.ItemsSource = _currentChat.Messages;

                _currentChat.RefreshConversation();
                this.Resources["CurrentChat.IsGroup"] = _currentChat.IsGroup ? Visibility.Visible : Visibility.Collapsed;
            }
            dispatcherTimer.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            dispatcherTimer.Stop();
        }

        private int _refreshOldMessagesTwiceInARow = 2;
        bool ShouldRefreshOldMessages()
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var firstItem = listView.ContainerFromIndex(0) as ListViewItem;
            Rect screenBounds = new Rect(0, 0, bounds.Width, bounds.Height);

            var firstitemVisible = (VisualTreeHelper.FindElementsInHostCoordinates(screenBounds, listView).Contains(firstItem));
            bool shouldRefresh = _refreshOldMessagesTwiceInARow > 0 || firstitemVisible;

            if (firstitemVisible && _refreshOldMessagesTwiceInARow < 6)
                _refreshOldMessagesTwiceInARow += 3;

            if (_refreshOldMessagesTwiceInARow > 0)
                _refreshOldMessagesTwiceInARow--;
            return shouldRefresh;
            //    Debug.WriteLine("Element is now visible");
            //else
            //    Debug.WriteLine("Element is no longer visible");
        }
        private async void Send_Click(object sender, RoutedEventArgs e)
        {

            _currentChat.AddMessage(new ChatMessage() { Message = NewMessageBox.Text, UserID = DataSource.Username, DisplayName = DataSource.Username });


            _currentChat.SendMessage(NewMessageBox.Text);

            NewMessageBox.Text = "";

        }

        private void NewMessageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Send.IsEnabled = !string.IsNullOrEmpty(NewMessageBox.Text);
            NewMessageBox.Height = NewMessageBox.LineCount()>1 ? 60:35 ;

        }

        private void NewMessageBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            
            if (e.Key == VirtualKey.Enter)
            {
                bool ctrldown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) || AnalyticsInfo.VersionInfo.DeviceFamily== "Windows.Mobile";

                if (!string.IsNullOrEmpty(NewMessageBox.Text) && !ctrldown)
                {
                    Send_Click(null, null);
                }
            }

        }

        private void BarButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            _currentChat.RefreshConversation();
        }

        private void BarButtonGetOlder_Click(object sender, RoutedEventArgs e)
        {
            _currentChat.RefreshOlderMessages();
        }

        protected override void OnInternetConnectivityChanged(bool newHasInternet)
        {
            base.OnInternetConnectivityChanged(newHasInternet);
            NoInternetRibon.Visibility = ShouldShowInternetConectivityRibon;
        }
    }


}
