using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
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
    public sealed partial class AddOrChangeDevicePage : Page
    {
        private List<Device> Devices;
        private const string DB_FILE = "db.json";
        private string PathDbFile;
        private Device ExistingDevice;

        public AddOrChangeDevicePage()
        {
            this.Devices = new List<Device>();
            // AppData folder access
            var localFolder = ApplicationData.Current.LocalFolder;
            this.PathDbFile = localFolder.Path + Path.DirectorySeparatorChar + DB_FILE;

            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(this.PathDbFile);
            string json = await FileIO.ReadTextAsync(file);
            this.Devices = JsonConvert.DeserializeObject<List<Device>>(json);

            // Load device information into fields if Edit Mode
            if(e.Parameter != null && e.Parameter is Device)
            {
                this.ExistingDevice = e.Parameter as Device;
                this.TextBoxName.Text = this.ExistingDevice.Name;
                this.TextBoxLocation.Text = this.ExistingDevice.Location;
                this.TextBoxIp.Text = this.ExistingDevice.IpAddress;
                this.TextBoxPort.Text = this.ExistingDevice.Port;
                this.TextBlockTitle.Text = $"Edit device \"{ this.ExistingDevice.Name }\"";
            }
        }

        private async void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if(!this.TextBoxName.Text.Equals(string.Empty) && !this.TextBoxIp.Text.Equals(string.Empty) && !this.TextBoxPort.Equals(string.Empty))
            {
                // To catch Edit Mode
                if (this.ExistingDevice != null)
                {
                    Device deviceToDelete = this.Devices.Where(x => x.Name.Equals(this.ExistingDevice.Name) && x.IpAddress.Equals(this.ExistingDevice.IpAddress)).FirstOrDefault();
                    this.Devices.Remove(deviceToDelete);
                }

                Device newDevice = new Device();
                newDevice.Name = this.TextBoxName.Text;
                newDevice.Location = this.TextBoxLocation.Text;
                newDevice.IpAddress = this.TextBoxIp.Text;
                newDevice.Port = this.TextBoxPort.Text;
                this.Devices.Add(newDevice);

                var dbContent = JsonConvert.SerializeObject(this.Devices);

                StorageFile file = await StorageFile.GetFileFromPathAsync(this.PathDbFile);

                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    await FileIO.WriteTextAsync(file, dbContent);
                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    var dialog = new MessageDialog(this.ExistingDevice != null ? "The device has been updated" : "The new device has been added");
                    if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        dialog.Content = "Cannot save configuration.";
                        dialog.Title = "Error";
                    }
                    dialog.Commands.Add(new UICommand("Ok") { Id = 0 });
                    dialog.DefaultCommandIndex = 0;
                    var result = await dialog.ShowAsync();

                    this.Frame.Navigate(typeof(MainPage), null);
                }
            }
            else
            {
                var dialogError = new MessageDialog($"Oops... You have to fill at least this fields : \"Name\", \"IP Address\" and \"Port\"");
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
    }
}
