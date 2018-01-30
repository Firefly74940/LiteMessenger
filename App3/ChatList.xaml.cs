using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatList : Page
    {
        public ChatList()
        {
            this.InitializeComponent();
            listView.ItemsSource = App.Names;
            
            //foreach (var name in MainPage.Names )
            //{
            //    listView.
            //}

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame.BackStack.Clear(); ;

            RefreshChatList();

        }
        private async void RefreshChatList()
        {

            var htmlDoc = await MainPage.GetHtmlDoc("/messages/");

            {
                var profileNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"header\"]/div/a[2]");
                var namelink = profileNode.GetAttributeValue("href", "");
                int end = namelink.IndexOf('?');
                App.Username = namelink.Substring(1, end - 1);
            }
            //var node = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"root\"]/div[1]/div[2]/div[1]/table[1]/tbody");//*[@id="root"]/div[1]/div[2]/div[1]/table[1]/tbody/tr/td/div/h3[1]/a
            var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"root\"]/div[1]/div[2]/div[1]/table");//*[@id="root"]/div[1]/div[2]/div[1]/table[1]/tbody/tr/td/div/h3[1]/a
            App.Names.Clear();
            foreach (var node in nodes)
            {
                var NameNode = node.SelectSingleNode("tr/td/div/h3[1]/a");
                
                App.Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
           // listView.ItemsSource = App.Names;
            // this.Frame.Navigate(typeof(ChatList));
        }
        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var chat = e.ClickedItem as ChatHeader;
            if (chat != null)
            {
                this.Frame.Navigate(typeof(ChatPage),chat);
            }

        }


    }
}
