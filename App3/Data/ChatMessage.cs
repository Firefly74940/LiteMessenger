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
    }
}
