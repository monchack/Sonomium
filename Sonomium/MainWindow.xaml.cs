﻿using System;
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

using System.Text.Json;

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
        private int albumArtSize = 0;

        class VolumioState
        {
            public string status { get; set; }
            public string artist { get; set; }
            public string album { get; set; }
            public string title { get; set; }
            public string trackType { get; set; }
        };

        class SonomiumSettings
        {
            public string ipServer { get; set; }
            public string cursoredArtist { get; set; }
            public int windowWidth { get; set; }
            public int windowHeight { get; set; }
            public int windowState { get; set; }
            public int albumArtSize { get; set; }
        };

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
        public void setAlbumArtSize(int size) { albumArtSize = size; }
        public int getAlbumArtSize() { return albumArtSize; }

        public string GetProfileFilePathAndName()
        {
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s += "\\Sonomium\\profile.json";
            return s;
        }

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


        public void UpdatePlayerUI()
        {
            string s = GetRestApi(getIp(), "getstate");
            VolumioState vs = JsonSerializer.Deserialize<VolumioState>(s);
            CurrentArtist.Text = vs.artist;
            CurrentTitle.Text = vs.title;
            CurrentAlbum.Text = vs.album;

        }

        public void addSelectedAlbuomToQue()
        {
            string line;

            // artist と album 情報から、トラックを抽出してキューに積む
            string s = "find albumartist " + "\"" + getSelectedArtist() + "\"" + " album " + "\"" + getSelectedAlbum() + "\"";
            string track = sendMpd(s);

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

            UpdatePlayerUI();
        }


        private void Button_Main_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(new PageMainPlayer(this));
            buttonMain.BorderBrush = SystemColors.HighlightBrush; //Brushes.Black;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = Brushes.Transparent;
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(new PageSettings(this));
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = SystemColors.HighlightBrush;
        }

        private void Button_Current_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(new PageCurrent(this));
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = SystemColors.HighlightBrush;
            buttonSettings.BorderBrush = Brushes.Transparent;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            GetRestApi(getIp(), "commands/?cmd=pause");
            UpdatePlayerUI();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            GetRestApi(getIp(), "commands/?cmd=play");
            UpdatePlayerUI();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            GetRestApi(getIp(), "commands/?cmd=next");
            UpdatePlayerUI();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            GetRestApi(getIp(), "commands/?cmd=prev");
            UpdatePlayerUI();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //save
            SonomiumSettings a = new SonomiumSettings { ipServer = this.ipServer, cursoredArtist = this.cursoredArtist, albumArtSize = this.albumArtSize };
            a.windowWidth = (int)this.Width;
            a.windowHeight = (int)this.Height;
            a.windowState = (int)this.WindowState;

            string jsonString;
            jsonString = JsonSerializer.Serialize(a);
            string s = GetProfileFilePathAndName();

            string path = System.IO.Path.GetDirectoryName(s);
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(s)); //must be path including file name
            }
            try
            {
                File.WriteAllText(s, jsonString);
            }
            catch
            {
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            string s = GetProfileFilePathAndName();
            string jsonString;

            try
            {
                jsonString = File.ReadAllText(s);
                SonomiumSettings ss = JsonSerializer.Deserialize<SonomiumSettings>(jsonString);
                Application.Current.MainWindow.WindowState = (System.Windows.WindowState)ss.windowState;
                Application.Current.MainWindow.Height = ss.windowHeight;
                Application.Current.MainWindow.Width = ss.windowWidth;
                if (ss.ipServer != "") setIp(ss.ipServer);
                if (ss.cursoredArtist != "") setCursoredArtist(ss.cursoredArtist);
                setAlbumArtSize(ss.albumArtSize);
            }
            catch
            {

            }
        }
    }
}
