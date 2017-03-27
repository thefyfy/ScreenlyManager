using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace ScreenlyManager
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// Back button : http://www.wintellect.com/devcenter/jprosise/handling-the-back-button-in-windows-10-uwp-apps
    /// </summary>
    public sealed partial class AddOrChangeAssetPage : Page
    {
        private List<Device> Devices { get; set; }

        public AddOrChangeAssetPage()
        {
            this.Devices = null;

            this.InitializeComponent();

            this.DatePickerEnd.Date = DateTime.Now.AddDays(1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is List<Device>)
            {
                this.Devices = e.Parameter as List<Device>;
                this.GridViewDevices.ItemsSource = this.Devices.Where(x => x.IsUp);
            }
        }

        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage), null);
        }
    }
}
