using System;
using System.Collections.ObjectModel;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using App3.Data;

namespace App3.ViewModels
{
    class ChatViewModel : NotificationBase<ChatHeader>
    {
        public string Name => This.Name;


        public string NameForView
        {
            get
            {
                if (UnreadCount > 0)
                    return $"{Name} ({UnreadCount})";
                return Name;
            }
        }
        public string MessagePreview
        {
            get
            {
                int max = 65;
                return This.MessagePreview.Length > max ? (This.MessagePreview.Substring(0,max-3)+"...") : This.MessagePreview;
            }
        }

        public int UnreadCount => This.UnreadCount;

        public bool HasUnread => UnreadCount != 0; // -1 = unread but unknow number
        public int Order => This.Order;
        public bool IsGroup => This.IsGroup;

        public FontWeight TitleFontWeightsForView => HasUnread ? FontWeights.Bold : FontWeights.Normal;
        public SolidColorBrush MessagepreviewColorForView => HasUnread ? new SolidColorBrush(new UISettings().GetColorValue( UIColorType.Accent)) : Application.Current.Resources["SystemControlPageTextBaseMediumBrush"] as SolidColorBrush; 

        public override string ToString() => This.ToString();

        private readonly ObservableCollection<ChatMessageViewModel> _messages;

        public ObservableCollection<ChatMessageViewModel> Messages => _messages;

        public ChatViewModel(ChatHeader chat = null) : base(chat)
        {
            Func<ChatMessage, ChatMessageViewModel> viewModelCreator = model => new ChatMessageViewModel(model);

            _messages = new ObservableViewModelCollection<ChatMessageViewModel, ChatMessage>(chat.Messages, viewModelCreator);
           
        }


        public void SendMessage(string text)
        {
            This.SendMessage(text);
        }

        public void RefreshConversation()
        {
            This.RefreshConversation(ChatHeader.RequestType.GetNewMessages);
        }

        public void RefreshOlderMessages()
        {
            This.RefreshConversation(ChatHeader.RequestType.GetOldMessages);
        }

        public void AddMessage(ChatMessage chatMessage)
        {
            //This.Messages.Add(chatMessage);
        }
        protected override void OnBasePropertyChanged(object item, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Order")
            {
                RaisePropertyChanged("Order");
            }
            if (e.PropertyName == "UnreadCount")
            {
                RaisePropertyChanged("HasUnread");
                RaisePropertyChanged("UnreadCount");
                RaisePropertyChanged("TitleFontWeightsForView");
                RaisePropertyChanged("MessagepreviewColorForView");
                RaisePropertyChanged("NameForView");
            }
        }
    }
}
