using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
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
            this.ProgressRingLoadingDevice.IsActive = true;
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
            {
                if(await device.IsReachable())
                    await device.GetAssetsAsync();
            }

            this.ProgressRingLoadingDevice.IsActive = false;
            this.ListViewDevice.ItemsSource = this.Devices;
        }

        /// <summary>
        /// Load assets for current device trought API and refresh assets view
        /// </summary>
        private async void RefreshAssetsForCurrentDeviceAsync()
        {
            await this.CurrentDevice.GetAssetsAsync();

            this.ListViewActiveAssets.ItemsSource = this.CurrentDevice.ActiveAssets;
            this.ListViewInactiveAssets.ItemsSource = this.CurrentDevice.InactiveAssets;
            this.TextBlockActiveAsset.Text = $"Active assets ({this.ListViewActiveAssets.Items.Count})";
            this.TextBlockInactiveAsset.Text = $"Inactive assets ({this.ListViewInactiveAssets.Items.Count})";
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
                    var dialog = new MessageDialog($"Oops... File {file.Name} couldn't be saved.", "Error");
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
            var deviceCliked = (Device)e.ClickedItem;
            this.TextBlockCommandBarMain.Text = $"Schedule Overview - {deviceCliked.Name}";
            this.CurrentDevice = deviceCliked;

            if(this.CurrentDevice.IsUp)
                this.RefreshAssetsForCurrentDeviceAsync();
            else
            {
                this.ListViewActiveAssets.ItemsSource = null;
                this.ListViewInactiveAssets.ItemsSource = null;
                this.TextBlockActiveAsset.Text = "Active assets";
                this.TextBlockInactiveAsset.Text = "Inactive assets";

                var dialog = new MessageDialog("Oops... This device is down");
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
        private async void ButtonPreview_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Uri.TryCreate((((Button)sender).Tag.ToString()), System.UriKind.Absolute, out Uri uriResult))
                await Windows.System.Launcher.LaunchUriAsync(uriResult);
        }
        
        /// <summary>
        /// Enable/disable an asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ToggleSwitchEnable_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var newState = ((ToggleSwitch)sender).IsOn;

            Asset currentAsset = ((ToggleSwitch)sender).DataContext as Asset;
            if (currentAsset != null)
            {
                // Don't know why, but toggled event is fired when dragging item in listview (and IsOn value's keep unchanged!), so I found this work around...
                if ((currentAsset.IsEnabled.Equals("1") ? true : false) != newState)
                {
                    currentAsset.IsEnabled = ((ToggleSwitch)sender).IsOn ? "1" : "0";
                    await this.CurrentDevice.UpdateAssetAsync(currentAsset);
                    this.RefreshAssetsForCurrentDeviceAsync();
                }
            }
        }

        /// <summary>
        /// Event to remove an asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonRemove_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
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
                var dialog = new MessageDialog($"Configuration exported to {file.Name}", "Backup created");
                if (!status)
                {
                    dialog.Content = $"File {file.Name} couldn't be saved.";
                    dialog.Title = "Error";
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

                try
                {
                    this.Devices = JsonConvert.DeserializeObject<List<Device>>(content);
                    StorageFile dbFile = await StorageFile.GetFileFromPathAsync(this.PathDbFile);
                    await FileIO.WriteTextAsync(dbFile, content);

                    this.LoadConfigurationAsync(this.PathDbFile);

                    var dialog = new MessageDialog($"Configuration imported from {file.Name}", "Import completed");
                    dialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                    dialog.DefaultCommandIndex = 0;
                    var result = await dialog.ShowAsync();
                }
                catch(Exception ex)
                {
                    var dialogError = new MessageDialog($"Oops... {file.Name} is an invalid file. We cannot grab \"Devices\" in this file. See error description below : { Environment.NewLine + ex.Message }");
                    dialogError.Commands.Add(new UICommand("Ok") { Id = 0 });
                    dialogError.DefaultCommandIndex = 0;
                    await dialogError.ShowAsync();
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
            var assetsInListView = ((ListView)sender).Items.OfType<Asset>().ToList();
            await this.CurrentDevice.UpdateOrderAssetsAsync(string.Join(",", assetsInListView.Select(x => x.AssetId)));
        }

        /// <summary>
        /// Go to creation of new asset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButtonAddAsset_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AddOrChangeAssetPage), null);
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
            ListView listView = (ListView)sender;
            this.MenuFlyoutDevice.ShowAt(listView, e.GetPosition(listView));
            var device = ((FrameworkElement)e.OriginalSource).DataContext;
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuFlyoutItemEditDevice_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AddOrChangeDevicePage), this.CurrentRightClickDevice);
        }

        #endregion
    }
}
