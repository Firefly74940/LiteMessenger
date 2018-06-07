using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using App3.Data;
using HtmlAgilityPack;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
{
    

    public class MessageDataTemplateSelector : DataTemplateSelector
    {

        public DataTemplate InfoMessage { get; set; }
        public DataTemplate SelfMessage { get; set; }

        public DataTemplate SelfLinkMessage { get; set; }
        public DataTemplate SelfImgMessage { get; set; }
        public DataTemplate OtherMessage { get; set; }
        public DataTemplate OtherLinkMessage { get; set; }

        public DataTemplate OtherImgMessage { get; set; }

        //public DataTemplate ReceivedTemplate
        //{
        //    get;
        //    set;
        //}
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            FrameworkElement elemnt = container as FrameworkElement;

            var message = item as ChatMessage;
            if (message == null) return null;
            bool fromSelf = message.MessageSource == MessageSources.Self;
            switch (message.MessageType)
            {
                case MessageTypes.Text:
                    return fromSelf ? SelfMessage : OtherMessage;

                case MessageTypes.Info:
                    return InfoMessage;

                case MessageTypes.Img:
                    return fromSelf ? SelfImgMessage : OtherImgMessage;

                case MessageTypes.Link:
                    return fromSelf ? SelfLinkMessage : OtherLinkMessage;
            }

            return InfoMessage;
        }
    }

    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class ChatPage : Page
    {
        public ChatPage()
        {
            this.InitializeComponent();


        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ChatHeader)
            {
                RefreshConversation(e.Parameter as ChatHeader);
                ChatName.Text = (e.Parameter as ChatHeader).Name;
            }

        }

        private string _action = "";
        private string _formEncoded = "";
        private void GetSubmitForm(HtmlDocument page)
        {
            var form = page.DocumentNode.SelectSingleNode("//*[@id=\"composer_form\"]");
            var inputsPost = new List<Tuple<string, string>>();
            _action = form.GetAttributeValue("action", "");
            var inputs = form.Descendants("input");
            _formEncoded = "";
            foreach (var element in inputs)
            {
                string name = element.GetAttributeValue("name", "undefined");
                string value = element.GetAttributeValue("value", "");
                string type = element.GetAttributeValue("type", "");
                if (!name.Equals("undefined") && (!type.Equals("submit") || name.Equals("send")))
                {
                    inputsPost.Add(new Tuple<string, string>(name, value));
                    if (!string.IsNullOrEmpty(_formEncoded))
                        _formEncoded += '&';
                    _formEncoded += name + "=" + Uri.EscapeDataString(value);
                }
            }
        }
        private async void RefreshConversation(ChatHeader header, HtmlDocument htmlDoc = null)
        {
            if (htmlDoc == null)
                htmlDoc = await MainPage.GetHtmlDoc(header.Href);
            listView.Items.Clear();
           
            GetSubmitForm(htmlDoc);
            var messagePackNodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"messageGroup\"]/div[2]/div");
            if (messagePackNodes == null)
            {
                var newMessage = new ChatMessage()
                {
                    MessageSource = MessageSources.None,
                    MessageType = MessageTypes.Info,
                    Message = "Error parsing messages"
                };
                listView.Items.Add(newMessage);
                return;
            }
            foreach (var messagePackNode in messagePackNodes)
            {

                var MessageUsername = "";
                var MessageDisplayUsername = "";
                var NameNode = messagePackNode.SelectSingleNode("div[1]/a");
                if (NameNode != null)
                {
                    var namelink = NameNode.GetAttributeValue("href", "");
                    int end = namelink.IndexOf('?');
                    MessageUsername = namelink.Substring(1, end - 1);
                    MessageDisplayUsername = HtmlEntity.DeEntitize(NameNode.InnerText);
                }


                var messageNodes = messagePackNode.SelectNodes("div[1]/div");
                if (messageNodes != null)
                {
                    foreach (var messageNode in messageNodes)
                    {
                        #region MessageText
                        if (messageNode.FirstChild.Name == "span")
                        {
                            string buildedMessage = "";

                            foreach (var textNode in messageNode.FirstChild.ChildNodes)
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
                                            var x = char.ConvertFromUtf32(int.Parse(name, NumberStyles.HexNumber));
                                            buildedMessage += x;
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
                                            var x = char.ConvertFromUtf32(int.Parse(name, NumberStyles.HexNumber));
                                            buildedMessage += x;
                                        }
                                        else
                                        {
                                            //full image ? 
                                        }

                                    }
                                }
                                else if (textNode.Name == "a")
                                {
                                    if (string.Equals(MessageUsername, App.Username))
                                    {
                                        listView.Items.Add(new ChatMessage()
                                        {
                                            MessageData = HtmlEntity.DeEntitize(textNode.GetAttributeValue("href", "")),
                                            Message = HtmlEntity.DeEntitize(textNode.InnerText),
                                            MessageType = MessageTypes.Link,
                                            MessageSource = MessageSources.Self,
                                            UserID = MessageUsername,
                                            DisplayName = MessageDisplayUsername,
                                        });
                                    }
                                    else
                                    {
                                        listView.Items.Add(new ChatMessage()
                                        {
                                            MessageData = HtmlEntity.DeEntitize(textNode.GetAttributeValue("href", "")),
                                            Message = HtmlEntity.DeEntitize(textNode.InnerText),
                                            MessageType = MessageTypes.Link,
                                            MessageSource = MessageSources.Other,
                                            UserID = MessageUsername,
                                            DisplayName = MessageDisplayUsername,
                                        });
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(buildedMessage))
                            {
                                ChatMessage newMessage = null;
                                if (string.IsNullOrWhiteSpace(MessageUsername))
                                {
                                    newMessage = new ChatMessage()
                                    {
                                        MessageSource = MessageSources.None,
                                        MessageType = MessageTypes.Info
                                    };
                                }
                                else if (string.Equals(MessageUsername, App.Username))
                                {
                                    newMessage = new ChatMessage()
                                    {
                                        MessageSource = MessageSources.Self,
                                        MessageType = MessageTypes.Text
                                    };
                                }
                                else
                                {
                                    newMessage = new ChatMessage()
                                    {
                                        MessageSource = MessageSources.Other,
                                        MessageType = MessageTypes.Text
                                    };
                                }
                                newMessage.Message = HtmlEntity.DeEntitize(buildedMessage);
                                newMessage.UserID = MessageUsername;
                                newMessage.DisplayName = MessageDisplayUsername;
                                listView.Items.Add(newMessage);

                            }
                        }
                        #endregion
                        #region MessageImg & link

                        if (messageNode.LastChild.Name == "div")
                        {
                            var userImages = messageNode.LastChild.SelectNodes("a");
                            if (userImages != null && userImages.Count > 0)
                            {
                                if (!String.IsNullOrEmpty(userImages[0].InnerText))
                                {
                                    #region Link

                                    if (string.Equals(MessageUsername, App.Username))
                                    {
                                        listView.Items.Add(new ChatMessage()
                                        {
                                            MessageData = HtmlEntity.DeEntitize(userImages[0].GetAttributeValue("href", "")),
                                            Message = HtmlEntity.DeEntitize(userImages[0].InnerText),
                                            MessageType = MessageTypes.Link,
                                            MessageSource = MessageSources.Self,
                                            UserID = MessageUsername,
                                            DisplayName = MessageDisplayUsername,
                                        });
                                    }
                                    else
                                    {
                                        listView.Items.Add(new ChatMessage()
                                        {
                                            MessageData = HtmlEntity.DeEntitize(userImages[0].GetAttributeValue("href", "")),
                                            Message = HtmlEntity.DeEntitize(userImages[0].InnerText),
                                            MessageType = MessageTypes.Link,
                                            MessageSource = MessageSources.Other,
                                            UserID = MessageUsername,
                                            DisplayName = MessageDisplayUsername,
                                        });
                                    }

                                    #endregion
                                }
                            }
                            else
                            {
                                var otherImages = messageNode.LastChild.SelectNodes("img");
                                if (otherImages != null)
                                {
                                    foreach (var otherImage in otherImages)
                                    {
                                        var imgSrc = HtmlEntity.DeEntitize(otherImage.GetAttributeValue("src", ""));

                                        if (string.Equals(MessageUsername, App.Username))
                                        {
                                            listView.Items.Add(new ChatMessage()
                                            {
                                                MessageData = imgSrc,
                                                Message = HtmlEntity.DeEntitize(otherImage.GetAttributeValue("alt", "")),
                                                MessageType = MessageTypes.Img,
                                                MessageSource = MessageSources.Self,
                                                UserID = MessageUsername,
                                                DisplayName = MessageDisplayUsername,
                                            });
                                        }
                                        else
                                        {
                                            listView.Items.Add(new ChatMessage()
                                            {
                                                MessageData = imgSrc,
                                                Message = HtmlEntity.DeEntitize(otherImage.GetAttributeValue("alt", "")),
                                                MessageType = MessageTypes.Img,
                                                MessageSource = MessageSources.Other,
                                                UserID = MessageUsername,
                                                DisplayName = MessageDisplayUsername,
                                            });
                                        }

                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                else if (!string.IsNullOrEmpty(messagePackNode.InnerText))
                {
                    listView.Items.Add(new ChatMessage() { Message = HtmlEntity.DeEntitize(messagePackNode.InnerText), MessageSource = MessageSources.None, MessageType = MessageTypes.Info });
                }
                //   Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
            //this.Frame.Navigate(typeof(ChatList));
            if (listView.Items.Count > 0)
                listView.ScrollIntoView(listView.Items.Last());
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            //  var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            //var a =  listView.ContainerFromIndex(0) as ListViewItem;
            //  Rect screenBounds = new Rect(0, 0,bounds.Width, bounds.Height);

            //  if (VisualTreeHelper.FindElementsInHostCoordinates(screenBounds, listView).Contains(a))
            //      Debug.WriteLine("Element is now visible");
            //  else
            //      Debug.WriteLine("Element is no longer visible");

            //  return;
            listView.Items.Add(new ChatMessage() { Message = NewMessageBox.Text, UserID = App.Username, DisplayName = App.Username });


            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    new Uri(App.requestUri, _action));
            HttpRequestMessage.Content = new HttpStringContent(_formEncoded += "&body=" + Uri.EscapeDataString(NewMessageBox.Text), UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            HttpRequestMessage.Headers.Add("User-Agent", App.CustomUserAgent);

            NewMessageBox.Text = "";

            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(HttpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(httpResponseBody);
                RefreshConversation(null, htmlDoc);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            NewMessageBox.Text = "";
        }

        private void NewMessageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Send.IsEnabled = !string.IsNullOrEmpty(NewMessageBox.Text);
        }

        private void NewMessageBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (!string.IsNullOrEmpty(NewMessageBox.Text))
                {
                    Send_Click(null, null);
                }
            }

        }
    }


}
