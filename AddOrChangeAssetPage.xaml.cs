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
    /// </summary>
    public sealed partial class AddOrChangeAssetPage : Page
    {
        ObservableCollection<Asset> ActiveAssets;
        ObservableCollection<Asset> InactiveAssets;

        public AddOrChangeAssetPage()
        {
            this.InitializeComponent();

            ActiveAssets = GetSampleActiveData();
            InactiveAssets = GetSampleInactiveData();
            this.ListViewActiveAssets.ItemsSource = ActiveAssets;
            this.ListViewInactiveAssets.ItemsSource = InactiveAssets;

            // Back button : http://www.wintellect.com/devcenter/jprosise/handling-the-back-button-in-windows-10-uwp-apps
        }

        private ObservableCollection<Asset> GetSampleActiveData()
        {
            return new ObservableCollection<Asset>
            {
                new Asset() { Name = "Test asset 1", IsActive = true, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" },
                new Asset() { Name = "Test asset 2", IsActive = true, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" },
                new Asset() { Name = "Test asset 3", IsActive = true, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" },
                new Asset() { Name = "Test asset 4", IsActive = true, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" },
                new Asset() { Name = "Test asset 5", IsActive = true, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" }
            };
        }

        private ObservableCollection<Asset> GetSampleInactiveData()
        {
            return new ObservableCollection<Asset>
            {
                new Asset() { Name = "Test asset 1", IsActive = false, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" },
                new Asset() { Name = "Test asset 2", IsActive = false, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" },
                new Asset() { Name = "Test asset 3", IsActive = false, Mimetype = "webpage", NoCache = "0", Uri = "http://www.google.fr/", Duration = "60", IsEnabled = "1" }
            };
        }
    }
}
