using System;
using System.Collections.ObjectModel;
using App3.Data;
using Microsoft.Toolkit.Uwp.UI;

namespace App3.ViewModels
{
    class AccountViewModel : NotificationBase
    {

        private static AccountViewModel _instance;

        public static AccountViewModel Instance
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
            _chatsAcv = new AdvancedCollectionView(_chats,true);
            _chatsAcv.SortDescriptions.Add(new SortDescription("Order", SortDirection.Ascending));
            _chatsAcv.Filter = x => x is ChatViewModel;
        }

        
        private ObservableCollection<ChatViewModel> _chats;
        private AdvancedCollectionView _chatsAcv;

        public AdvancedCollectionView Chats => _chatsAcv;


        public  void RefreshChatList()
        {
           
                DataSource.RefreshChatList();
            
        }
    }
}
