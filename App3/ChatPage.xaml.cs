using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web;
using HtmlAgilityPack;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace App3
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

    public class ChatInfoMessage : ChatMessage
    {

    }
    public class ChatImgOtherMessage : ChatMessage
    {

    }
    public class ChatImgSelfMessage : ChatMessage
    {

    }
    public class ChatTextSelfMessage : ChatMessage
    {

    }
    public class ChatTextOtherMessage : ChatMessage
    {

    }
    public class ChatLinkSelfMessage : ChatMessage
    {

    }
    public class ChatLinkOtherMessage : ChatMessage
    {

    }
    public class MessageDataTemplateSelector : DataTemplateSelector
    {

        public DataTemplate InfoMessage
        {
            get;
            set;
        }
        public DataTemplate SelfMessage
        {
            get;
            set;
        }

        public DataTemplate SelfLinkMessage
        {
            get;
            set;
        }
        public DataTemplate SelfImgMessage
        {
            get;
            set;
        }
        public DataTemplate OtherMessage
        {
            get;
            set;
        }
        public DataTemplate OtherLinkMessage
        {
            get;
            set;
        }
        public DataTemplate OtherImgMessage
        {
            get;
            set;
        }
        //public DataTemplate ReceivedTemplate
        //{
        //    get;
        //    set;
        //}
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            FrameworkElement elemnt = container as FrameworkElement;

            if (item is ChatTextOtherMessage)
            {
                return OtherMessage;
            }
            else if (item is ChatTextSelfMessage)
            {
                return SelfMessage;
            }
            else if (item is ChatLinkOtherMessage)
            {
                return OtherLinkMessage;
            }
            else if (item is ChatLinkSelfMessage)
            {
                return SelfLinkMessage;
            }
            else if (item is ChatImgOtherMessage)
            {
                return OtherImgMessage;
            }
            else if (item is ChatImgSelfMessage)
            {
                return SelfImgMessage;
            }
            else
            {
                return InfoMessage;
            }
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
                if (!name.Equals("undefined"))
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
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            //listView.Items.Add("");
            GetSubmitForm(htmlDoc);
            var messagePackNodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"messageGroup\"]/div[2]/div");
            if (messagePackNodes == null)
            {
                var newMessage = new ChatInfoMessage()
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
                                        listView.Items.Add(new ChatLinkSelfMessage()
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
                                        listView.Items.Add(new ChatLinkOtherMessage()
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
                                    newMessage = new ChatInfoMessage()
                                    {
                                        MessageSource = MessageSources.None,
                                        MessageType = MessageTypes.Info
                                    };
                                }
                                else if (string.Equals(MessageUsername, App.Username))
                                {
                                    newMessage = new ChatTextSelfMessage()
                                    {
                                        MessageSource = MessageSources.Self,
                                        MessageType = MessageTypes.Text
                                    };
                                }
                                else
                                {
                                    newMessage = new ChatTextOtherMessage()
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
                                        listView.Items.Add(new ChatLinkSelfMessage()
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
                                        listView.Items.Add(new ChatLinkOtherMessage()
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
                                            listView.Items.Add(new ChatImgSelfMessage()
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
                                            listView.Items.Add(new ChatImgOtherMessage()
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
                    listView.Items.Add(new ChatInfoMessage() { Message = HtmlEntity.DeEntitize(messagePackNode.InnerText), MessageType = MessageTypes.Info });
                }
                //   Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
            //this.Frame.Navigate(typeof(ChatList));
            if (listView.Items.Count > 0)
                listView.ScrollIntoView(listView.Items.Last());
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            listView.Items.Add(new ChatTextSelfMessage() { Message = NewMessageBox.Text, UserID = App.Username, DisplayName = App.Username });


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
