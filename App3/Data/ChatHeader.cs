using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;
using HtmlAgilityPack;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;

namespace App3.Data
{
    public class ChatHeader : NotificationBase
    {
        private bool _refreshInProgress = false;
        public string Name { get; set; }
        public bool IsGroup { get; set; }
        public string Href { get; set; }
        public int UnreadCount { get; set; }

        public override string ToString()
        {
            if (UnreadCount > 0)
                return $"{Name} ({UnreadCount})";
            return Name;
        }

        public int Order = -1;
        public int NewOrder = -1;
        private string _action = "";
        private string _formEncoded = "";


        public enum RequestType
        {
            GetNewMessages,
            GetOldMessages,
        }

        private int NewestMessagesIndex = 0;
        private string NewestMessagesLink = "";
        private string OlderMessagesLink = "";

        public readonly ObservableCollection<ChatMessage> Messages = new ObservableCollection<ChatMessage>();

        public void GetSubmitForm(HtmlDocument page)
        {
            var form = page.DocumentNode.SelectSingleNode("//*[@id=\"composer_form\"]");
            if(form == null)
                return;
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

        public async void SendMessage(string message)
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
                //RefreshConversation(RequestType.GetNewMessages);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }



        public async void RefreshConversation(RequestType requestType, HtmlDocument htmlDoc = null)
        {
            if(_refreshInProgress) return;
            _refreshInProgress = true;
            string hrefToLoad = Href;

            if (requestType == RequestType.GetNewMessages && !string.IsNullOrEmpty(NewestMessagesLink))
            {
                hrefToLoad = NewestMessagesLink;
            }
            else if (requestType == RequestType.GetOldMessages)
            {
                hrefToLoad = OlderMessagesLink;
            }
            if (htmlDoc == null)
                htmlDoc = await DataSource.GetHtmlDoc(hrefToLoad);
            // Messages.Clear();
            List<ChatMessage> newMessages = new List<ChatMessage>();
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
                newMessages.Add(newMessage);
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
                                        newMessages.Add(new ChatMessage()
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
                                        newMessages.Add(new ChatMessage()
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
                                newMessages.Add(newMessage);

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
                                        newMessages.Add(new ChatMessage()
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
                                        newMessages.Add(new ChatMessage()
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
                                            newMessages.Add(new ChatMessage()
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
                                            newMessages.Add(new ChatMessage()
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
                    newMessages.Add(new ChatMessage() { Message = HtmlEntity.DeEntitize(messagePackNode.InnerText), MessageSource = MessageSources.None, MessageType = MessageTypes.Info });
                }
                //   Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
            //this.Frame.Navigate(typeof(ChatList));

            AddMessages(newMessages, requestType);
            //
            // manage link to get new and old messages (at the end so NewestMessagesIndex is correct)
            //
            {
                var linkToOlderMessages = HtmlEntity.DeEntitize(htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"see_older\"]/a").GetAttributeValue("href", ""));

                if (string.IsNullOrEmpty(NewestMessagesLink))
                {
                    NewestMessagesLink = linkToOlderMessages.Replace("last_message_timestamp", "first_message_timestamp");
                    OlderMessagesLink = linkToOlderMessages;
                }

                if (requestType == RequestType.GetNewMessages)
                {
                    string linkToNewerMessages = HtmlEntity.DeEntitize(htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"see_newer\"]/a")?.GetAttributeValue("href", ""));
                    if (!string.IsNullOrEmpty(linkToNewerMessages))
                    {
                        NewestMessagesLink = AdjustTimestamp(linkToNewerMessages);
                        NewestMessagesIndex = Messages.Count;
                        //Request new refresh
                    }
                }

                if (requestType == RequestType.GetOldMessages)
                {
                    OlderMessagesLink = linkToOlderMessages;
                }
            }

            _refreshInProgress = false;
        }

        private string AdjustTimestamp(string linkToNewerMessages)
        {
            Match match = Regex.Match(linkToNewerMessages, @"first_message_timestamp=([0-9]+)&",
                RegexOptions.IgnoreCase);

            // Here we check the Match instance.
            if (match.Success)
            {
                // Finally, we get the Group value and display it.
                string key = match.Groups[1].Value;
                long newTimeStamp = long.Parse(key) + 1L;

                return linkToNewerMessages.Replace(key, newTimeStamp.ToString());
            }
            else
            {
                return linkToNewerMessages; // failed
            }
        }

        private void AddMessages(List<ChatMessage> newMessages, RequestType requestType)
        {
            if (requestType == RequestType.GetOldMessages)
            {
                for (int i = newMessages.Count - 1; i >= 0; i--)
                {
                    Messages.Insert(0, newMessages[i]);
                    NewestMessagesIndex++;
                }
            }
            else if (requestType == RequestType.GetNewMessages)
            {
                //while (Messages.Count > NewestMessagesIndex)
                //{
                //    Messages.RemoveAt(Messages.Count - 1);
                //}

                //for (int i = 0; i < newMessages.Count; i++)
                //{
                //    Messages.Add(newMessages[i]);
                //    //bool shouldAdd = false;
                //    //if (Messages.Count <= NewestMessagesIndex + i)
                //    //{

                //    //}
                //}

              

                for (int i = 0; i < newMessages.Count; i++)
                {
                   
                    bool shouldAdd = false;
                    if (Messages.Count <= NewestMessagesIndex + i)
                    {
                        Messages.Add(newMessages[i]);
                    }
                    else
                    {
                        if (Messages[NewestMessagesIndex + i].Equals(newMessages[i]))continue;

                      
                        //clear lastest Messages
                        

                        while (Messages.Count > NewestMessagesIndex + i)
                        {
                            Messages.RemoveAt(Messages.Count - 1);
                        }
                        Messages.Add(newMessages[i]);
                    }
                }
            }
        }
    }
}
