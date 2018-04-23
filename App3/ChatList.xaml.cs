﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
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

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            RefreshChatList();

        }
        public async void RefreshChatList()
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

                var split = NameNode.InnerText.Split(new[] { '(', ')' });
                var name = split[0];
                int badgeCount = split.Length > 1 ? int.Parse(split[1]) : 0;
                App.Names.Add(new ChatHeader() { Name = name, Href = NameNode.GetAttributeValue("href", ""), UnreadCount = badgeCount });
            }
            // listView.ItemsSource = App.Names;
            // this.Frame.Navigate(typeof(ChatList));
        }
        private void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var chat = e.ClickedItem as ChatHeader;
            if (chat != null)
            {
                this.Frame.Navigate(typeof(ChatPage), chat);
            }

        }

        private async void button_Click(object sender, RoutedEventArgs e)
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
                    annotation.SupportedOperations = ContactAnnotationOperations.Message|ContactAnnotationOperations.ContactProfile;

                    // Save annotation to contact annotation list
                    // Windows.ApplicationModel.Contacts.ContactAnnotationList 
                   // await annotationList.TrySaveAnnotationAsync(annotation);
                }
                await annotationList.TrySaveAnnotationAsync(annotation);
            }


        }
    }
}
