using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Popups;
using Windows.UI.Xaml;
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
        private Device CurrentRightClickDevice;
        private string PathDbFile;
        private Windows.ApplicationModel.Resources.ResourceLoader Loader;

        public MainPage()
        {
            this.Loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            this.InitializeComponent();

            // AppData folder access
            var localFolder = ApplicationData.Current.LocalFolder;
            this.PathDbFile = localFolder.Path + Path.DirectorySeparatorChar + DB_FILE;

            // Load user config or create db file (if not exists) (containing IPs and informations about RPi)
            if (!File.Exists(this.PathDbFile))
            {
                var file = File.Create(this.PathDbFile);
                file.Dispose();
            }
            else
            {
                this.ProgressRingLoadingDevice.IsActive = true;
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
            if (this.Devices != null)
            {
                foreach (var device in this.Devices)
                {
                    if (await device.IsReachable())
                        await device.GetAssetsAsync();
                }
                this.AppBarButtonAddAsset.IsEnabled = true;
                this.ListViewDevice.ItemsSource = this.Devices;
            }
            this.ProgressRingLoadingDevice.IsActive = false;
        }

        /// <summary>
        /// Load assets for current device trought API and refresh assets view
        /// </summary>
        private async void RefreshAssetsForCurrentDeviceAsync()
        {
            await this.CurrentDevice.GetAssetsAsync();

            this.ListViewActiveAssets.ItemsSource = this.CurrentDevice.ActiveAssets;
            this.ListViewInactiveAssets.ItemsSource = this.CurrentDevice.InactiveAssets;
            this.TextBlockActiveAsset.Text = $"{ this.Loader.GetString("ActiveAssets") } ({this.ListViewActiveAssets.Items.Count})";
            this.TextBlockInactiveAsset.Text = $"{ this.Loader.GetString("InactiveAssets") } ({this.ListViewInactiveAssets.Items.Count})";
        }

        /// <summary>
        /// Serialize devices list to file
        /// </summary>
        /// <param name="file">StorageFile item where JSON will write</param>
        /// <returns>True if export was ok, false otherwise</returns>
        private async Task<bool> SaveFileConfiguration(StorageFile file)
        {
            var dbContent = JsonConvert.SerializeObject(this.Devices);

            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteTextAsync(file, dbContent);
                Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    var dialog = new MessageDialog(string.Format(this.Loader.GetString("ErrorCannotSaveFile"), file.Name), this.Loader.GetString("Error"));
                    dialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                    dialog.DefaultCommandIndex = 0;
                    var result = await dialog.ShowAsync();
                    return false;
                }
                else
                    return true;
            }
            else
                return false;
        }

        #region View's events

        /// <summary>
        /// Refresh button click in left command bar. Used to refresh devices list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonRefreshDevice_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.ProgressRingLoadingDevice.IsActive = true;
            this.AppBarButtonAddAsset.IsEnabled = false;
            this.LoadConfigurationAsync(this.PathDbFile);
        }
        
        /// <summary>
        /// Refresh button click in main command bar. Used to refresh assets for current device.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonRefreshAssets_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (this.CurrentDevice != null)
                this.RefreshAssetsForCurrentDeviceAsync();
        }

        /// <summary>
        /// Event for selection in device list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ListViewDeviceItem_Click(object sender, ItemClickEventArgs e)
        {
            var deviceCliked = e.ClickedItem as Device;
            this.TextBlockCommandBarMain.Text = $"{ this.Loader.GetString("ScheduleOverview") } - {deviceCliked.Name}";
            this.CurrentDevice = deviceCliked;

            if(this.CurrentDevice.IsUp)
                this.RefreshAssetsForCurrentDeviceAsync();
            else
            {
                this.ListViewActiveAssets.ItemsSource = null;
                this.ListViewInactiveAssets.ItemsSource = null;
                this.TextBlockActiveAsset.Text = this.Loader.GetString("ActiveAssets");
                this.TextBlockInactiveAsset.Text = this.Loader.GetString("InactiveAssets");

                var dialog = new MessageDialog(this.Loader.GetString("DeviceDown"));
                var resultDialog = dialog.ShowAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));
                resultDialog.Cancel();
            }
        }

        /// <summary>
        /// Open asset link with default browser if it's a web link
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonPreview_Click(object sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(((sender as Button).Tag.ToString()), System.UriKind.Absolute, out Uri uriResult))
                await Windows.System.Launcher.LaunchUriAsync(uriResult);
        }

        /// <summary>
        /// Duplicate selected asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDuplicate_Click(object sender, RoutedEventArgs e)
        {
            var assetId = (sender as Button).Tag.ToString();
            this.Frame.Navigate(typeof(AddOrChangeAssetPage), new Tuple<List<Device>, Device, string>(this.Devices, this.CurrentDevice, assetId));
        }

        /// <summary>
        /// Edit selected asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonEdit_Click(object sender, RoutedEventArgs e)
        {
            var assetId = (sender as Button).Tag.ToString();
            this.Frame.Navigate(typeof(AddOrChangeAssetPage), new Tuple<Device, string>(this.CurrentDevice, assetId));
        }

        /// <summary>
        /// Event to remove an asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonRemove_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var dialog = new MessageDialog(this.Loader.GetString("ConfirmDelete"), this.Loader.GetString("Delete"));
            dialog.Commands.Add(new UICommand(this.Loader.GetString("Yes")) { Id = 0 });
            dialog.Commands.Add(new UICommand(this.Loader.GetString("No")) { Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var result = await dialog.ShowAsync();
            var assetId = (sender as Button).Tag.ToString();
            if((int)result.Id == 0)
                await Task.Run(() => this.CurrentDevice.RemoveAssetAsync(assetId));

            this.RefreshAssetsForCurrentDeviceAsync();
        }

        /// <summary>
        /// Enable/disable an asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToggleSwitchEnable_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var newState = (sender as ToggleSwitch).IsOn;

            Asset currentAsset = (sender as ToggleSwitch).DataContext as Asset;
            if (currentAsset != null)
            {
                // Don't know why, but toggled event is fired when dragging item in listview (and IsOn value's keep unchanged!), so I found this work around...
                if ((currentAsset.IsEnabled.Equals("1") ? true : false) != newState)
                {
                    currentAsset.StartDate = currentAsset.StartDate.ToUniversalTime();
                    currentAsset.EndDate = currentAsset.EndDate.ToUniversalTime();
                    currentAsset.IsEnabled = (sender as ToggleSwitch).IsOn ? "1" : "0";
                    await this.CurrentDevice.UpdateAssetAsync(currentAsset);
                    this.RefreshAssetsForCurrentDeviceAsync();
                }
            }
        }

        /// <summary>
        /// Export devices list to JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AppBarButtonExportConf_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var dbContent = JsonConvert.SerializeObject(this.Devices);

            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "db";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                bool status = await this.SaveFileConfiguration(file);
                var dialog = new MessageDialog(this.Loader.GetString("ConfigurationExported") + file.Name, this.Loader.GetString("BackupCreated"));
                if (!status)
                {
                    dialog.Content = string.Format(this.Loader.GetString("ErrorCannotSaveFile"), file.Name);
                    dialog.Title = this.Loader.GetString("Error");
                }
                dialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                dialog.DefaultCommandIndex = 0;
                var result = await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// Import devices from JSON file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AppBarButtonImportConf_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            openPicker.FileTypeFilter.Add(".json");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                string content = await FileIO.ReadTextAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                this.ProgressRingLoadingDevice.IsActive = true;

                try
                {
                    this.Devices = JsonConvert.DeserializeObject<List<Device>>(content);
                    StorageFile dbFile = await StorageFile.GetFileFromPathAsync(this.PathDbFile);
                    await FileIO.WriteTextAsync(dbFile, content);

                    this.LoadConfigurationAsync(this.PathDbFile);

                    var dialog = new MessageDialog(this.Loader.GetString("ConfigurationImported") + file.Name, this.Loader.GetString("ImportCompleted"));
                    dialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                    dialog.DefaultCommandIndex = 0;
                    var result = await dialog.ShowAsync();
                }
                catch(Exception ex)
                {
                    var dialogError = new MessageDialog(string.Format(this.Loader.GetString("InvalidFile"), file.Name) + Environment.NewLine + ex.Message);
                    dialogError.Commands.Add(new UICommand("Ok") { Id = 0 });
                    dialogError.DefaultCommandIndex = 0;
                    await dialogError.ShowAsync();
                    this.ProgressRingLoadingDevice.IsActive = false;
                }
            }
        }

        /// <summary>
        /// Event fired when we change ListView active assets order
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void ListViewActiveAssets_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var assetsInListView = (sender as ListView).Items.OfType<Asset>().ToList();
            await this.CurrentDevice.UpdateOrderAssetsAsync(string.Join(",", assetsInListView.Select(x => x.AssetId)));
        }

        /// <summary>
        /// Go to creation of new asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonAddAsset_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AddOrChangeAssetPage), this.Devices);
        }

        /// <summary>
        /// Go to creation of new device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonAddDevice_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AddOrChangeDevicePage), null);
        }

        /// <summary>
        /// Right click on device item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListViewDevice_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            ListView listView = sender as ListView;
            this.MenuFlyoutDevice.ShowAt(listView, e.GetPosition(listView));
            var device = (e.OriginalSource as FrameworkElement).DataContext;
            if (device == null)
                this.MenuFlyoutDevice.Hide();
            else
                this.CurrentRightClickDevice = device as Device;
        }

        /// <summary>
        /// Remove item from devices list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MenuFlyoutItemRemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            this.ProgressRingLoadingDevice.IsActive = true;
            this.Devices.Remove(this.CurrentRightClickDevice);

            StorageFile file = await StorageFile.GetFileFromPathAsync(this.PathDbFile);
            await this.SaveFileConfiguration(file);
            
            this.CurrentRightClickDevice = null;
            this.LoadConfigurationAsync(this.PathDbFile);
        }

        /// <summary>
        /// Edit item from devices list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuFlyoutItemEditDevice_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AddOrChangeDevicePage), this.CurrentRightClickDevice);
        }

        /// <summary>
        /// Open the configuration device link
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MenuFlyoutItemOpenDevice_Click(object sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(this.CurrentRightClickDevice.HttpLink, System.UriKind.Absolute, out Uri uriResult))
                await Windows.System.Launcher.LaunchUriAsync(uriResult);
        }

        #endregion
    }
}
