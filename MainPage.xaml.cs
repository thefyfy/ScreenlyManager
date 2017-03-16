using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        public MainPage()
        {

            // AppData folder access
            var localFolder = ApplicationData.Current.LocalFolder;

            // Load user config or create db file (if not exists) (containing IPs and informations about RPi)
            if (!File.Exists(localFolder.Path + Path.DirectorySeparatorChar + DB_FILE))
                File.Create(localFolder.Path + Path.DirectorySeparatorChar + DB_FILE);
            else
                this.LoadConfigurationAsync(localFolder.Path + Path.DirectorySeparatorChar + DB_FILE);
                

            this.InitializeComponent();

            this.ListViewDevice.ItemsSource = this.Devices;
        }

        public async void LoadConfigurationAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            string json = await FileIO.ReadTextAsync(file);
            this.Devices = JsonConvert.DeserializeObject<List<Device>>(json);
        }

        private void ListViewDeviceItemClick(object sender, ItemClickEventArgs e)
        {
            var deviceCliked = (Device)e.ClickedItem;
            this.TextBlockCommandBarMain.Text = $"Schedule Overview - {deviceCliked.Name}";

            List<Asset> assets = new List<Asset>();
            assets.AddRange(Task.Run(() => this.GetAssetsAsync(deviceCliked)).Result);

            this.ListViewActiveAssets.ItemsSource = assets.FindAll(x => x.IsActive);
            this.ListViewInactiveAssets.ItemsSource = assets.FindAll(x => !x.IsActive);
            this.TextBlockActiveAsset.Text = $"Active assets ({this.ListViewActiveAssets.Items.Count})";
            this.TextBlockInactiveAsset.Text = $"Inactive assets ({this.ListViewInactiveAssets.Items.Count})";

            this.CurrentDevice = deviceCliked;
        }

        private async void ButtonPreviewClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Uri.TryCreate((((Button)sender).Tag.ToString()), System.UriKind.Absolute, out Uri uriResult))
                await Windows.System.Launcher.LaunchUriAsync(uriResult);
        }

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
                await Task.Run(() => this.RemoveAsset(this.CurrentDevice, assetId));

            // TODO : refresh assets
        }

        public async Task<List<Asset>> GetAssetsAsync(Device d)
        {
            List<Asset> returnedAssets = new List<Asset>();
            string resultJson = string.Empty;
            string parameters = "/api/assets";

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d.HttpLink + parameters);
                request.Method = "GET";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    resultJson = reader.ReadToEnd();
                }

                if (!resultJson.Equals(string.Empty))
                    returnedAssets = JsonConvert.DeserializeObject<List<Asset>>(resultJson);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getWting assets.", ex);
            }

            return returnedAssets;
        }

        public async Task<bool> RemoveAsset(Device d, string assetId)
        {
            string resultJson = string.Empty;
            string parameters = "/api/assets/" + assetId;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d.HttpLink + parameters);
                request.Method = "DELETE";
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    resultJson = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error when asset deleting.", ex);
            }

            return true;
        }

        //public Asset CreateAsset(Device d, Asset a)
        //{
        //    Asset returnedAsset = new Asset();
        //    JsonSerializerSettings settings = new JsonSerializerSettings();
        //    IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
        //    {
        //        DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
        //    };
        //    settings.Converters.Add(dateConverter);

        //    string json = JsonConvert.SerializeObject(a, settings);
        //    var postData = "model=" + json;
        //    var data = Encoding.ASCII.GetBytes(postData);

        //    string resultJson = string.Empty;
        //    string parameters = "/api/assets";

        //    try
        //    {
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d.HttpLink + parameters);
        //        request.Method = "POST";
        //        request.ContentType = "application/x-www-form-urlencoded";
        //        request.ContentLength = data.Length;
        //        Stream dataStream = request.GetRequestStream();
        //        dataStream.Write(data, 0, data.Length);
        //        dataStream.Close();

        //        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        //        {
        //            Stream dataResponseStream = response.GetResponseStream();
        //            StreamReader reader = new StreamReader(dataResponseStream);
        //            resultJson = reader.ReadToEnd();
        //            reader.Close();
        //            dataResponseStream.Close();
        //        }

        //        if (!resultJson.Equals(string.Empty))
        //            returnedAsset = JsonConvert.DeserializeObject<Asset>(resultJson, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error while creating asset.", ex);
        //    }
        //    return returnedAsset;
        //}

        //public Asset GetAsset(Device d, string assetId)
        //{
        //    Asset returnedAsset = new Asset();
        //    JsonSerializerSettings settings = new JsonSerializerSettings();
        //    IsoDateTimeConverter dateConverter = new IsoDateTimeConverter
        //    {
        //        DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"
        //    };
        //    settings.Converters.Add(dateConverter);

        //    string resultJson = string.Empty;
        //    string parameters = "/api/assets/" + assetId;

        //    try
        //    {
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d.HttpLink + parameters);
        //        request.Method = "GET";
        //        using (HttpWebResponse response = (HttpWebResponse)request.())
        //        {
        //            Stream dataStream = response.GetResponseStream();
        //            StreamReader reader = new StreamReader(dataStream);
        //            resultJson = reader.ReadToEnd();
        //            reader.Close();
        //            dataStream.Close();
        //        }

        //        if (!resultJson.Equals(string.Empty))
        //            returnedAsset = JsonConvert.DeserializeObject<Asset>(resultJson, settings);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception(string.Format("Error while getting asset {0}", assetId), ex);
        //    }

        //    return returnedAsset;
        //}
    }
}
