using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

    public enum MessageSources
    {
        Self,
        Other,
        None,
    }

    public class ChatMessage
    {
        public MessageTypes MessageType { get; set; }
        public MessageSources MessageSource { get; set; }
        public string DisplayName { get; set; }
        public string UserID { get; set; }
        public string Message { get; set; }
        public string MessageData { get; set; }
        public override string ToString()
        {
            if (MessageSource == MessageSources.None)
                return Message;
            return DisplayName + ": " + Message;
        }

        public bool Equals(ChatMessage other)
        {

            bool isDataTheSame = MessageData == other.MessageData;
            if (MessageType == MessageTypes.Link)
                isDataTheSame = MessageData.Substring(0, MessageData.IndexOf('&')) == 
                                other.MessageData.Substring(0, other.MessageData.IndexOf('&'));
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
