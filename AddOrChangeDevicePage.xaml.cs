using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
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
    public sealed partial class AddOrChangeDevicePage : Page
    {
        private List<Device> Devices;
        private const string DB_FILE = "db.json";

        public AddOrChangeDevicePage()
        {
            this.Devices = new List<Device>();

            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Devices = e.Parameter as List<Device>;
        }

        private async void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {
            if(!this.TextBoxName.Text.Equals(string.Empty) && !this.TextBoxIp.Text.Equals(string.Empty) && !this.TextBoxPort.Equals(string.Empty))
            {
                Device newDevice = new Device();
                newDevice.Name = this.TextBoxName.Text;
                newDevice.Location = this.TextBoxLocation.Text;
                newDevice.IpAddress = this.TextBoxIp.Text;
                newDevice.Port = this.TextBoxPort.Text;
                this.Devices.Add(newDevice);

                var dbContent = JsonConvert.SerializeObject(this.Devices);

                var pathDbFile = ApplicationData.Current.LocalFolder.Path + Path.DirectorySeparatorChar + DB_FILE;
                StorageFile file = await StorageFile.GetFileFromPathAsync(pathDbFile);

                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    await FileIO.WriteTextAsync(file, dbContent);
                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    var dialog = new MessageDialog($"The new device has been added");
                    if (status != Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        dialog.Content = $"Cannot save configuration.";
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
