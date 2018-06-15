using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using HtmlAgilityPack;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace App3.Data
{
    public class ChatHeader
    {
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public string Href { get; set; }
        public int UnreadCount { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public int Order = -1;

        private string _action = "";
        private string _formEncoded = "";

        public ObservableCollection<ChatMessage> Messages=new ObservableCollection<ChatMessage>();

        public void GetSubmitForm(HtmlDocument page)
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

        public async void SendMessage(string message, ChatPage callback)
        {
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    new Uri(DataSource.requestUri, _action));
            HttpRequestMessage.Content = new HttpStringContent(_formEncoded += "&body=" + Uri.EscapeDataString(message), UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            HttpRequestMessage.Headers.Add("User-Agent", DataSource.CustomUserAgent);

            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(HttpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(httpResponseBody);
                RefreshConversation(htmlDoc);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }

        public async void RefreshConversation(HtmlDocument htmlDoc = null)
        {
            if (htmlDoc == null)
                htmlDoc = await DataSource.GetHtmlDoc(Href);
            Messages.Clear();

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
                Messages.Add(newMessage);
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
                                    if (string.Equals(MessageUsername, DataSource.Username))
                                    {
                                        Messages.Add(new ChatMessage()
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
                                        Messages.Add(new ChatMessage()
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
                                else if (string.Equals(MessageUsername, DataSource.Username))
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
                                Messages.Add(newMessage);

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

                                    if (string.Equals(MessageUsername, DataSource.Username))
                                    {
                                        Messages.Add(new ChatMessage()
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
                                        Messages.Add(new ChatMessage()
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

                                        if (string.Equals(MessageUsername, DataSource.Username))
                                        {
                                            Messages.Add(new ChatMessage()
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
                                            Messages.Add(new ChatMessage()
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
                    Messages.Add(new ChatMessage() { Message = HtmlEntity.DeEntitize(messagePackNode.InnerText), MessageSource = MessageSources.None, MessageType = MessageTypes.Info });
                }
                //   Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
            //this.Frame.Navigate(typeof(ChatList));
           
        }
    }
}
