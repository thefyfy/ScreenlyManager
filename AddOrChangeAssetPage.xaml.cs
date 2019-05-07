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
        private Device DeviceToUpdate { get; set; }
        private Asset AssetToUpdate { get; set; }
        public List<Tuple<string, string>> MimeTypes { get; set; }
        private bool IsAnUpdate { get; set; }

        private Windows.ApplicationModel.Resources.ResourceLoader Loader;

        public AddOrChangeAssetPage()
        {
            this.Devices = null;
            this.AssetToUpdate = new Asset();

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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is List<Device>)
            {
                this.GridViewDevices.Visibility = Visibility.Visible;
                this.TextBlockDevice.Visibility = Visibility.Visible;
                this.Devices = e.Parameter as List<Device>;
                this.GridViewDevices.ItemsSource = this.Devices.Where(x => x.IsUp);
                this.IsAnUpdate = false;
            }
            else if (e.Parameter != null && e.Parameter is Tuple<Device, string>)
            {
                this.GridViewDevices.Visibility = Visibility.Collapsed;
                this.TextBlockDevice.Visibility = Visibility.Collapsed;
                this.IsAnUpdate = true;
                this.DeviceToUpdate = (e.Parameter as Tuple<Device, string>).Item1;
                string assetIdToUpdate = (e.Parameter as Tuple<Device, string>).Item2;

                this.AssetToUpdate = await this.DeviceToUpdate.GetAssetAsync(assetIdToUpdate);

                this.TextBoxName.Text = this.AssetToUpdate.Name;
                this.TextBoxUrl.Text = this.AssetToUpdate.ReadableUri;
                this.ComboBoxAssetType.SelectedItem = this.MimeTypes.Where(x => x.Item1.Equals(this.AssetToUpdate.Mimetype)).FirstOrDefault();
                this.DatePickerStart.Date = this.AssetToUpdate.StartDate;
                this.TimePickerStart.Time = this.AssetToUpdate.StartDate.TimeOfDay;
                this.DatePickerEnd.Date = this.AssetToUpdate.EndDate;
                this.TimePickerEnd.Time = this.AssetToUpdate.EndDate.TimeOfDay;
                this.TextBoxDuration.Text = this.AssetToUpdate.Duration;
                this.ToggleSwitchEnable.IsOn = this.AssetToUpdate.IsEnabled.Equals(1) ? true : false;
                this.ToggleSwitchDisableCache.IsOn = this.AssetToUpdate.NoCache.Equals(1) ? true : false;

                this.TextBlockTitle.Text = string.Format(this.Loader.GetString("EditAsset"), this.AssetToUpdate.Name, this.DeviceToUpdate.Name);
            }
            else if (e.Parameter != null && e.Parameter is Tuple<List<Device>, Device, string>)
            {
                this.GridViewDevices.Visibility = Visibility.Visible;
                this.TextBlockDevice.Visibility = Visibility.Visible;
                this.IsAnUpdate = false;
                this.Devices = (e.Parameter as Tuple<List<Device>, Device, string>).Item1;
                this.GridViewDevices.ItemsSource = this.Devices.Where(x => x.IsUp);
                Device deviceToCopy = (e.Parameter as Tuple<List<Device>, Device, string>).Item2;
                string assetIdToUpdate = (e.Parameter as Tuple<List<Device>, Device, string>).Item3;

                this.AssetToUpdate = await deviceToCopy.GetAssetAsync(assetIdToUpdate);

                this.TextBoxName.Text = this.AssetToUpdate.Name;
                this.TextBoxUrl.Text = this.AssetToUpdate.ReadableUri;
                this.ComboBoxAssetType.SelectedItem = this.MimeTypes.Where(x => x.Item1.Equals(this.AssetToUpdate.Mimetype)).FirstOrDefault();
                this.DatePickerStart.Date = this.AssetToUpdate.StartDate;
                this.TimePickerStart.Time = this.AssetToUpdate.StartDate.TimeOfDay;
                this.DatePickerEnd.Date = this.AssetToUpdate.EndDate;
                this.TimePickerEnd.Time = this.AssetToUpdate.EndDate.TimeOfDay;
                this.TextBoxDuration.Text = this.AssetToUpdate.Duration;
                this.ToggleSwitchEnable.IsOn = this.AssetToUpdate.IsEnabled.Equals(1) ? true : false;
                this.ToggleSwitchDisableCache.IsOn = this.AssetToUpdate.NoCache.Equals(1) ? true : false;

                this.TextBlockTitle.Text = string.Format(this.Loader.GetString("DuplicateAsset"), this.AssetToUpdate.Name);
            }
        }

        private async void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!this.TextBoxName.Text.Equals(string.Empty) && !this.TextBoxUrl.Text.Equals(string.Empty) && (this.DatePickerStart.Date.Date + this.TimePickerStart.Time) < (this.DatePickerEnd.Date.Date + this.TimePickerEnd.Time) && !this.TextBoxDuration.Text.Equals(string.Empty) && this.ComboBoxAssetType.SelectedValue != null)
            {
                Asset a = this.AssetToUpdate;
                a.Name = this.TextBoxName.Text;
                a.Uri = this.TextBoxUrl.Text;
                a.StartDate = (this.DatePickerStart.Date.Date + this.TimePickerStart.Time).ToUniversalTime();
                a.EndDate = (this.DatePickerEnd.Date.Date + this.TimePickerEnd.Time).ToUniversalTime();
                a.Duration = this.TextBoxDuration.Text;
                a.IsEnabled = this.ToggleSwitchEnable.IsOn ? 1 : 0;
                a.NoCache = this.ToggleSwitchDisableCache.IsOn ? 1 : 0;
                a.Mimetype = this.ComboBoxAssetType.SelectedValue as string;
                a.SkipAssetCheck = 1;
                a.IsProcessing = 0;

                var dialog = new MessageDialog(this.Loader.GetString("ConfirmationAddAsset"));

                if (this.IsAnUpdate)
                {
                    await this.DeviceToUpdate.UpdateAssetAsync(a);
                    dialog.Content = this.Loader.GetString("ConfirmationUpdateAsset");
                }
                else
                {
                    if (this.GridViewDevices.SelectedItems.Count > 0)
                    {
                        var devicesSelected = this.GridViewDevices.SelectedItems.ToList();
                        foreach (var device in devicesSelected)
                            await (device as Device).CreateAsset(a);
                    }
                    else
                    {
                        var dialogError = new MessageDialog(this.Loader.GetString("RequiredAssetFileds"));
                        dialogError.Commands.Add(new UICommand("Ok") { Id = 0 });
                        dialogError.DefaultCommandIndex = 0;
                        await dialogError.ShowAsync();
                    }
                }
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

        private void DatePickerStart_DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            this.DatePickerEnd.Date = this.DatePickerStart.Date.AddDays(1);
        }
    }
}
