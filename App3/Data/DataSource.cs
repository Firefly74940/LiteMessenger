using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Web.Http;
using HtmlAgilityPack;

namespace App3.Data
{
    static class DataSource
    {

        public static ObservableCollection<ChatHeader> Names = new ObservableCollection<ChatHeader>();
        public static Uri requestUri = new Uri("https://mbasic.facebook.com");
        public static string requestUriString = "https://mbasic.facebook.com";
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
        public static string NewMessageLink = "";
        public static Windows.Storage.ApplicationDataContainer localSettings;

        const int ChatCountPerPage = 10;
        const int ChatPagesToCrawlAtStart = 4;
        const int ChatPagesToCrawlForSmallRefreshes = 2;
        private static int _currentConvRefreshOrder = 0;
        public static bool ConvRefreshInProgress = false;
        public static int NextRefreshInXMs = 0;
        public enum RequestType
        {
            Start,
            Refresh,
        }
        public static async void TryRefreshConversationList(RequestType requestType = RequestType.Refresh)
        {
            if (ConvRefreshInProgress) return;
            ConvRefreshInProgress = true;

            await RefreshChatList(null, requestType == RequestType.Refresh ? ChatPagesToCrawlForSmallRefreshes : ChatPagesToCrawlAtStart);

            ConvRefreshInProgress = false;

        }
        static async Task RefreshChatList(string requestedUrl = null, int maxPageToCrawl = 1)
        {
            maxPageToCrawl--;
            var isFirstPass = string.IsNullOrEmpty(requestedUrl);
            var chatListUrl = isFirstPass ? "/messages/" : requestedUrl;
            var htmlDoc = await GetHtmlDoc(chatListUrl);

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

            if (string.IsNullOrEmpty(NewMessageLink))
            {
                var newMessageNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"root\"]/div[1]/div[1]/table/tbody/tr/td[1]/a");
                if (newMessageNode != null)
                {
                    NewMessageLink = HtmlEntity.DeEntitize(newMessageNode.GetAttributeValue("href", ""));
                }
            }

            string nextConvsPageLink = "";
            if (maxPageToCrawl > 0)
            {
                var olderMessagesLinkNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"see_older_threads\"]/a");
                if (olderMessagesLinkNode != null)
                {
                    nextConvsPageLink = HtmlEntity.DeEntitize(olderMessagesLinkNode.GetAttributeValue("href", ""));
                }

            }

            var messagesNodeXPath = isFirstPass ? "//*[@id=\"root\"]/div[1]/div[2]/div[1]/table" : "//*[@id=\"root\"]/div[1]/div[2]/div[2]/table";
            var nodes = htmlDoc.DocumentNode.SelectNodes(messagesNodeXPath);

            if (isFirstPass)
            {
                // tag all for potential deletion
                for (var index = 0; index < Names.Count; index++)
                {
                    if (Names[index].NewOrder < 500)
                        Names[index].NewOrder += 100;
                }


                _currentConvRefreshOrder = 0;
            }

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var NameNode = node.SelectSingleNode("tr/td/div/h3[1]/a");

                    var split = NameNode.InnerText.Split(new[] { '(', ')' });
                    var name = split[0];
                    int badgeCount = 0;

                    if (split.Length > 1)
                    {
                        int parsedInt = 0;
                        if (int.TryParse(split[1], out parsedInt))
                            badgeCount = parsedInt;
                    }

                    if (badgeCount == 0)
                    {
                        badgeCount = node.GetClasses().Count() > 8 ? -1 : 0;
                    }


                    var href = NameNode.GetAttributeValue("href", "");

                    var messagePreviewNode = node.SelectSingleNode("tr/td/div/h3[2]");

                    var header = FindOrCreateHeader(href);
                    header.Name = HtmlEntity.DeEntitize(name);
                    header.UnreadCount = badgeCount;
                    header.NewOrder = _currentConvRefreshOrder;
                    header.MessagePreview = HtmlEntity.DeEntitize(GetTextmessageFromNode(messagePreviewNode.FirstChild));
                    _currentConvRefreshOrder++;
                }
            }


            List<SortingData> sortingData = new List<SortingData>();
            for (var index = 0; index < Names.Count; index++)
            {
                sortingData.Add(new SortingData() { Order = Names[index].Order, Index = index, NewOrder = Names[index].NewOrder });
            }
            sortingData.Sort((p, q) => p.NewOrder.CompareTo(q.NewOrder));

            for (int i = 0; i < sortingData.Count; i++)
            {
                var indexInName = sortingData[i].Index;
                if (sortingData[i].NewOrder == -1)
                {
                    Names.RemoveAt(indexInName);
                    for (int j = 0; j < sortingData.Count; j++)
                    {
                        //if item was after removed item it's now suposed order is naturally lower 
                        if (sortingData[i].Order < sortingData[j].Order)
                        {
                            sortingData[j].Order -= 1;
                        }

                        //if item was after removed item it's new index is naturally lower 
                        if (indexInName < sortingData[j].Index)
                        {
                            sortingData[j].Index -= 1;
                        }
                    }
                }
                else if (sortingData[i].NewOrder < 100)
                {
                    if (sortingData[i].NewOrder == sortingData[i].Order) // allredy at the right place
                    {
                        Names[indexInName].Order = sortingData[i].NewOrder;
                    }
                    else
                    {
                        Names[indexInName].SetProperty<int>(ref Names[indexInName].Order, Names[indexInName].NewOrder, "Order");

                        for (int j = 0; j < sortingData.Count; j++)
                        {
                            //if item was before and is now after moved element it now supposed order is higher
                            if ((sortingData[i].Order > sortingData[j].Order) && (sortingData[i].NewOrder <= sortingData[j].Order) && sortingData[j].Order < 100)
                                sortingData[j].Order += 1;
                        }
                    }
                }
            }

            if (maxPageToCrawl > 0 && !string.IsNullOrEmpty(NewMessageLink))
            {
                await RefreshChatList(nextConvsPageLink, maxPageToCrawl);
            }
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
                //if (chatHeader.IsGroup)
                //    continue;
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
            if (_localSettingsInitialized) return;
            _localSettingsInitialized = true;

            localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("isLogin"))
                _isLogedIn = (bool)localSettings.Values["isLogin"];
            if (localSettings.Values.ContainsKey("username"))
                Username = (string)localSettings.Values["username"];
        }

        public static async Task<HtmlDocument> GetHtmlDoc(string relativeUri)
        {
            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get,
                    new Uri(DataSource.requestUri, relativeUri));

            HttpRequestMessage.Headers.Add("User-Agent", DataSource.CustomUserAgent);
            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(HttpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(httpResponseBody);
            return htmlDoc;
        }

        public static string GetTextmessageFromNode(HtmlNode node)
        {
            string buildedMessage = "";

            foreach (var textNode in node.ChildNodes)
            {
                if (textNode.Name == "#text")
                {
                    buildedMessage += textNode.InnerText;
                }
                else if (textNode.Name == "i")
                {
                    var isrc = textNode.GetAttributeValue("style", "");
                    if (!string.IsNullOrEmpty(isrc))
                    {
                        var nameStart = isrc.LastIndexOf('/');
                        var nameEnd = isrc.LastIndexOf('.');

                        if (nameStart != -1 && nameEnd != -1)
                        {
                            var name = isrc.Substring(nameStart + 1, nameEnd - nameStart - 1);

                            var codes = name.Split('_');
                            foreach (var code in codes)
                            {
                                int parsedInt = 0;
                                if (int.TryParse(code,  NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsedInt))
                                {
                                    var x = char.ConvertFromUtf32(parsedInt);
                                    buildedMessage += x;
                                }
                            }

                        }

                    }
                }
                else if (textNode.Name == "img")
                {
                    var isrc = textNode.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(isrc))
                    {
                        var nameStart = isrc.LastIndexOf('/');
                        var nameEnd = isrc.LastIndexOf('.');


                        if (nameStart != -1 && nameEnd != -1)
                        {
                            var name = isrc.Substring(nameStart + 1, nameEnd - nameStart - 1);

                            var codes = name.Split('_');
                            foreach (var code in codes)
                            {
                                try
                                {
                                    var utf32 = int.Parse(code, NumberStyles.HexNumber);
                                    var x = char.ConvertFromUtf32(utf32);
                                    buildedMessage += x;
                                }
                                catch (Exception e)
                                {

                                }

                            }

                        }
                        else
                        {
                            //full image ? 
                        }

                    }
                }
            }

            return buildedMessage;
        }

        public static async Task<string> GetNameFromRemotId(string remotID)
        {
            if (string.IsNullOrEmpty(remotID)) return "Unknown";
            ContactList contactList;


            {
                ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
                IReadOnlyList<ContactList> contactLists = await store.FindContactListsAsync();

                if (contactLists.Count > 0)
                    contactList = contactLists[0];
                else return "Unknown";

            }

            Contact contact = await contactList.GetContactFromRemoteIdAsync(remotID);

            if (contact != null)
                return contact.Name;

            return "Unknown";
        }
        public static async Task<string> GetRemoteIdForContactIdAsync(Contact contactId)
        {
            ContactStore store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            var fullContact = await store.GetContactAsync(contactId.Id);
            ContactAnnotationStore annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);

            var contactAnnotations = await annotationStore.FindAnnotationsForContactAsync(fullContact);

            if (contactAnnotations.Count > 0)
            {
                return contactAnnotations[0].RemoteId;
            }

            return string.Empty;
        }





        public static void InitSystemHooks()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            _hasInternet = NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }

        private static void NetworkInformation_NetworkStatusChanged(object sender)
        {
            bool newValue = false;
            newValue = NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;

            if (newValue == _hasInternet) return;
            _hasInternet = newValue;
            NetworkStatusChanged?.Invoke(newValue);
        }

        private static bool _hasInternet;
        public static bool HasInternet
        {
            get { return _hasInternet; }
        }
        public delegate void NetworkStatusChangedEventHandler(bool newHasInternet);
        public static event NetworkStatusChangedEventHandler NetworkStatusChanged;
    }
}
