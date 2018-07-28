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

        public override string ToString() => This.ToString();
    }
}
