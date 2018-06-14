using System;
using System.Collections.Generic;
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
                    new Uri(App.requestUri, _action));
            HttpRequestMessage.Content = new HttpStringContent(_formEncoded += "&body=" + Uri.EscapeDataString(message), UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            HttpRequestMessage.Headers.Add("User-Agent", App.CustomUserAgent);

            try
            {
                //Send the GET request
                httpResponse = await httpClient.SendRequestAsync(HttpRequestMessage);
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(httpResponseBody);
               callback.RefreshConversation(htmlDoc);
            }
            catch (Exception ex)
            {
                httpResponseBody = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }
    }
}
