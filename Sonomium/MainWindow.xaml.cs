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

namespace Sonomium
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private NavigationService navigation;
        private string ipServer = "192.168.0.51";
        private string selectedAlbum = "";
        private string selectedArtist = "";
        private BitmapImage selectedAlbumImage;

        public MainWindow()
        {
            InitializeComponent();
            navigation = this.mainFrame.NavigationService;
            navigation.Navigate(new PageSettings(this));
        }

        public void setIp(string ip) { ipServer = ip; }
        public string getIp() { return ipServer; }
        public void setSelectedAlbum(string album) { selectedAlbum = album; }
        public string getSelectedAlbum() { return selectedAlbum; }
        public void setSelectedArtist(string artist) { selectedArtist = artist; }
        public string getSelectedArtist() { return selectedArtist; }
        public void setSelectedAlbumImage(BitmapImage image) { selectedAlbumImage = image; }
        public BitmapImage getSelectedAlbumImage() { return selectedAlbumImage; }

        public string sendMpd(string command, bool wait = true)
        {
            string ipString = getIp();
            int port = 6600;

            System.Net.Sockets.TcpClient tcp = new System.Net.Sockets.TcpClient(ipString, port);
            System.Net.Sockets.NetworkStream ns = tcp.GetStream();
            ns.ReadTimeout = 10000;
            ns.WriteTimeout = 10000;

            string sendMsg = command + '\n';
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            byte[] sendBytes = encoding.GetBytes(sendMsg);

            ns.Write(sendBytes, 0, sendBytes.Length);

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] resBytes = new byte[256];
            int resSize = 0;

            if (wait) System.Threading.Thread.Sleep(500);

            do
            {
                resSize = ns.Read(resBytes, 0, resBytes.Length);
                if (resSize == 0)
                {
                    // 切断
                    break;
                }
                ms.Write(resBytes, 0, resSize);
            } while (ns.DataAvailable);
            string response = encoding.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();

            ns.Close();
            tcp.Close();

            return response;
        }


        private void Button_Main_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(new PageMainPlayer(this));
            buttonMain.BorderBrush = Brushes.Black;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = Brushes.Transparent;
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(new PageSettings(this));
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = Brushes.Black;
        }

        private void Button_Current_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(new PageCurrent(this));
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Black;
            buttonSettings.BorderBrush = Brushes.Transparent;
        }
    }
}
