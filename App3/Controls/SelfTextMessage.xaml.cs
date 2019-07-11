using App3.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour en savoir plus sur le modèle d'élément Contrôle utilisateur, consultez la page https://go.microsoft.com/fwlink/?LinkId=234236

namespace App3.Controls
{
    public sealed partial class SelfTextMessage : UserControl
    {
        public SelfTextMessage()
        {
            this.InitializeComponent();

           
            DataContextChanged += SelfTextMessage_DataContextChanged;
        }

        private void SelfTextMessage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            GenerateContent(DataContext,tb);
        }

        public static void GenerateContent(object DataContext,TextBlock tb)
        {
            var message = DataContext as ChatMessageViewModel;
            tb.Inlines.Clear();
            if (message != null)
            {
                foreach (var item in message.Message)
                {

                    if (item.Type == Data.MessageItemTypes.Text)
                    {
                        tb.Inlines.Add(new Run { Text = item.Text });
                    }
                    else
                    {
                        Hyperlink link = new Hyperlink { NavigateUri = new Uri(item.Data), Foreground = Application.Current.Resources["AccentButtonForeground"] as SolidColorBrush };
                        link.Inlines.Add(new Run { Text = item.Text });
                        tb.Inlines.Add(link);
                    }
                }

            }
        }
    }
}
