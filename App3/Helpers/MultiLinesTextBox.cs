using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace App3.Helpers
{
    class MultiLinesTextBox : TextBox
    {
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                bool ctrldown =
                    Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) ||
                    Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ||
                    AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile";
                if (ctrldown)
                {
                    base.OnKeyDown(e);
                }
                else
                {
                    //e.Handled = true;
                }
            }
            else
            {
                base.OnKeyDown(e);
            }
        }
    }
}
