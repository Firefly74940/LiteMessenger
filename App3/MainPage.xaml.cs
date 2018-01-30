using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using App1;
using HtmlAgilityPack;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App3
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public class ChatHeader
    {
        public string Name { get; set; }
        public string Href { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            
            this.InitializeComponent();
            ShowStatusBar();
            UserAgent.SetUserAgent(App.CustomUserAgent);
            App.localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (App.localSettings.Values.ContainsKey("isLogin"))
                App._isLogedIn = (bool) App.localSettings.Values["isLogin"];

            if(!App._isLogedIn)
            {
                HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get,
                    new Uri("https://mbasic.facebook.com/messages/"));
                
            }
           
          
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void ShowStatusBar()
        {
            // turn on SystemTray for mobile
            // don't forget to add a Reference to Windows Mobile Extensions For The UWP
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                var statusbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusbar.ShowAsync();
                //statusbar.BackgroundColor = Windows.UI.Colors.Transparent;
                statusbar.BackgroundColor = Windows.UI.Colors.Black;
                statusbar.BackgroundOpacity = 1;
                statusbar.ForegroundColor = Windows.UI.Colors.White;
            }
        }

        public static async Task<HtmlDocument> GetHtmlDoc(string relativeUri)
        {
            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
            string httpResponseBody = "";

            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
            HttpRequestMessage HttpRequestMessage =
                new HttpRequestMessage(HttpMethod.Get,
                    new Uri(App.requestUri,relativeUri));

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
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(httpResponseBody);
            return htmlDoc;
        }
        //private async void AppBar()
        //{


        //   var htmlDoc = await GetHtmlDoc("/messages/");

        //    {
        //        var profileNode = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"header\"]/div/a[2]");
        //        var namelink = profileNode.GetAttributeValue("href", "");
        //        int end = namelink.IndexOf('?');
        //        Username= namelink.Substring(1, end - 1);
        //    }
        //    //var node = htmlDoc.DocumentNode.SelectSingleNode("//*[@id=\"root\"]/div[1]/div[2]/div[1]/table[1]/tbody");//*[@id="root"]/div[1]/div[2]/div[1]/table[1]/tbody/tr/td/div/h3[1]/a
        //    var nodes = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"root\"]/div[1]/div[2]/div[1]/table");//*[@id="root"]/div[1]/div[2]/div[1]/table[1]/tbody/tr/td/div/h3[1]/a
        //    foreach (var node in nodes)
        //    {
        //        var NameNode=node.SelectSingleNode("tr/td/div/h3[1]/a");
        //        Names.Add(new ChatHeader(){Name = NameNode.InnerText ,Href = NameNode.GetAttributeValue("href","") });
        //    }
            
        //}

        //private void button_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Frame.Navigate(typeof(ChatList));

        //}
    }
}
