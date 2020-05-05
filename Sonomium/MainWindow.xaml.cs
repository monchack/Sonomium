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

using System.IO;
using System.Threading;

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
        private string cursoredArtist = "";
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
        public void setCursoredArtist(string artist) { cursoredArtist = artist; }
        public string getCursoredArtist() { return cursoredArtist; }

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

        static string PostRestApi(string ipString, string cmd, string postData)
        {
            string ipAddress;

            if (postData == "") return "";

            //ipAddress = @"http://" + ipBox.Text + @"/api/v1/getstate";
            ipAddress = @"http://" + ipString + @"/api/v1/";
            ipAddress += cmd; // "addToQueue";

            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(ipAddress);

            req.Method = System.Net.WebRequestMethods.Http.Post;
            req.ContentType = "application/json; charset=\"utf-8\" ";
            req.KeepAlive = false;
            System.Net.Cache.HttpRequestCachePolicy noCachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.NoCacheNoStore);
            req.CachePolicy = noCachePolicy;
            req.Accept = "*/*";
            req.Timeout = 1000; // //タイムアウト (デフォルト 100sec だったのを 1sec = 1000msec に)

            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            req.ContentLength = byteArray.Length;

            // 送信
            try
            {
                Stream dataStream = req.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
            }
            catch
            {
            }

            // 応答
            string resp="";
            try
            {
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)req.GetResponse();
                System.IO.Stream stream = response.GetResponseStream();
                System.IO.StreamReader streamreader = new System.IO.StreamReader(stream);
                resp = streamreader.ReadToEnd();
            }
            catch
            {
            }
            return resp;
        }

        static string GetRestApi(string ipString, string cmd)
        {
            string ipAddress;


            //ipAddress = @"http://" + ipBox.Text + @"/api/v1/getstate";
            ipAddress = @"http://" + ipString + @"/api/v1/";
            ipAddress += cmd; // "addToQueue";

            System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(ipAddress);

            req.Method = System.Net.WebRequestMethods.Http.Get;
            req.ContentType = "application/json; charset=\"utf-8\" ";
            req.KeepAlive = false;
            System.Net.Cache.HttpRequestCachePolicy noCachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.NoCacheNoStore);
            req.CachePolicy = noCachePolicy;
            req.Accept = "*/*";
            req.Timeout = 1000; // //タイムアウト (デフォルト 100sec だったのを 1sec = 1000msec に)

            // 送信 & 応答
            string resp = "";
            try
            {
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)req.GetResponse();
                System.IO.Stream stream = response.GetResponseStream();
                System.IO.StreamReader streamreader = new System.IO.StreamReader(stream);
                resp = streamreader.ReadToEnd();
            }
            catch
            {
            }
            return resp;
        }

        public void addSelectedAlbuomToQue()
        {
            string line;
            int qued = 0;

            string currentQue = GetRestApi(getIp(), "getQueue");
            StringReader srQue = new StringReader(currentQue);
            while ((line = srQue.ReadLine()) != null)
            {
                if (line.Contains("uri\":"))
                {
                    ++qued;
                }
            }

                string s = getSelectedAlbum();
            //string s = artistList.SelectedItem.ToString() + "\" " + "album " + "\"" + ci.AlbumTitle;
            string track = sendMpd("find albumartist " + "\"" + s + "\"");
            StringReader sr = new StringReader(track);

            List<string> files = new List<string>();
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("file: "))
                {
                    string file = line.Replace("file: ", "");
                    files.Add(file);
                }
            }
            string postData = "[";
            foreach (string file in files)
            {
                postData += @"{""uri"":""" + file + @"""},";
            }
            postData = postData.TrimEnd(',');
            postData += "]";
            PostRestApi(getIp(), "addPlay", postData);
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
