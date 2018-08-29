using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour en savoir plus sur le modèle d'élément Contrôle utilisateur, consultez la page https://go.microsoft.com/fwlink/?LinkId=234236

namespace App3
{
    public sealed partial class WebPopUp : UserControl
    {
        public WebPopUp()
        {
            this.InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            WebView.Navigate(new Uri("about:blank"));
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            WebView.Refresh();
        }

        public void RequestPage(string url)
        {
            this.Visibility = Visibility.Visible;
            WebView.Navigate(new Uri(url));
        }

        private async void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            if (args.Uri.AbsolutePath.EndsWith(".jpg"))
            {
                // string html = await WebView.InvokeScriptAsync("eval", new [] { @"document.body.setAttribute(""style"",""margin: 0px; background: #0e0e0e);" });
                string html = await WebView.InvokeScriptAsync("eval",
                    new[] {@"document.body.setAttribute(""style"",""margin: 0px; background: #0e0e0e;"");"});
                var text = html;
                //WebView.InvokeScriptAsync("eval", new string[] { "document.body.zoom = (window.innerWidth * 100 / document.body.clientWidth) + '%'" });
                WebView.InvokeScriptAsync("eval", new[]
                {
                    @"
             var images = document.getElementsByTagName('img'); 
             for (var i=0;i<images.length;i++) 
             { 
                 images[i].setAttribute(""style"",""width:""+window.innerWidth+""px""); 
             }    
"
                });

                Save.Visibility = Visibility.Visible;
            }
            else
            {
                Save.Visibility = Visibility.Collapsed;
            }

            Saved.Visibility = Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Downloader(WebView.Source.AbsoluteUri, WebView.Source.AbsolutePath, ".jpg");
        }

        public async void Downloader(string requestUrl, string filename, string fileType)
        {
            try
            {
                var downloader = new BackgroundDownloader();
                var regex = new Regex(@"[\\|/\:\*\?""<>\\|]");
                var result_filename = regex.Replace(filename, " ").Replace(";", "");
                result_filename= filename.Substring(filename.LastIndexOf('/')+1, filename.LastIndexOf('.')- filename.LastIndexOf('/')-1);
                FolderPicker fo = new FolderPicker();
                fo.SuggestedStartLocation = PickerLocationId.Downloads;
                fo.FileTypeFilter.Add(fileType);
                StorageFolder folder = await fo.PickSingleFolderAsync();
                var filePart = await folder.CreateFileAsync(result_filename + fileType, CreationCollisionOption.GenerateUniqueName);
                DownloadOperation download = downloader.CreateDownload(new Uri(requestUrl), filePart);
                await StartDownloadAsync(download);
            }
            catch (Exception)
            {
                return;
            }

        }
        private async Task StartDownloadAsync(DownloadOperation obj)
        {
            var process = new Progress<DownloadOperation>(ProgressCallBack);
            await obj.StartAsync().AsTask(process);
        }
        public void ProgressCallBack(DownloadOperation obj)
        {
            this.Visibility = Visibility.Visible;
            var Percentage = ((double)obj.Progress.BytesReceived / obj.Progress.TotalBytesToReceive) * 100;

            if (Percentage >= 100)
            {
                Saved.Visibility = Visibility.Visible;
            }
        }
    }
}