using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using App3.Data;
using HtmlAgilityPack;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
{
    

    public class MessageDataTemplateSelector : DataTemplateSelector
    {

        public DataTemplate InfoMessage { get; set; }
        public DataTemplate SelfMessage { get; set; }

        public DataTemplate SelfLinkMessage { get; set; }
        public DataTemplate SelfImgMessage { get; set; }
        public DataTemplate OtherMessage { get; set; }
        public DataTemplate OtherLinkMessage { get; set; }

        public DataTemplate OtherImgMessage { get; set; }

        //public DataTemplate ReceivedTemplate
        //{
        //    get;
        //    set;
        //}
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var message = item as ChatMessage;
            if (message == null) return null;
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
            }

            return InfoMessage;
        }
    }

    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatPage : Page
    {
        public ChatPage()
        {
            this.InitializeComponent();
        }

        private ChatHeader _currentChatHeader;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var header = e.Parameter as ChatHeader;
            if (header != null)
            {
                _currentChatHeader = header;
                _currentChatHeader.RefreshConversation();
                ChatName.Text = header.Name;
                listView.ItemsSource = header.Messages;
            }
        }


  

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            //  var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            //var a =  listView.ContainerFromIndex(0) as ListViewItem;
            //  Rect screenBounds = new Rect(0, 0,bounds.Width, bounds.Height);

            //  if (VisualTreeHelper.FindElementsInHostCoordinates(screenBounds, listView).Contains(a))
            //      Debug.WriteLine("Element is now visible");
            //  else
            //      Debug.WriteLine("Element is no longer visible");

            //  return;
            _currentChatHeader.Messages.Add(new ChatMessage() { Message = NewMessageBox.Text, UserID = DataSource.Username, DisplayName = DataSource.Username });


           _currentChatHeader.SendMessage(NewMessageBox.Text,this);

            NewMessageBox.Text = "";

        }

        private void NewMessageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Send.IsEnabled = !string.IsNullOrEmpty(NewMessageBox.Text);
        }

        private void NewMessageBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (!string.IsNullOrEmpty(NewMessageBox.Text))
                {
                    Send_Click(null, null);
                }
            }

        }
    }


}
