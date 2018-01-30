using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
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
    public class ChatMessage
    {
        public string DisplayName { get; set; }
        public string UserID { get; set; }
        public string Message { get; set; }
        public override string ToString()
        {
            return DisplayName + ": " + Message;
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
        private async void RefreshConversation(ChatHeader header)
        {
            var htmlDoc = await MainPage.GetHtmlDoc(header.Href);

            GetSubmitForm(htmlDoc);
            var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"messageGroup\"]/div[2]/div");
            foreach (var node in nodes)
            {
                var MessageUsername = "";
                var MessageDisplayUsername = "";
                var NameNode = node.SelectSingleNode("div[1]/a");
                if (NameNode != null)
                {
                    var namelink = NameNode.GetAttributeValue("href", "");
                    int end = namelink.IndexOf('?');
                    MessageUsername = namelink.Substring(1, end - 1);
                    MessageDisplayUsername = HtmlEntity.DeEntitize(NameNode.InnerText);
                }
                var TextNodes = node.SelectNodes("div[1]/div");
                if (TextNodes != null)
                {
                    foreach (var textNode in TextNodes)
                    {
                        listView.Items.Add(new ChatMessage() { Message = HtmlEntity.DeEntitize(textNode.InnerText), UserID = MessageUsername, DisplayName = MessageDisplayUsername });
                    }
                }
                else if (!string.IsNullOrEmpty(node.InnerText))
                {
                    listView.Items.Add(new ChatMessage() { Message = HtmlEntity.DeEntitize(node.InnerText), UserID = MessageUsername, DisplayName = MessageDisplayUsername });
                }
                //   Names.Add(new ChatHeader() { Name = NameNode.InnerText, Href = NameNode.GetAttributeValue("href", "") });
            }
            //this.Frame.Navigate(typeof(ChatList));
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            listView.Items.Add(new ChatMessage() { Message = NewMessageBox.Text, UserID = "sidou", DisplayName = "sidou" });


            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Post,
                    new Uri(App.requestUri, _action));
            HttpRequestMessage.Content=new HttpStringContent(_formEncoded += "&body="+ Uri.EscapeDataString(NewMessageBox.Text),UnicodeEncoding.Utf8,"application/x-www-form-urlencoded");
            HttpRequestMessage.Headers.Add("User-Agent", App.CustomUserAgent);
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
            //var htmlDoc = new HtmlDocument();
            //htmlDoc.LoadHtml(httpResponseBody);
          





            NewMessageBox.Text = "";
        }

        private void NewMessageBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Send.IsEnabled= !string.IsNullOrEmpty(NewMessageBox.Text);
        }
    }


}
