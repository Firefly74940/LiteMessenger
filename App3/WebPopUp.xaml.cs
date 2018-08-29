using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    }
}