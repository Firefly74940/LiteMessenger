using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App3.Data;

namespace App3.ViewModels
{
    class ChatViewModel : NotificationBase<ChatHeader>
    {
        public string Name => This.Name;

        public int UnreadCount => This.UnreadCount;

        public override string ToString() => This.ToString();

        private ObservableCollection<ChatMessageViewModel> _messages;

        public ObservableCollection<ChatMessageViewModel> Messages => _messages;

        public ChatViewModel(ChatHeader chat = null): base (chat)
        {
            Func<ChatMessage, ChatMessageViewModel> viewModelCreator = model => new ChatMessageViewModel(model);

            _messages= new ObservableViewModelCollection<ChatMessageViewModel, ChatMessage>(chat.Messages, viewModelCreator);

        }

       
    }
}
