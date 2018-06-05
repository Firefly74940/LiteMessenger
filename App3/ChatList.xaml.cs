using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame.BackStack.Clear();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            RefreshChatList();

        }
        public async void RefreshChatList()
        {

            var htmlDoc = await MainPage.GetHtmlDoc("/messages/");

            {
                var profileNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"header\"]/div/a[2]");
                if (profileNode == null) return; //@TODO handle no internet 

                var namelink = profileNode.GetAttributeValue("href", "");
                int end = namelink.IndexOf('?');
                if (string.IsNullOrEmpty(App.Username))
                {
                    App.Username = namelink.Substring(1, end - 1);
                    App.localSettings.Values["username"] = App.Username;
                }
            }

            var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"root\"]/div[1]/div[2]/div[1]/table");


            // tag all for potential deletion
            for (var index = 0; index < App.Names.Count; index++)
            {
                App.Names[index].Order = -1;
            }

            int order = 0;
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var NameNode = node.SelectSingleNode("tr/td/div/h3[1]/a");

                    var split = NameNode.InnerText.Split(new[] { '(', ')' });
                    var name = split[0];
                    int badgeCount = split.Length > 1 ? int.Parse(split[1]) : 0;
                    var href = NameNode.GetAttributeValue("href", "");
                    var header = FindOrCreateHeader(href);
                    header.Name = name;
                    header.UnreadCount = badgeCount;
                    header.Order = order;
                    order++;
                }
            }

            //RemoveOthers: 
            for (var index = 0; index < App.Names.Count; index++)
            {
                if (App.Names[index].Order == -1)
                {
                    App.Names.RemoveAt(index);
                    index--;
                }
            }

            App.Names.SortSlow((a, b) => { return a.Order.CompareTo(b.Order); });
        }

        private ChatHeader FindOrCreateHeader(string href)
        {

            foreach (var chatHeader in App.Names)
            {
                if (chatHeader.Href == href)
                {
                    return chatHeader;
                }
            }

            var newHeader = new ChatHeader() { Href = href };
            App.Names.Add(newHeader);
            return newHeader;
        }
        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var chat = e.ClickedItem as ChatHeader;
            if (chat != null)
            {
                this.Frame.Navigate(typeof(ChatPage), chat);
            }

        }

        private async void SyncContacts()
        {
            ContactList contactList;

            //CleanUp
            {
                ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                IReadOnlyList<ContactList> contactLists = await store.FindContactListsAsync();

                if (contactLists.Count > 0)
                    await contactLists[0].DeleteAsync();

                contactList = await store.CreateContactListAsync("Lite Messenger");
            }

            ContactAnnotationList annotationList;

            {
                ContactAnnotationStore annotationStore = await
                    ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);


                IReadOnlyList<ContactAnnotationList> annotationLists = await annotationStore.FindAnnotationListsAsync();
                if (annotationLists.Count > 0)
                    await annotationLists[0].DeleteAsync();

                annotationList = await annotationStore.CreateAnnotationListAsync();
            }


            String appId = "4a6ce7f5-f418-4ba8-8836-c06d77ab735d_g91sr9nghxvmm!App";
            foreach (var chatHeader in App.Names)
            {
                if (chatHeader.IsGroup)
                    continue;
                Contact contact = new Contact();
                contact.FirstName = chatHeader.Name;
                contact.RemoteId = chatHeader.Href;
                await contactList.SaveContactAsync(contact);

                ContactAnnotation annotation = new ContactAnnotation();
                annotation.ContactId = contact.Id;
                annotation.RemoteId = chatHeader.Href;

                annotation.SupportedOperations = ContactAnnotationOperations.Message;



                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {

                    annotation.ProviderProperties.Add("ContactPanelAppID", appId);
                    annotation.SupportedOperations = ContactAnnotationOperations.Message | ContactAnnotationOperations.ContactProfile;

                }
                await annotationList.TrySaveAnnotationAsync(annotation);
            }


        }

        private void CommandBar_Opening(object sender, object e)
        {
            BarButtonDisconect.Visibility = Visibility.Visible;
            BarButtonSync.Visibility = Visibility.Visible;
        }

        private void CommandBar_Closing(object sender, object e)
        {
            BarButtonDisconect.Visibility = Visibility.Collapsed;
            BarButtonSync.Visibility = Visibility.Collapsed;
        }

        private void BarButtonDisconect_Click(object sender, RoutedEventArgs e)
        {
            Windows.Web.Http.Filters.HttpBaseProtocolFilter myFilter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();


            var cookieManager = myFilter.CookieManager;
            HttpCookieCollection myCookieJar = cookieManager.GetCookies(new Uri("https://mbasic.facebook.com"));
            foreach (HttpCookie cookie in myCookieJar)
            {
                cookieManager.DeleteCookie(cookie);
            }
            myCookieJar = cookieManager.GetCookies(new Uri("https://mbasic.facebook.com"));
            foreach (HttpCookie cookie in myCookieJar)
            {
                cookieManager.DeleteCookie(cookie);
            }

            App._isLogedIn = false;
            App.localSettings.Values["isLogin"] = false;

            App.Username = string.Empty;
            App.localSettings.Values["username"] = string.Empty;
            App.Names.Clear();
            Frame.Navigate(typeof(MainPage));
        }

        private void BarButtonSync_Click(object sender, RoutedEventArgs e)
        {
            SyncContacts();
        }
    }
}
