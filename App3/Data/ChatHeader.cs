using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private bool _sendInProgress = false;
        private bool _sendLikeInProgress = false;
        private bool _sendPhotoInProgress = false;
        private int _sendingMessagesToRemove = 0;
        public bool RefreshInProgress => _refreshInProgress;
        public int RefreshInterval = 500;
        public int NextRefreshInXMs = 0;

        private string _sendStickerLink = "";
        public string SendStickerLink => _sendStickerLink;

        public string Name { get; set; }


        private string _messagePreview;
        public string MessagePreview
        {
            get => _messagePreview;
            set => SetProperty(ref _messagePreview, value);
        }

        public bool IsGroup { get; set; }

        private string _href;
        public string Href
        {
            get => _href;
            set
            {
                _href = value;
                IsGroup = _href.Contains("cid.g.");
            }


        }

        private int _unreadCount;
        public int UnreadCount
        {
            get => _unreadCount;
            set => SetProperty(ref _unreadCount, value);
        }

        public override string ToString()
        {
            if (UnreadCount > 0)
                return $"{Name} ({UnreadCount})";
            return Name;
        }

        public int Order = -1;
        public int NewOrder = -1;
        private string _formAction = "";
        private string _formEncodedSendPhoto = "";
        private string _formEncodedSendLike = "";
        private string _formEncodedSendMessage = "";


        public enum RequestType
        {
            GetNewMessages,
            GetOldMessages,
        }

        public int NewestMessagesIndex = 0;
        private string NewestMessagesLink = "";
        private string OlderMessagesLink = "";

        public readonly ObservableCollection<ChatMessage> Messages = new ObservableCollection<ChatMessage>();

        public List<ChatMessage> SendingMessages = new List<ChatMessage>();

        public void GetSubmitForm(HtmlDocument page)
        {
            var form = page.DocumentNode.SelectSingleNode("//*[@id=\"composer_form\"]");
            if (form == null)
                return;
            var inputsPost = new List<Tuple<string, string>>();
            _formAction = form.GetAttributeValue("action", "");
            var inputs = form.Descendants("input");
            _formEncodedSendMessage = "";
            _formEncodedSendLike = "";
            _formEncodedSendPhoto = "";
            foreach (var element in inputs)
            {
                string name = element.GetAttributeValue("name", "undefined");
                string value = element.GetAttributeValue("value", "");
                string type = element.GetAttributeValue("type", "");
                var isSubmitInput = type.Equals("submit");
                var isSendInput = name.Equals("send");
                var isSendPhotoInput = name.Equals("send_photo");
                var isLikeInput = name.Equals("like");
                if (!name.Equals("undefined") && (!isSubmitInput || isSendInput || isSendPhotoInput || isLikeInput))
                {
                    inputsPost.Add(new Tuple<string, string>(name, value));
                    if (!string.IsNullOrEmpty(_formEncodedSendMessage))
                    {
                        _formEncodedSendMessage += '&';
                        _formEncodedSendLike += '&';
                        _formEncodedSendPhoto += '&';

                    }

                    var inputFormated = name + "=" + Uri.EscapeDataString(value);
                    if (!isSubmitInput || isSendInput)
                        _formEncodedSendMessage += inputFormated;

                    if (!isSubmitInput || isSendPhotoInput)
                        _formEncodedSendPhoto += inputFormated;

                    if (!isSubmitInput || isLikeInput)
                        _formEncodedSendLike += inputFormated;
                }
            }
        }
        public void CheckSendingMessages()
        {
            if (_sendInProgress || SendingMessages.Count == 0) return;

            SendMessage(SendingMessages[0]);
        }
        public async void SendMessage(ChatMessage message)
        {
            _sendInProgress = true;
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    new Uri(DataSource.requestUri, _formAction))
                {
                    Content = new HttpStringContent(_formEncodedSendMessage += "&body=" + Uri.EscapeDataString(message.GetMessageAsString()),
                        UnicodeEncoding.Utf8, "application/x-www-form-urlencoded")
                };
            httpRequestMessage.Headers.Add("User-Agent", DataSource.CustomUserAgent);

            bool success = false;
            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(httpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
                success = true;
                //httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                //var htmlDoc = new HtmlDocument();
                //htmlDoc.LoadHtml(httpResponseBody);
                //RefreshConversation(RequestType.GetNewMessages);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            if (success)
            {
                _sendingMessagesToRemove++;
                SendingMessages.RemoveAt(0);
            }

            _sendInProgress = false;
            NextRefreshInXMs = 0;
            CheckSendingMessages();
        }

        public async void SendLike()
        {
            if (_sendLikeInProgress) return;
            _sendLikeInProgress = true;
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    new Uri(DataSource.requestUri, _formAction))
                {
                    Content = new HttpStringContent(_formEncodedSendLike,
                        UnicodeEncoding.Utf8, "application/x-www-form-urlencoded")
                };
            httpRequestMessage.Headers.Add("User-Agent", DataSource.CustomUserAgent);

            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(httpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }


            _sendLikeInProgress = false;
        }

        public async Task<string> GetSendPhotoLink()
        {
            string httpResponseBody = "";
            if (_sendPhotoInProgress) return httpResponseBody;
            _sendPhotoInProgress = true;
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();


            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage httpRequestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    new Uri(DataSource.requestUri, _formAction))
                {
                    Content = new HttpStringContent(_formEncodedSendPhoto,
                        UnicodeEncoding.Utf8, "application/x-www-form-urlencoded")
                };
            httpRequestMessage.Headers.Add("User-Agent", DataSource.CustomUserAgent);

            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(httpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = httpResponse.RequestMessage.RequestUri.ToString();
            }
            catch (Exception ex)
            {
                //httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }


            _sendPhotoInProgress = false;
            return httpResponseBody;
        }

        public async void RefreshConversation(RequestType requestType, HtmlDocument htmlDoc = null)
        {
            if (_refreshInProgress) return;
            _refreshInProgress = true;
            RefreshInterval = Math.Min(RefreshInterval + 150, 5000);
            string hrefToLoad = Href;

            if (requestType == RequestType.GetNewMessages && !string.IsNullOrEmpty(NewestMessagesLink))
            {
                hrefToLoad = NewestMessagesLink;
            }
            else if (requestType == RequestType.GetOldMessages)
            {
                hrefToLoad = OlderMessagesLink;
            }

            if (string.IsNullOrEmpty(hrefToLoad))
            {
                _refreshInProgress = false;
                return; // probably allready have all the new messages 
            }
            if (htmlDoc == null)
                htmlDoc = await DataSource.GetHtmlDoc(hrefToLoad);
            // Messages.Clear();
            List<ChatMessage> newMessages = new List<ChatMessage>();
            GetSubmitForm(htmlDoc);


            var sendStickerNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"composer_form\"]/span/a[1]");
            if (sendStickerNode != null)
            {
                _sendStickerLink = HtmlEntity.DeEntitize(sendStickerNode.GetAttributeValue("href", ""));
            }

            var messagePackNodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"messageGroup\"]/div[2]/div");


            // in case of no 'see previous messages'
            if (messagePackNodes == null)
                messagePackNodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"messageGroup\"]/div[1]/div");
            if (messagePackNodes == null)
            {
                LogErrorParsing();
                _refreshInProgress = false;
                return;
            }
            foreach (var messagePackNode in messagePackNodes)
            {

                var messageUsername = "";
                var messageDisplayUsername = "";
                var nameNode = messagePackNode.SelectSingleNode("div[1]/a");
                var messageSource = MessageSources.None;
                if (nameNode != null)
                {
                    var namelink = nameNode.GetAttributeValue("href", "");
                    int end = namelink.IndexOf('?');
                    messageUsername = namelink.Substring(1, end - 1);
                    messageDisplayUsername = HtmlEntity.DeEntitize(nameNode.InnerText);
                    if (string.IsNullOrWhiteSpace(messageUsername))
                    {
                        messageSource = MessageSources.None;
                    }
                    else if (string.Equals(messageUsername, DataSource.Username))
                    {
                        messageSource = MessageSources.Self;
                    }
                    else
                    {
                        messageSource = MessageSources.Other;
                    }
                }


                var messageNodes = messagePackNode.SelectNodes("div[1]/div");
                if (messageNodes != null)
                {
                    foreach (var messageNode in messageNodes)
                    {

                       // MessageTypes MessageType = GetMessageType(messageNode);
                        #region MessageText
                        if (messageNode.FirstChild.Name == "span")
                        {
                            List<MessageItem> buildedMessage = new List<MessageItem>();

                            foreach (var textNode in messageNode.FirstChild.ChildNodes)
                            {
                                if (textNode.Name == "#text")
                                {
                                    buildedMessage.Add(new MessageItem { Text = HtmlEntity.DeEntitize(textNode.InnerText), Type = MessageItemTypes.Text });
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
                                                var x = char.ConvertFromUtf32(int.Parse(code, NumberStyles.HexNumber));
                                                buildedMessage.Add(new MessageItem { Text = HtmlEntity.DeEntitize(x), Type = MessageItemTypes.Text });
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
                                            var x = char.ConvertFromUtf32(int.Parse(name, NumberStyles.HexNumber));
                                            buildedMessage.Add(new MessageItem { Text = HtmlEntity.DeEntitize(x), Type = MessageItemTypes.Text });
                                        }
                                        else
                                        {
                                            //full image ? 
                                        }

                                    }
                                }
                                else if (textNode.Name == "a")
                                {
                                    buildedMessage.Add(new MessageItem { Text = HtmlEntity.DeEntitize(textNode.InnerText), Data = HtmlEntity.DeEntitize(textNode.GetAttributeValue("href", "")), Type = MessageItemTypes.Link });

                                }
                            }

                            if (buildedMessage.Count > 0)
                            {
                                ChatMessage newMessage = new ChatMessage()
                                {
                                    MessageSource = messageSource,
                                    Message = buildedMessage,
                                    UserID = messageUsername,
                                    DisplayName = messageDisplayUsername,
                                    MessageType = MessageTypes.Text
                                };

                                if (messageSource == MessageSources.None)
                                {
                                    newMessage.MessageType = MessageTypes.Info;
                                }

                                newMessages.Add(newMessage);

                            }
                        }
                        #endregion
                        #region MessageImg & link

                        if (messageNode.LastChild.Name == "div")
                        {
                            var userImages = messageNode.LastChild.SelectNodes("a");
                            var userVideo = messageNode.LastChild.FirstChild?.SelectNodes("a");
                            bool isTrueVideoNode = userVideo != null && userVideo.Count > 0 && userVideo[0].FirstChild != null &&
                                                   userVideo[0].FirstChild.Name == "div";
                            var userManyImages = messageNode.LastChild?.LastChild?.SelectNodes("a");
                            var otherImages = messageNode.LastChild.SelectNodes("img");
                            var files = messageNode.LastChild.SelectNodes("div/div[1]/span/a");
                            if (userImages != null && userImages.Count > 0)
                            {
                                if (!String.IsNullOrEmpty(userImages[0].InnerText))
                                {
                                    #region Link

                                    var linkTitle = userImages[0].InnerText;

                                    var linkTitleNode = userImages[0].SelectSingleNode("div/table/tbody/tr/td[2]/h3");
                                    if (linkTitleNode != null)
                                        linkTitle = linkTitleNode.InnerText;

                                    newMessages.Add(new ChatMessage()
                                    {
                                        MessageData = HtmlEntity.DeEntitize(userImages[0].GetAttributeValue("href", "")),
                                        Message = { new MessageItem { Text = HtmlEntity.DeEntitize(linkTitle) } },
                                        MessageType = MessageTypes.Link,
                                        MessageSource = messageSource,
                                        UserID = messageUsername,
                                        DisplayName = messageDisplayUsername,
                                    });


                                    #endregion
                                }
                                else if (userImages[0].FirstChild.Name == "img")
                                {
                                    var imgSrc = HtmlEntity.DeEntitize(userImages[0].FirstChild.GetAttributeValue("src", ""));

                                    newMessages.Add(new ChatMessage()
                                    {
                                        MessageAditionalData = HtmlEntity.DeEntitize(userImages[0].GetAttributeValue("href", "")),
                                        MessageData = imgSrc,
                                        Message = { new MessageItem { Text = HtmlEntity.DeEntitize(userImages[0].FirstChild.GetAttributeValue("alt", "")) } },
                                        MessageType = MessageTypes.Photo,
                                        MessageSource = messageSource,
                                        UserID = messageUsername,
                                        DisplayName = messageDisplayUsername,
                                    });
                                }

                            }
                            else if (userVideo != null && userVideo.Count > 0 && isTrueVideoNode)
                            {
                                var videoLink = HtmlEntity.DeEntitize(userVideo[0].GetAttributeValue("href", ""));
                                videoLink = videoLink.Remove(0, videoLink.IndexOf('=') + 1);
                                videoLink = Uri.UnescapeDataString(videoLink);

                                var previewSrc = userVideo[0].LastChild?.FirstChild?.GetAttributeValue("src", "");

                                newMessages.Add(new ChatMessage()
                                {
                                    MessageAditionalData = videoLink,
                                    MessageData = HtmlEntity.DeEntitize(previewSrc),
                                    Message = { new MessageItem { Text = HtmlEntity.DeEntitize(userVideo[0].FirstChild.GetAttributeValue("alt", "")) } },
                                    MessageType = MessageTypes.Video,
                                    MessageSource = messageSource,
                                    UserID = messageUsername,
                                    DisplayName = messageDisplayUsername,
                                });
                            }
                            else if (userManyImages != null && userManyImages.Count > 0)
                            {
                                foreach (var image in userManyImages)
                                {
                                    if (image.FirstChild.Name == "img")
                                    {
                                        var imgSrc = HtmlEntity.DeEntitize(image.FirstChild.GetAttributeValue("src", ""));
                                        var textItem = new MessageItem { Text = HtmlEntity.DeEntitize(image.FirstChild.GetAttributeValue("alt", "")) };
                                        var chatMessage = new ChatMessage()
                                        {
                                            MessageAditionalData = HtmlEntity.DeEntitize(image.GetAttributeValue("href", "")),
                                            MessageData = imgSrc,
                                            Message = { textItem },
                                            MessageType = MessageTypes.Photo,
                                            MessageSource = messageSource,
                                            UserID = messageUsername,
                                            DisplayName = messageDisplayUsername
                                        };
                                        newMessages.Add(chatMessage);
                                    }
                                }
                            }
                            else if (otherImages != null)
                            {

                                foreach (var otherImage in otherImages)
                                {
                                    var imgSrc = HtmlEntity.DeEntitize(otherImage.GetAttributeValue("src", ""));

                                    newMessages.Add(new ChatMessage()
                                    {
                                        MessageData = imgSrc,
                                        Message = { new MessageItem { Text = HtmlEntity.DeEntitize(otherImage.GetAttributeValue("alt", "")) } },
                                        MessageType = MessageTypes.Img,
                                        MessageSource = messageSource,
                                        UserID = messageUsername,
                                        DisplayName = messageDisplayUsername,
                                    });

                                }

                            }
                            else if (files != null)
                            {
                                foreach (var fileNode in files)
                                {
                                    newMessages.Add(new ChatMessage()
                                    {
                                        MessageData = DataSource.requestUriString + HtmlEntity.DeEntitize(fileNode.GetAttributeValue("href", "")),
                                        Message = { new MessageItem { Text = HtmlEntity.DeEntitize(fileNode.InnerText) } },
                                        MessageType = MessageTypes.File,
                                        MessageSource = messageSource,
                                        UserID = messageUsername,
                                        DisplayName = messageDisplayUsername,
                                    });
                                }
                            }

                        }
                        #endregion
                    }
                }
                else if (!string.IsNullOrEmpty(messagePackNode.InnerText))
                {
                    newMessages.Add(new ChatMessage() { Message = { new MessageItem { Text = HtmlEntity.DeEntitize(messagePackNode.InnerText) } }, MessageSource = MessageSources.None, MessageType = MessageTypes.Info });
                }
                //   Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
            //this.Frame.Navigate(typeof(ChatList));

            AddMessages(newMessages, requestType);
            //
            // manage link to get new and old messages (at the end so NewestMessagesIndex is correct)
            //
            {
                var linkToOlderMessages = HtmlEntity.DeEntitize(htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"see_older\"]/a")?.GetAttributeValue("href", ""));

                if (string.IsNullOrEmpty(NewestMessagesLink) && !string.IsNullOrEmpty(linkToOlderMessages))
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
                        NewestMessagesIndex = Messages.Count - SendingMessages.Count - _sendingMessagesToRemove;
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

        //private MessageTypes GetMessageType(HtmlNode messageNode)
        //{
        //    throw new NotImplementedException();
        //}

        private void LogErrorParsing()
        {
            var newMessage = new ChatMessage()
            {
                MessageSource = MessageSources.None,
                MessageType = MessageTypes.Info,
                Message = { new MessageItem { Text = "Error parsing messages" } }
            };
            Messages.Add(newMessage);
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

        private void CheckPreviousMessageOwner(int selfIndex, ChatMessage message)
        {
            if (selfIndex == 0 && Messages.Count > 0)
            {
                Messages[0].PreviousMessageHasSameSender = Messages[0].UserID == message.UserID;
            }

            if (selfIndex > 0)
            {
                int otherIndex = selfIndex - 1;
                message.PreviousMessageHasSameSender = Messages[otherIndex].UserID == message.UserID;
            }
        }
        private void AddMessages(List<ChatMessage> newMessages, RequestType requestType)
        {
            bool didSomething = false;
            if (requestType == RequestType.GetOldMessages)
            {
                for (int i = newMessages.Count - 1; i >= 0; i--)
                {
                    didSomething = true;
                    CheckPreviousMessageOwner(0, newMessages[i]);
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
                    var sendingMessagesCount = SendingMessages.Count + _sendingMessagesToRemove;
                    if ((Messages.Count - sendingMessagesCount) <= NewestMessagesIndex + i)
                    {
                        CheckPreviousMessageOwner((Messages.Count - sendingMessagesCount), newMessages[i]);
                        Messages.Insert((Messages.Count - sendingMessagesCount), newMessages[i]);
                        didSomething = true;
                    }
                    else
                    {
                        if (Messages[NewestMessagesIndex + i].Equals(newMessages[i])) continue;


                        //clear lastest Messages


                        while ((Messages.Count - sendingMessagesCount) > NewestMessagesIndex + i)
                        {
                            Messages.RemoveAt((Messages.Count - sendingMessagesCount) - 1);
                        }
                        CheckPreviousMessageOwner((Messages.Count - sendingMessagesCount), newMessages[i]);
                        Messages.Insert((Messages.Count - sendingMessagesCount), newMessages[i]);
                        didSomething = true;
                    }
                }
            }

            if (didSomething)
            {
                RefreshInterval = 500;
                while (_sendingMessagesToRemove > 0)
                {
                    Messages.RemoveAt(Messages.Count - (SendingMessages.Count + _sendingMessagesToRemove));
                    _sendingMessagesToRemove--;
                }

            }
        }
    }
}
