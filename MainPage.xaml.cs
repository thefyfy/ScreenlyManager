using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScreenlyManager
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string DB_FILE = "db.json";

        private List<Device> Devices;
        private Device CurrentDevice;
        private string PathDbFile;

        public MainPage()
        {
            this.InitializeComponent();

            // AppData folder access
            var localFolder = ApplicationData.Current.LocalFolder;

            // Load user config or create db file (if not exists) (containing IPs and informations about RPi)
            if (!File.Exists(localFolder.Path + Path.DirectorySeparatorChar + DB_FILE))
                File.Create(localFolder.Path + Path.DirectorySeparatorChar + DB_FILE);
            else
            {
                this.PathDbFile = localFolder.Path + Path.DirectorySeparatorChar + DB_FILE;
                this.LoadConfigurationAsync(this.PathDbFile);
            }
        }

        /// <summary>
        /// Load devices from JSON file and refresh list in ListView
        /// </summary>
        /// <param name="path">Path to "db.json" file (default storage : appdata/local)</param>
        public async void LoadConfigurationAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            string json = await FileIO.ReadTextAsync(file);
            this.Devices = JsonConvert.DeserializeObject<List<Device>>(json);
            // Need to get list of assets for each device
            foreach (var device in this.Devices)
                await device.GetAssetsAsync();

            this.ListViewDevice.ItemsSource = this.Devices;
        }

        /// <summary>
        /// Load assets for device in param trought and refresh view
        /// </summary>
        private async void RefreshAssetsForCurrentDeviceAsync()
        {
            await this.CurrentDevice.GetAssetsAsync();

            this.ListViewActiveAssets.ItemsSource = this.CurrentDevice.Assets.FindAll(x => x.IsActive);
            this.ListViewInactiveAssets.ItemsSource = this.CurrentDevice.Assets.FindAll(x => !x.IsActive);
            this.TextBlockActiveAsset.Text = $"Active assets ({this.ListViewActiveAssets.Items.Count})";
            this.TextBlockInactiveAsset.Text = $"Inactive assets ({this.ListViewInactiveAssets.Items.Count})";
        }

        #region View's events

        /// <summary>
        /// Refresh button click in left command bar. Used to refresh devices list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonRefreshDeviceClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.LoadConfigurationAsync(this.PathDbFile);
        }
        
        /// <summary>
        /// Refresh button click in main command bar. Used to refresh assets for current device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonRefreshAssetsClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (this.CurrentDevice != null)
                this.RefreshAssetsForCurrentDeviceAsync();
        }

        /// <summary>
        /// Event for selection in device list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewDeviceItemClick(object sender, ItemClickEventArgs e)
        {
            var deviceCliked = (Device)e.ClickedItem;
            this.TextBlockCommandBarMain.Text = $"Schedule Overview - {deviceCliked.Name}";
            this.CurrentDevice = deviceCliked;

            this.RefreshAssetsForCurrentDeviceAsync();
        }

        /// <summary>
        /// Open asset link with default browser if it'a a web link
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonPreviewClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Uri.TryCreate((((Button)sender).Tag.ToString()), System.UriKind.Absolute, out Uri uriResult))
                await Windows.System.Launcher.LaunchUriAsync(uriResult);
        }
        
        private async void ToggleSwitchEnableToggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Asset currentAsset = ((ToggleSwitch)sender).DataContext as Asset;
            if (currentAsset != null)
            {
                currentAsset.IsEnabled = ((ToggleSwitch)sender).IsOn ? "1" : "0";
                await this.CurrentDevice.UpdateAssetAsync(currentAsset);
                this.RefreshAssetsForCurrentDeviceAsync();
            }
        }

        /// <summary>
        /// Event to remove an asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonRemoveClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var dialog = new MessageDialog("Are you sure to delete this item?", "Delete");
            dialog.Commands.Add(new UICommand("Yes") { Id = 0 });
            dialog.Commands.Add(new UICommand("No") { Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var result = await dialog.ShowAsync();
            var assetId = ((Button)sender).Tag.ToString();
            if((int)result.Id == 0)
                await Task.Run(() => this.CurrentDevice.RemoveAssetAsync(assetId));

            this.RefreshAssetsForCurrentDeviceAsync();
        }

        #endregion
    }
}
