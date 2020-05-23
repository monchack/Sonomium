using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.Json;
using System.Net;

namespace Sonomium
{
    /// <summary>
    /// PageSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class PageSettings : Page
    {
        class VolumioVersionInfo
        {
            public string systemversion { get; set; }
            public string builddate { get; set; }
            public string hardware { get; set; }
        };

        private MainWindow mainWindow;

        public PageSettings(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
        }

        private void IpBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Net.IPAddress ip;

            if (!System.Net.IPAddress.TryParse(ipBox.Text, out ip))
            {
                // invalid
            }
            if (mainWindow!=null) mainWindow.setIp(ipBox.Text);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                ipBox.Text = mainWindow.getIp();
                if (mainWindow.getAlbumArtSize() == 0) buttonAlbumArtSmall.IsChecked = true;
                else if (mainWindow.getAlbumArtSize() == 2) buttonAlbumArtLarge.IsChecked = true;
                else if (mainWindow.getAlbumArtSize() == 3) buttonAlbumArtXLarge.IsChecked = true;
                else buttonAlbumArtNormal.IsChecked = true;

                if (mainWindow.getAlbumArtResolution() == 0) buttonAlbumArtResolutionNormal.IsChecked = true;
                else buttonAlbumArtResolutionHigh.IsChecked = true;
            }
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private void ButtonAlbumArt_Checked(object sender, RoutedEventArgs e)
        {
            if (buttonAlbumArtSmall.IsChecked == true) { mainWindow.setAlbumArtSize(0); }
            else if (buttonAlbumArtLarge.IsChecked == true) { mainWindow.setAlbumArtSize(2); }
            else if (buttonAlbumArtXLarge.IsChecked == true) { mainWindow.setAlbumArtSize(3); }
            else mainWindow.setAlbumArtSize(1);
        }

        private void ConnectivityTest_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null) return;
            string json = mainWindow.GetServerInfo();
            try
            {
                VolumioVersionInfo vvi = JsonSerializer.Deserialize<VolumioVersionInfo>(json);
                textTestResult.Text = "version :" + vvi.systemversion + "\n" + "build date: " + vvi.builddate + "\n" + "hardware: " + vvi.hardware;
            }
            catch
            {
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null) return;
            mainWindow.RemoveImageCache();
        }

        private void ButtonAlbumArtResolution_Checked(object sender, RoutedEventArgs e)
        {
            if (buttonAlbumArtResolutionNormal.IsChecked == true) { mainWindow.setAlbumArtResolution(0); }
            else if (buttonAlbumArtResolutionHigh.IsChecked == true) { mainWindow.setAlbumArtResolution(1); }
        }

        private void IpSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostEntry("volumio.local");
                foreach (IPAddress address in hostInfo.AddressList)
                {
                    ipBox.Text = address.ToString();
                    break;
                }
            }
            catch
            {
            }
        }
    }
}
