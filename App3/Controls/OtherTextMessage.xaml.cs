using App3.ViewModels;
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

namespace App3.Controls
{
    public sealed partial class OtherTextMessage : UserControl
    {
        public OtherTextMessage()
        {
            this.InitializeComponent();

            DataContextChanged += OtherTextMessage_DataContextChanged;
        }
        public Boolean IsGroup
        {
            get { return (Boolean)this.GetValue(IsGroupProperty); }
            set { this.SetValue(IsGroupProperty, value); }
        }
        public static readonly DependencyProperty IsGroupProperty = DependencyProperty.Register(
          "IsGroup", typeof(Boolean), typeof(OtherTextMessage), new PropertyMetadata(false, new PropertyChangedCallback(OnFirstPropertyChanged)));
        private static void OnFirstPropertyChanged(
        DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = sender as OtherTextMessage;
            ctrl.SenderName.Visibility = ctrl.IsGroup ? Visibility.Collapsed : Visibility.Visible;
        }
        private void OtherTextMessage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            SelfTextMessage.GenerateContent(DataContext, tb);
        }
    }
}
