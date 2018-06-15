﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Web.Http;

namespace App3.Data
{
   static class DataSource
    {

        public static ObservableCollection<ChatHeader> Names = new ObservableCollection<ChatHeader>();
      
        public static Uri requestUri = new Uri("https://mbasic.facebook.com");
        public const string CustomUserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.2490.71 Safari/537.36";

        private static bool _localSettingsInitialized = false;
        private static bool _isLogedIn = false;
        public static bool IsLogedIn
        {
            get
            {
                if (!_localSettingsInitialized)
                {
                    InitLocalSettings();
                }
                return _isLogedIn;

            }
            set
            {
                _isLogedIn = value;
                localSettings.Values["isLogin"] = value;
            }
        }
        private static string _username;
        public static string Username
        {
            get
            {
                if (!_localSettingsInitialized)
                {
                    InitLocalSettings();
                }

                return _username;

            }
            set
            {
                _username = value;
                localSettings.Values["username"] = value;
            }
        }



        public static Windows.Storage.ApplicationDataContainer localSettings;

        public static async void RefreshChatList()
        {

            var htmlDoc = await MainPage.GetHtmlDoc("/messages/");

            {
                var profileNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"header\"]/div/a[2]");
                if (profileNode == null) return; //@TODO handle no internet 

                var namelink = profileNode.GetAttributeValue("href", "");
                int end = namelink.IndexOf('?');
                if (string.IsNullOrEmpty(Username))
                {
                    Username = namelink.Substring(1, end - 1);
                }
            }

            var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"root\"]/div[1]/div[2]/div[1]/table");


            // tag all for potential deletion
            for (var index = 0; index < Names.Count; index++)
            {
                Names[index].Order = -1;
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
            for (var index = 0; index < Names.Count; index++)
            {
                if (Names[index].Order == -1)
                {
                    Names.RemoveAt(index);
                    index--;
                }
            }

            Names.SortSlow((a, b) => { return a.Order.CompareTo(b.Order); });
        }

        private static ChatHeader FindOrCreateHeader(string href)
        {

            foreach (var chatHeader in Names)
            {
                if (chatHeader.Href == href)
                {
                    return chatHeader;
                }
            }

            var newHeader = new ChatHeader() { Href = href };
            Names.Add(newHeader);
            return newHeader;
        }

        public static void Disconect()
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

           IsLogedIn = false;
 
            Username = string.Empty;
          
            Names.Clear();
        }
        public static async void SyncContacts()
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
            foreach (var chatHeader in Names)
            {
                if (chatHeader.IsGroup)
                    continue;
                Contact contact = new Contact
                {
                    FirstName = chatHeader.Name,
                    RemoteId = chatHeader.Href
                };
                await contactList.SaveContactAsync(contact);

                ContactAnnotation annotation = new ContactAnnotation
                {
                    ContactId = contact.Id,
                    RemoteId = chatHeader.Href,
                    SupportedOperations = ContactAnnotationOperations.Message
                };


                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5))
                {

                    annotation.ProviderProperties.Add("ContactPanelAppID", appId);
                    annotation.SupportedOperations = ContactAnnotationOperations.Message | ContactAnnotationOperations.ContactProfile;

                }
                await annotationList.TrySaveAnnotationAsync(annotation);
            }


        }

        public static void InitLocalSettings()
        {
            if(_localSettingsInitialized)return;
            _localSettingsInitialized = true;

            localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("isLogin"))
                _isLogedIn = (bool)localSettings.Values["isLogin"];
            if (localSettings.Values.ContainsKey("username"))
                Username = (string)localSettings.Values["username"];
        }
    }
}