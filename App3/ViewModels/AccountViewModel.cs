using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App3.Data;

namespace App3.ViewModels
{
    class AccountViewModel : NotificationBase
    {

        private static AccountViewModel _instance;

        private static AccountViewModel Instance
        {
            get
            {
                if( _instance == null)
                    _instance= new AccountViewModel();
                return _instance;
            }
        }


        public AccountViewModel()
        {
            Func<ChatHeader, ChatViewModel> viewModelCreator = model => new ChatViewModel(model);

            _chats = new ObservableViewModelCollection<ChatViewModel, ChatHeader>(DataSource.Names, viewModelCreator);
        }

        
        private ObservableCollection<ChatViewModel> _chats;

        public ObservableCollection<ChatViewModel> Chats => _chats;

       
    }
}
