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
    public class TrackInfo
    {
        public string albumTitle { get; set; }
        public string filePath { get; set; }
        public string albumArtist { get; set; }
        public string trackTitle { get; set; }
        public int length { get; set; }
    };

    public class TrackDb
    {
        public int num { set; get; }
        public List<TrackInfo> list { set; get; }
    };

    public class AlbumInfo
    {
        public string albumTitle { get; set; }
        public string filePath { get; set; }
        public string albumArtist { get; set; }
    };

    public class AlbumDb
    {
        public int num { set; get; }
        public List<AlbumInfo> list { set; get; }
    };

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private NavigationService navigation;
        private string ipServer = "";
        private string selectedAlbum = "";
        private string selectedArtist = "";
        private string cursoredArtist = "";
        private BitmapImage selectedAlbumImage;
        private int albumArtSize = 0;
        private AlbumDb albumDb;
        private TrackDb trackDb; 
        private Page pageMain;
        private Page pageAll;
        private Page pageSettings;
        private Page pageTracks;
        private int albumArtResolution = 0;

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
            public int albumArtResolution { get; set; }
        };

        public MainWindow()
        {
            InitializeComponent();

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
                setAlbumArtResolution(ss.albumArtResolution);
            }
            catch
            {
            }

            s = GetDbFilePathAndName();
            try
            {
                //jsonString = File.ReadAllText(s);
                //albumDb = JsonSerializer.Deserialize<AlbumDb>(jsonString);
            }
            catch
            {
                //albumDb = new AlbumDb();
                //albumDb.list = new List<AlbumInfo>();
                //albumDb.num = 0;
            }
            albumDb = new AlbumDb();
            albumDb.list = new List<AlbumInfo>();
            albumDb.num = 0;

            trackDb = new TrackDb();
            trackDb.list = new List<TrackInfo>();
            trackDb.num = 0;


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
        public int getAlbumArtSize() { return albumArtSize;  }
        public void setAlbumDb(AlbumDb db) { albumDb = db; }
        public AlbumDb getAlbumDb() { return albumDb; }
        public void setAlbumArtResolution(int resolution) {  albumArtResolution = resolution;}
        public int getAlbumArtResolution() {  return albumArtResolution; }
        
        public string GetProfileFilePathAndName()
        {
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s += "\\Sonomium\\profile.json";
            return s;
        }

        public string GetDbFilePathAndName()
        {
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s += "\\Sonomium\\albumdb.json";
            return s;
        }

        public string GetImageCacheDirectory()
        {
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s += "\\Sonomium\\Temp\\ImageCache\\";

            if (!File.Exists(s))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(s));
            }

            return s;
        }

        static public string _sendMpd(string ipString, string command, bool wait = true)
        {
            int port = 6600;
            System.Net.Sockets.TcpClient tcp;

            if (ipString == "") return "";
            try
            {
                tcp = new System.Net.Sockets.TcpClient(ipString, port);
            }
            catch
            {
                return "";
            }
            System.Net.Sockets.NetworkStream ns = tcp.GetStream();
            ns.ReadTimeout = 5000;
            ns.WriteTimeout = 5000;

            string sendMsg = command + '\n';
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;
            byte[] sendBytes = encoding.GetBytes(sendMsg);

            ns.Write(sendBytes, 0, sendBytes.Length);

            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            byte[] resBytes = new byte[1024*512];
            int resSize = 0;

            if (wait) System.Threading.Thread.Sleep(500);

            do
            {
                resSize = ns.Read(resBytes, 0, resBytes.Length);
                ms.Write(resBytes, 0, resSize);

                if (resSize >= 4)
                {
                    if (resBytes[resSize - 1] == '\n')
                    {
                        if (resBytes[resSize - 4] == '\n' && resBytes[resSize - 3] == 'O' && resBytes[resSize - 2] == 'K')
                        {
                            break;
                        }
                    }
                }
                if (resSize == 3)
                {
                    if (resBytes[resSize - 3] == 'O' && resBytes[resSize - 2] == 'K' && resBytes[resSize - 1] == '\n')
                    {
                        break;
                    }
                }
            } while (resSize > 0);

            string response = encoding.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            ms.Close();

            ns.Close();
            tcp.Close();

            return response;
        }

        public string sendMpd(string command, bool wait = true)
        {
            return _sendMpd(getIp(), command, wait);
        }

        static string PostRestApi(string ipString, string cmd, string postData)
        {
            string ipAddress;

            if (postData == "") return "";

            ipAddress = @"http://" + ipString + @"/api/v1/";
            ipAddress += cmd;

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

            ipAddress = @"http://" + ipString + @"/api/v1/";
            ipAddress += cmd;

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

        static async Task WaitAndGetVolumioStatus(string ip, bool wait, MainWindow window)
        {
            string s="";
            await Task.Run(() =>
            {
                if (wait == true) Thread.Sleep(1000);
                s = GetRestApi(ip, "getstate");
            });
            VolumioState vs = JsonSerializer.Deserialize<VolumioState>(s);
            window.CurrentArtist.Text = vs.artist;
            window.CurrentTitle.Text = vs.title;
            window.CurrentAlbum.Text = vs.album;
        }

        static void CreateAlbumDb(string ip, MainWindow window)
        {
            string s = "";
            string line;
            int i = 0;

            s = _sendMpd(ip, "listallinfo");

            StringReader sr = new StringReader(s);


            List<string> files = new List<string>();

            string lastAlbum = "";
            string lastArtist = "";
            string lastFilePath = "";
            string lastTrackTitle = "";
            int lastTime = 0;

            window.trackDb.list.Clear();
            window.albumDb.list.Clear();


            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("file: "))
                {
                    if (lastArtist != "" && lastAlbum != "")
                    {
                        TrackInfo info = new TrackInfo();
                        info.albumArtist = lastArtist;
                        info.albumTitle = lastAlbum;
                        info.filePath = lastFilePath;
                        info.trackTitle = lastTrackTitle;
                        info.length = lastTime;
                        window.trackDb.list.Add(info);
                    }
                    lastArtist = "";
                    lastAlbum = "";
                    lastTrackTitle = "";
                    lastFilePath = "";

                    string file = line.Replace("file: ", "");
                    lastFilePath = file;
                }
                if (line.Contains("AlbumArtist: "))
                {
                }
                else if (line.Contains("Artist: "))
                {
                    string artist = line.Replace("Artist: ", "");
                    lastArtist = artist;
                }
                if (line.Contains("Album: "))
                {
                    string album = line.Replace("Album: ", "");
                    lastAlbum = album;
                }
                if (line.Contains("Title: "))
                {
                    string title = line.Replace("Title: ", "");
                    lastTrackTitle = title;
                }
                if (line.Contains("Time: "))
                {
                    string time = line.Replace("Time: ", "");
                    int n = Int32.Parse(time);
                    lastTime = n;
                }
            }
            if (lastArtist != "" && lastAlbum != "")
            {
                TrackInfo info = new TrackInfo();
                info.albumArtist = lastArtist;
                info.albumTitle = lastAlbum;
                info.filePath = lastFilePath;
                window.trackDb.list.Add(info);
            }

            var art = (from b in window.trackDb.list
                      select b.albumArtist).Distinct();

            foreach (var b in art)
            {
                var alb = (from c in window.trackDb.list
                           where c.albumArtist == b
                           select c.albumTitle).Distinct();
                foreach (var d in alb)
                {
                    var filess = from e in window.trackDb.list
                                 where e.albumArtist == b
                                 where e.albumTitle == d
                                 select e.filePath;
                    foreach (var x in filess)
                    {
                        AlbumInfo info = new AlbumInfo();
                        info.albumArtist = b;
                        info.albumTitle = d;
                        info.filePath = x;
                        window.albumDb.list.Add(info);
                        ++i;
                        break;
                    }
                }
            }


            window.albumDb.num = i;
        }

        public List<TrackInfo> GetCurrentAlbumTracks()
        {
            List<TrackInfo> list = new List<TrackInfo>();

            var tracks = from e in trackDb.list
                            where e.albumArtist == selectedArtist
                            where e.albumTitle == selectedAlbum
                            select e;
            foreach (var x in tracks)
            {
                list.Add(x);
            }
            return list;
        }

        public List<AlbumInfo> GetCurrentArtistAlbums()
        {
            List<AlbumInfo> list = new List<AlbumInfo>();

            var tracks = from e in albumDb.list
                         where e.albumArtist == selectedArtist
                         select e;
            foreach (var x in tracks)
            {
                list.Add(x);
            }
            return list;
        }

        public List<AlbumInfo> GetCursoredArtistAlbums()
        {
            List<AlbumInfo> list = new List<AlbumInfo>();

            var tracks = from e in albumDb.list
                         where e.albumArtist == cursoredArtist
                         select e;
            foreach (var x in tracks)
            {
                list.Add(x);
            }
            return list;
        }

        public string GetServerInfo()
        {
            string json = GetRestApi(this.getIp(), "getSystemVersion");
            return json;
        }

        public void RemoveImageCache()
        {
            string s = GetImageCacheDirectory();
            try
            {
                System.IO.Directory.Delete(s, true);
            }
            catch
            {
            }
        }

        public void UpdatePlayerUI()
        {
            Task t = WaitAndGetVolumioStatus(getIp(), true, this);
        }

        public void addSelectedAlbuomToQue(int pos, bool start)
        {
            List<string> files = new List<string>();
            int i = 0;

            var tracks = GetCurrentAlbumTracks();
            for (i = 0; i < tracks.Count; ++i)
            {
                if (i + 1 < pos) continue;
                files.Add(tracks[i].filePath);
            }

            string postData = "[";
            foreach (string file in files)
            {
                postData += @"{""uri"":""" + file + @"""},";
            }
            postData = postData.TrimEnd(',');
            postData += "]";

            if (start==true) PostRestApi(getIp(), "addPlay", postData);
            else PostRestApi(getIp(), "addToQueue", postData);

            UpdatePlayerUI();
        }


        private void Button_Main_Click(object sender, RoutedEventArgs e)
        {
            //navigation.Navigate(new PageAllAlbums(this));
            navigation.Navigate(pageAll);
            buttonMain.BorderBrush = SystemColors.HighlightBrush; //Brushes.Black;
            buttonArtist.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = Brushes.Transparent;
        }

        private void Button_Artist_Click(object sender, RoutedEventArgs e)
        {
            //navigation.Navigate(new PageMainPlayer(this));
            navigation.Navigate(pageMain);
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonArtist.BorderBrush = SystemColors.HighlightBrush;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = Brushes.Transparent;
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            //navigation.Navigate(new PageSettings(this));
            navigation.Navigate(pageSettings);
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonArtist.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = SystemColors.HighlightBrush;
        }

        public void Button_Current_Click(object sender, RoutedEventArgs e)
        {
            //navigation.Navigate(new PageCurrent(this));
            navigation.Navigate(pageTracks);
            buttonMain.BorderBrush = Brushes.Transparent;
            buttonArtist.BorderBrush = Brushes.Transparent;
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
            SonomiumSettings a = new SonomiumSettings { ipServer = this.ipServer, cursoredArtist = this.cursoredArtist, albumArtSize = this.albumArtSize, albumArtResolution=this.albumArtResolution };
            a.windowWidth = (int)this.Width;
            a.windowHeight = (int)this.Height;
            a.windowState = (int)this.WindowState;


            string jsonString;
            jsonString = JsonSerializer.Serialize(a);
            string s = GetProfileFilePathAndName();
            string path = System.IO.Path.GetDirectoryName(s) + "\\";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }
            try
            {
                File.WriteAllText(s, jsonString);
            }
            catch
            {
            }

            /*
            jsonString = JsonSerializer.Serialize(albumDb);
            s = GetDbFilePathAndName();
            path = System.IO.Path.GetDirectoryName(s) + "\\";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            }
            try
            {
                File.WriteAllText(s, jsonString);
            }
            catch
            {
            }
            */
        }

        private void Back15Button_Click(object sender, RoutedEventArgs e)
        {
            _sendMpd(getIp(), "seekcur -15", false);
        }

        private void Skip15Button_Click(object sender, RoutedEventArgs e)
        {
            _sendMpd(getIp(), "seekcur +15", false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Task t =  CreateAlbumDb(getIp(), this);
            CreateAlbumDb(getIp(), this);

            pageMain = new PageMainPlayer(this);
            pageAll = new PageAllAlbums(this);
            pageSettings = new PageSettings(this);
            pageTracks = new PageCurrent(this);
        }
    }
}
