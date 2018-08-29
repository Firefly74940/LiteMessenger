using System.ComponentModel;
using Windows.UI.Xaml;
using App3.Data;

namespace App3.ViewModels
{
    class ChatMessageViewModel : NotificationBase<ChatMessage>
    {
        public ChatMessageViewModel(ChatMessage message = null) : base(message)
        {

        }
        public MessageTypes MessageType => This.MessageType;

        public MessageSources MessageSource => This.MessageSource;

        public string DisplayName => This.DisplayName;

        public string UserID => This.UserID;

        public string Message => This.Message;

        public string MessageData => This.MessageData;
        public string MessageAditionalData => This.MessageAditionalData;

        public override string ToString() => This.ToString();

        public bool PreviousMessageHasSameSender=> This.PreviousMessageHasSameSender;

        public Thickness Margins
        {
            get
            {
                int margintop = PreviousMessageHasSameSender ? 2 : 6;
                int left = MessageSource == MessageSources.Self ? 150 : 0;
                int right = MessageSource == MessageSources.Self ? 0 : 150;

                return new Thickness(left,margintop,right, margintop);
            }
        }

        protected override void OnBasePropertyChanged(object item, PropertyChangedEventArgs e)
        {
            base.OnBasePropertyChanged(item, e);
            if (e.PropertyName == "PreviousMessageHasSameSender")
            {
                RaisePropertyChanged("Margins");
            }
            
        }
    }
}
