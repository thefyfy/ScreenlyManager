using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        public List<Tuple<string, string>> MimeTypes { get; set; }

        private Windows.ApplicationModel.Resources.ResourceLoader Loader;

        public AddOrChangeAssetPage()
        {
            this.Devices = null;

            this.Loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            this.MimeTypes = new List<Tuple<string, string>>()
            {
                new Tuple<string, string>("webpage", this.Loader.GetString("Webpage")),
                new Tuple<string, string>("image", this.Loader.GetString("Image")),
                new Tuple<string, string>("video", this.Loader.GetString("Video"))
            };

            this.InitializeComponent();

            this.DatePickerEnd.Date = DateTime.Now.AddDays(1);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is List<Device>)
            {
                this.GridViewDevices.Visibility = Visibility.Collapsed;
                this.TextBlockDevice.Visibility = Visibility.Collapsed;
                this.Devices = e.Parameter as List<Device>;
                this.GridViewDevices.ItemsSource = this.Devices.Where(x => x.IsUp);
            }
            else if (e.Parameter != null && e.Parameter is Tuple<Device, Asset>)
            {
                this.GridViewDevices.Visibility = Visibility.Visible;
                this.TextBlockDevice.Visibility = Visibility.Visible;
                
            }
        }

        private async void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if(!this.TextBoxName.Text.Equals(string.Empty) && !this.TextBoxUrl.Text.Equals(string.Empty) && this.DatePickerStart.Date < this.DatePickerEnd.Date && !this.TextBoxDuration.Text.Equals(string.Empty) && this.GridViewDevices.SelectedItems.Count > 0 && this.ComboBoxAssetType.SelectedValue != null)
            {
                Asset a = new Asset();
                a.Name = this.TextBoxName.Text;
                a.Uri = this.TextBoxUrl.Text;
                a.Mimetype = this.ComboBoxAssetType.SelectedValue as string;
                a.StartDate = (this.DatePickerStart.Date.Date + this.TimePickerStart.Time).ToUniversalTime();
                a.EndDate = (this.DatePickerEnd.Date.Date + this.TimePickerEnd.Time).ToUniversalTime();
                a.Duration = this.TextBoxDuration.Text;
                a.IsEnabled = this.ToggleSwitchEnable.IsOn ? "1" : "0";
                a.NoCache = this.ToggleSwitchDisableCache.IsOn ? "1" : "0";
                a.Mimetype = this.ComboBoxAssetType.SelectedValue as string;

                var devicesSelected = this.GridViewDevices.SelectedItems.ToList();
                foreach (var device in devicesSelected)
                    await (device as Device).CreateAsset(a);

                var dialog = new MessageDialog(this.Loader.GetString("ConfirmationAddAsset"));
                dialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                dialog.DefaultCommandIndex = 0;
                var result = await dialog.ShowAsync();

                this.Frame.Navigate(typeof(MainPage), null);
            }
            else
            {
                var dialogError = new MessageDialog(this.Loader.GetString("RequiredAssetFileds"));
                dialogError.Commands.Add(new UICommand("Ok") { Id = 0 });
                dialogError.DefaultCommandIndex = 0;
                await dialogError.ShowAsync();

                this.TextBoxName.Focus(FocusState.Programmatic);
                this.TextBoxName.SelectAll();
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage), null);
        }

        private void ComboBoxAssetType_Loaded(object sender, RoutedEventArgs e)
        {
            this.ComboBoxAssetType.SelectedIndex = 0;
        }
    }
}
