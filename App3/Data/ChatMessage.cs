
namespace App3.Data
{
    public enum MessageTypes
    {
        Text,
        Info,
        Img,
        Link,
        File,
        Sticker,
        Photo,
        Video,
    }

    public enum MessageSources
    {
        Self,
        Other,
        None,
    }

    public class ChatMessage : NotificationBase
    {

        public MessageTypes MessageType { get; set; }
        public MessageSources MessageSource { get; set; }
        public string DisplayName { get; set; }
        public string UserID { get; set; }
        public string Message { get; set; }
        public string MessageData { get; set; }
        public string MessageAditionalData { get; set; }
        public bool SendingInProgress { get; set; }
    
        private bool _previousMessageHasSameSender;
        public bool PreviousMessageHasSameSender
        {
            get => _previousMessageHasSameSender;
            set => SetProperty(ref _previousMessageHasSameSender, value);
        }

        public override string ToString()
        {
            if (MessageSource == MessageSources.None)
                return Message;
            return DisplayName + ": " + Message;
        }

        public bool Equals(ChatMessage other)
        {

            bool isDataTheSame = MessageData == other.MessageData;
            if (MessageType == MessageTypes.Link && MessageData!=null && other.MessageData!=null)
            {
                var indexOf1 = MessageData.IndexOf('&');
                var indexOf2 = other.MessageData.IndexOf('&');
                if (indexOf1 >= 0 && indexOf2 >= 0)
                {
                    if (indexOf1 == indexOf2)
                    {
                        // no choice to trust as fb generate new link data each time 
                        isDataTheSame = true; // MessageData.Substring(0, indexOf1) == other.MessageData.Substring(0, indexOf2);
                    }
                    else
                    {
                        isDataTheSame = false;
                    }
                }
            }else if (MessageType == MessageTypes.Video || MessageType==MessageTypes.Photo)
            {
                isDataTheSame = MessageAditionalData == other.MessageAditionalData;
            }
            else if( MessageType==MessageTypes.Img)
            {
                var indexOf1 = MessageData.IndexOf("ht=scontent");
                var indexOf2 = other.MessageData.IndexOf("ht=scontent");
                if (indexOf1 >= 0 && indexOf2 >= 0)
                {
                    if (indexOf1 == indexOf2)
                    {
                        isDataTheSame = MessageData.Substring(0, indexOf1) == other.MessageData.Substring(0, indexOf2);
                    }
                    else
                    {
                        isDataTheSame = false;
                    }
                }
            }
            if (MessageType == other.MessageType &&
                MessageSource == other.MessageSource &&
                DisplayName == other.DisplayName &&
                UserID == other.UserID &&
                Message == other.Message &&
                isDataTheSame)
                return true;


            return false;
        }
    }
}
