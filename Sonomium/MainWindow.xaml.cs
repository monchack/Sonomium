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
using System.Net.Http;

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
        public string genre { get; set; }
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
        public string albumCacheImagePath { get; set; }
        public List<string> genreList { get; set; }
        public int genreIndex { get; set; }
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
        private string playedAlbum = "";
        private string playedArtist = "";
        private string playedTitle = "";
        private BitmapImage selectedAlbumImage;
        private int albumArtSize = 0;
        private AlbumDb albumDb;
        private TrackDb trackDb; 
        //private Page pageMain;
        private Page pageAll;
        private Page pageSettings;
        private Page pageTracks;
        private Page pageOpening;
        private int albumArtResolution = 0;
        private Task readTask = null;
        private CancellationTokenSource cancellationSource;

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

            navigation = this.mainFrame.NavigationService;

            // DBの初期化. load は Window_Loaded でおこなう.

            albumDb = new AlbumDb();
            albumDb.list = new List<AlbumInfo>();
            albumDb.num = 0;

            trackDb = new TrackDb();
            trackDb.list = new List<TrackInfo>();
            trackDb.num = 0;

            //navigation = this.mainFrame.NavigationService;
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
        public string getPlayedArtist() { return playedArtist; }
        public string getPlayedAlbum() { return playedAlbum; }
        public string getPlayedTitle() { return playedTitle; }
        public List<string> genreList { get; set; }

        public string GetTrackListFilePathAndName()
        {
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            s += "\\Sonomium\\listallinfo.txt";
            return s;
        }

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

        public static string GetImageCacheDirectory()
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

        public static async Task WaitAndGetVolumioStatus(string ip, bool wait, MainWindow window)
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
            window.playedArtist = vs.artist;
            window.playedAlbum = vs.album;
            window.playedTitle = vs.title;
        }

        public static (string artist, string album, string title)? GetVolumioStatusSync(string ip, bool wait)
        {
            string s = "";
            if (wait == true) Thread.Sleep(1000);
            s = GetRestApi(ip, "getstate");
            try
            {
                VolumioState vs = JsonSerializer.Deserialize<VolumioState>(s);
                return (vs.artist, vs.album, vs.title);
            }
            catch
            {
            }
            return null;
        }

        static void CreateAlbumDb(string ip, MainWindow window)
        {
            string s = "";
            string line;
            int i = 0;
            string listFile = window.GetTrackListFilePathAndName();
            bool readFromFile = false;

            //Thread.Sleep(3000);

            s = _sendMpd(ip, "listallinfo");
            if (s == "")
            {
                //ipが無効 もしくは　ネットが無効の場合は、すでにローカルにあるファイルを利用
                try
                {
                    System.IO.StreamReader srList = new System.IO.StreamReader(listFile, false);
                    s = srList.ReadToEnd();
                    readFromFile = true;
                }
                catch
                {
                    //ファイルも存在しない
                    return;
                }
            }

            StringReader sr = new StringReader(s);
            List<string> files = new List<string>();

            string lastAlbum = "";
            string lastArtist = "";
            string lastFilePath = "";
            string lastTrackTitle = "";
            string lastGenre = "";
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
                        info.genre = lastGenre;
                        window.trackDb.list.Add(info);
                    }
                    lastArtist = "";
                    lastAlbum = "";
                    lastTrackTitle = "";
                    lastFilePath = "";
                    System.Resources.ResourceManager resource = Properties.Resources.ResourceManager;
                    lastGenre = resource.GetString("HTML_GenreUnspecfied");

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
                if (line.Contains("Genre: "))
                {
                    string genre = line.Replace("Genre: ", "");
                    lastGenre = genre;
                }
            }
            if (lastArtist != "" && lastAlbum != "")
            {
                TrackInfo info = new TrackInfo();
                info.albumArtist = lastArtist;
                info.albumTitle = lastAlbum;
                info.filePath = lastFilePath;
                info.length = lastTime;
                info.genre = lastGenre;
                info.trackTitle = lastTrackTitle;
                window.trackDb.list.Add(info);
            }
            /// ここで 全track情報がセットされた

            /// art: 全アーティスト名リスト
            var art = (from b in window.trackDb.list
                      select b.albumArtist).Distinct();

            foreach (var b in art)
            {
                /// alb: そのアーティストの全アルバムリスト
                var alb = (from c in window.trackDb.list
                           where c.albumArtist == b
                           select c.albumTitle).Distinct();

                foreach (var d in alb)
                {
                    //トラックリストからアルバム中に多い順に並んだジャンルリストを得る
                    var albTracks = from e in window.trackDb.list
                                 where e.albumArtist == b
                                 where e.albumTitle == d
                                 select (e);
                    List<string> a = window.GetSortedGenreList(albTracks.ToList());


                    // filess: そのアルバムの全ファイル
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
                        info.genreList = a;
                        info.albumCacheImagePath = GetAlbumCacheImageFilePathFromOriginalFilePath(x);
                        window.albumDb.list.Add(info);
                        ++i;
                        break;
                    }
                }
            }
            window.albumDb.num = i;
            if (!readFromFile)
            {
                try
                {
                    System.IO.StreamWriter swList = new System.IO.StreamWriter(listFile, false);
                    swList.Write(s);
                    swList.Close();
                }
                catch
                {
                }
            }

            /// 全トラック中にでてくるgenreのリスト作成し、頻出度順に並べる
            var tracks = from e in window.trackDb.list
                         select (e);
            window.genreList = window.GetSortedGenreList(tracks.ToList());

            //// 各アルバムの代表genre が、全genre頻出度リストの何番目かをセット
            foreach (var x in window.albumDb.list)
            {
                if (x.genreList.Count == 0)
                {
                    x.genreIndex = -1;
                }
                else
                {
                    int n = window.genreList.IndexOf(x.genreList[0]);
                    x.genreIndex = n;
                }
            }

            //// db ができたので、bitmap のコピーと html　作成を開始
            //Task t = Task.Run(() => downloadImages(window.albumDb, window.getIp(), window.GetImageCacheDirectory(), window.cancellationSource.Token));
            window.downloadImages();
            window.generateHtml();


        }

        public (string artist, string album) getAlbumArtistAndNameById(string id)
        {
            //int n = Convert.ToInt32(id);
            int n = Int32.Parse(id, System.Globalization.NumberStyles.HexNumber);
            var albums = from e in albumDb.list
                         where e.GetHashCode() == n
                         select e;
            string artist = "";
            string album = "";
            foreach (var x in albums)
            {
                artist = x.albumArtist;
                album = x.albumTitle;
                break;
            }
            return (artist, album);
        }

        public List<string> GetArtistList()
        {
            List<string> artist_list = new List<string>();
            var art = (from b in trackDb.list
                       select b.albumArtist).Distinct(); // artistの重複を除去
            return art.ToList();
        }

        public List<string> GetArtistListForGenre(string genre)
        {
            List<string> artist_list = new List<string>();
            var art = (from b in trackDb.list
                where b.genre == genre
                select b.albumArtist).Distinct(); // artistの重複を除去
            return art.ToList();
        }

        public List<string> GetSortedGenreList(List<TrackInfo> tracks)
        {
            List<string> genreList = new List<string>();
            var x = tracks.GroupBy(b => b.genre).Select(c => new { genre = c.Key.ToString(), count = c.Count() }).OrderByDescending(d => d.count).ToList();
            foreach (var s in x)
            {
                if (s.genre!="") genreList.Add(s.genre);
            }
            return genreList;
        }

        public List<TrackInfo> GetAlbumTracks((string artist, string album)v)
        {
            List<TrackInfo> list = new List<TrackInfo>();

            var tracks = from e in trackDb.list
                         where e.albumArtist == v.artist
                         where e.albumTitle == v.album
                         select e;
            foreach (var x in tracks)
            {
                list.Add(x);
            }
            return list;
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

        public void UpdatePlayedArtistAndAlbum()
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

        public static string GetAlbumCacheImageFilePathFromOriginalFilePath(string filePath)
        {
            string pathAndName = "";
            pathAndName = filePath;
            int n = pathAndName.LastIndexOf('/');
            string s = pathAndName.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
            string fileName = System.IO.Path.GetFileName(s) + ".jpg";
            string imageCacheFileName = GetImageCacheDirectory() + fileName;

            return imageCacheFileName;
        }

        public string GetAlbumCacheImageFilePathAndName(string artistName, string albumName)
        {
            string pathAndName = "";
            var albums = from e in albumDb.list
                         where e.albumArtist == artistName
                         where e.albumTitle == albumName
                         select e;
            foreach (var x in albums)
            {
                pathAndName = x.filePath;
                break;
            }

            int n = pathAndName.LastIndexOf('/');
            string s = pathAndName.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
            string fileName = System.IO.Path.GetFileName(s) + ".jpg";
            string imageCacheFileName = GetImageCacheDirectory() + fileName;

            return imageCacheFileName;
        }

        public BitmapImage getBitmapImageFromFileName(string fileName)
        {
            BitmapImage bitmap;
            bitmap = new BitmapImage();
            try
            {
                bitmap.BeginInit();
                //if (getAlbumArtResolution() == 0) bitmap.DecodePixelWidth = size;
                bitmap.UriSource = new Uri(@"file://" + fileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            catch
            {
                return null;
            }
            return bitmap;
        }

        public async void downloadImages()
        {
            await Task.Run(() => CopyImageFile(albumDb, getIp(), GetImageCacheDirectory(), cancellationSource.Token));
        }

        public string generateHtml()
        {
            string html = "";
            AlbumDb db = getAlbumDb();
            List<string> genreList = this.genreList;// GetGenreList();

            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName += "\\Sonomium\\albums.html";

            html += @"<html>";
            html += @"<head>";
            html += @"<title></title>";
            html += @"<style>";
            html += @"body { overscroll-behavior : none; margin:0 0 0 0;overflow-x:hidden; overflow-y:hidden;  } ";
            html += @".super_container { scroll-snap-type: x mandatory; overflow:auto;   display:flex;  width:100vw;height:auto;  scroll-behavior: smooth;}";
            html += @".sub_container {scroll-snap-align: start; flex:none; width:100%; height:100vh; overflow:visible; overflow-x:visible; overflow-y:scroll; }";
            html += @".wrapper2 {z-index:0; position: relative; display: flex; flex-wrap : wrap ;  flex-direction: row; justify-content: space-between; overflow:visible; overflow-x:hidden;overflow-y:hidden; }";
            html += @".option_list {font-family: ""Segoe UI"", ""BIZ UDPGothic"", ""Segoe UI"";font-size: 14pt;margin: 3pt 0 3pt 0; }";
            html += @".option_list:checked { background-color: red;}";
            html += @"option:checked  { background-color: red;}";


            string cardSize = "16vw";
            string cardMinSize = "160px";
            string albumArtSize = "15vw";
            string albumArtMinSize = "150px";
            string fontsize = "9pt";
            
            switch (getAlbumArtSize())
            {
                case 0:
                cardSize = "11vw";
                cardMinSize = "80px";
                albumArtSize = "10vw";
                albumArtMinSize = "80px";
                break;

                case 1:
                cardSize = "13vw";
                cardMinSize = "80px";
                albumArtSize = "12vw";
                albumArtMinSize = "80px";
                fontsize = "10pt";
                break;

                case 2:
                cardSize = "16vw";
                cardMinSize = "80px";
                albumArtSize = "15vw";
                albumArtMinSize = "80px";
                fontsize = "11pt";
                break;

                case 3:
                cardSize = "17vw";
                cardMinSize = "80px";
                albumArtSize = "16vw";
                albumArtMinSize = "80px";
                fontsize = "12pt";
                break;
            }

            html += $@".card {{ font-family:  ""Segoe UI""; width: {cardSize}; min-width: {cardMinSize}; background: #fff; border-width: 0px; float: left; text-align: center; }}";html += "\r\n";
            html += $@".cardx {{ width: {cardSize}; min-width: {cardMinSize}; height: 0px; background: #fff; border-width: 0px; float: left; text-align: center; }}"; ; html += "\r\n";

            html += $@".highlight {{position: relative; width: {albumArtSize};margin: 0;}}";
            html += $@".caption {{ display: none; font-family: ""Segoe UI Semibold"", ""BIZ UDPGothic"", ""Segoe UI"";   pointer-events: none; animation: captionAnime 0.8s linear; line-height:1.5; border-radius: 0 0 5px 5px; font-size: {fontsize};  user-select: none; position: absolute;bottom: -60px;left: 0;z-index: 2;width: 100%; background:rgba(255,255,255,0.6);}} ";
            html += @".highlight:active  figcaption { display:inline; bottom: 0;}";
            html += @".highlight:hover  figcaption { display:inline; bottom: 0;}";
            html += @"@keyframes  captionAnime { 90% { color : black; background:rgba(255, 255, 255, 0.55) } 50% { color : rgba(0,0,0,0.6); background:rgba(255, 255, 255, 0.4) } 0% { color : rgba(0,0,0,0); background:rgba(255, 255, 255, 0) }}";
            html += @".card:hover img{  transition-duration: 0.3s;  filter: blur(2px) ; }";

            if (getAlbumArtResolution() == 1)
            {
                html += $@".card_image {{ border-radius: 5px 5px 5px 5px; width: {albumArtSize}; min-width: {albumArtMinSize}; height: {albumArtSize}; min-height: {albumArtMinSize}; box-shadow: 3pt 3pt 5pt gray ;}}";
            }
            else
            {
                html += $@".card_image {{width:{albumArtSize} ; min-width: {albumArtMinSize}; height: {albumArtSize}; min-height: {albumArtMinSize}; }}";
            }

            html += @".card_content { padding: 5pt 0px 8pt 0px;  }";
            html += @".card-title { font-size: 20px; margin-bottom: 40px; text-align: center; color: #333;}";
            html += @".card_text { color: #777; user-select: none; height:26pt;  font-size: 12px;   text-align: left; margin: 0vw 0.5vw 0 0;  overflow : hidden;display: -webkit-box;-webkit-box-orient: vertical;-webkit-line-clamp: 2; }";

            ///test style
            html += @"ul { margin: 0; padding-left: 0;}";
            html += @"li {list-style: none; font-family:""Segoe UI"";  font-size: 16pt; }";
            html += @"#menu {  position: fixed; top: 0; right: -340px; width: 300px;  height: 100%;  padding: 20px; transition: left .5s, right .5s; background-color: rgba(86, 86, 86, .7); z-index:20; }";
            html += @".toggle { font-size: 50px; cursor: pointer;}";
            html += @".toggle:hover  { text-decoration: underline; }";
            html += @"#open_optional  {  display: none;}";
            html += @"#open_optional:checked + #menu  {  right: 0;}";
            html += @".list_box_2 { border: 0; background:transparent; outline: none; padding: 8pt 16pt 8pt 12pt; border-radius: 8pt; box-shadow: inset 8pt 8pt 12pt #D8DBDF, inset -8pt -8pt 12pt #FFFFFF;  }";



            html += @"</style>";
            html += @"</head>";
            html += @"<body onselectstart=""return false;"" >";

            html += @"<script type=""text/javascript"">";
            html += @"var lastDownload=Date.now();"; html += "\r\n";
            html += @"function reload(e) { if (Date.now() < lastDownload + 15000) e.src=e.src; } 
                function reload2(e) { e.onload=""""; e.src=e.src; }
                function startImageLoadTimer(e) { setTimeout( reload,1500, e); }"; //1.5sec ごとにリトライ
            html += @"function finalImageLoad(e) { setTimeout( reload2,8000, e); lastDownload=Date.now(); }"; // 8sec後に念のため再読み込み
            
            //html += @"function do_select() {var x = document.getElementsByClassName('card');[].forEach.call(x, function(v) {   }); }";

            html += @"function all_clear(){var cards = document.getElementsByClassName('card');var len = cards.length;for (var i = 0; i < len; ++i){ cards[i].style.display =""none"";} }"; html += "\r\n";
            html += @"function close_optional(){document.getElementById(""open_optional"").click(); window.location.href=""#p2""; }"; html += "\r\n";

            ////genre から artist を表示し直す
            html += @"const artist_for_genre_tbl = {"; html += "\r\n";
            foreach (var x in genreList)
            {
                List<string> a = GetArtistListForGenre(x);
                html += $@"""{x}"" : ["; html += "\r\n";
                foreach (var y in a)
                {
                    string s = y.Replace("'", @"\'"); html += "\r\n";
                    html += $@"""{s}"",";
                }
                html += @"],"; html += "\r\n";
            }
            html += @"};"; html += "\r\n";

            /// artist ハッシュテーブル
            List<string> artistList = GetArtistList();
            html += @"const artist_hash_tbl = {"; html += "\r\n";
            foreach (var x in artistList)
            {
                string s = x.Replace("'", @"\'"); html += "\r\n";
                html += $@"""{s}"" : [{x.GetHashCode().ToString()}],"; html += "\r\n";
            }
            html += @"};"; html += "\r\n";

            html += @"function resetArtistList(element) {"; html += "\r\n";
            html += @"genre = element.options[element.selectedIndex].text;";
            html += @"type = element.options[element.selectedIndex].value;";
            html += $@"console.log(""genre selected : "" + genre + "" : "" + type);";
            html += @"if (type=='0') {allGenreSelected(); return;}";
            html += @"show_all_albums_for_genre(genre);";
            html += @"let x = document.getElementById('artistListForGenre');"; html += "\r\n";
            html += @"var length = x.options.length;for (i = 0; i < length; i++) {  x.options[0].remove(); }";
            html += @"artist_for_genre_tbl[genre].forEach( s => {"; html += "\r\n";
            html += @"let option = document.createElement('option');"; html += "\r\n";
            html += @"option.innerHTML = s;"; html += "\r\n";
            html += @"option.className=""option_list"";"; html += "\r\n";
            html += @"x.appendChild(option);  "; html += "\r\n";
            html += @"});"; html += "\r\n";
            html += @"}"; html += "\r\n";

            html += @"function artist_selected(artist) {";
            html += @"var s = artist_hash_tbl[artist];";
            html += $@"console.log(""artist selected: ""+ artist+ "" : "" + s);";
            html += @"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){ if (cards[i].id == s ) cards[i].style.display =""block""; else cards[i].style.display=""none"";}";
            html += @"}"; html += "\r\n";

            html += @"function show_all_albums_for_genre(genre) {";
            html += @"var array = new Array();";
            html += @"artist_for_genre_tbl[genre].forEach( s => {"; html += "\r\n";
            html += @"var x = artist_hash_tbl[s]; array.push(String(x));";
            html += @"});"; html += "\r\n";
            html += @"console.log('array:', array);";
            html += @"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){ if (array.includes(cards[i].id)) {cards[i].style.display =""block""; console.log('OK: ' + cards[i].id);} else cards[i].style.display=""none""; }";
            html += @"}";


            ///allgenre
            html += @"function allGenreSelected() {";
            html += $@"console.log(""all genre selected"");";
            html += @"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){ cards[i].style.display=""block"";}";
            html += @"let x = document.getElementById('artistListForGenre');"; html += "\r\n";
            html += @"var length = x.options.length;for (i = 0; i < length; i++) {  x.options[0].remove(); }";
            artistList = GetArtistList();
            html += @"var option;";
            foreach (var x in artistList)
            {
                html += @"option = document.createElement('option');"; html += "\r\n";
                string s = x.Replace("'", @"\'"); html += "\r\n";
                html += $@"option.innerHTML = '{s}';"; html += "\r\n";
                html += @"option.className=""option_list"";"; html += "\r\n";
                html += @"x.appendChild(option);  "; html += "\r\n";
            }
            html += $@"console.log(""all genre selected : end"");";
            html += @"}"; html += "\r\n";

            html += @"function test(genre_index) {";
            html += @"var cards = document.getElementsByClassName('card');";
            html += @"var len = cards.length;";
            html += @"for (var i = 0; i < len; ++i) {if(genre_index == -1 || cards[i].id == genre_index) cards[i].parentNode.style.display =""block""; else cards[i].parentNode.style.display =""none""; }";
            html += @"}";
            #if !DEBUG
            html += @"window.onload = function() {  document.body.oncontextmenu = function () { return false;  }  }";
            #else
            html += @"window.onload = function() { }";
            #endif
            html += @"</script>";
            html += @"<div class=""super_container"" >";
            html += @"<div class=""sub_container"" >";

            html += @"<div style=""position:sticky; z-index:10; top:50%; left:calc(100% - 20pt); pointer-events: none; box-shadow: rgb(210, 210, 210) 4pt 4pt 6pt inset, rgb(255, 255, 255) -4pt -4pt 6pt inset; border-radius: 50%; height: 13pt; width: 13pt; background-color: rgb(255, 255, 255);  ""></div>";
            html += @"<div class=""wrapper""  >" + "\r\n";

            
            html += @"<div style=""background : linear-gradient(rgba(0,0,0,0.7), rgba(0,0,0,0.2)); width: 100%; height: 5pt; position:fixed; z-index:10; top:0; pointer-events: none; ""></div>";
            html += @"<div style=""background : linear-gradient(rgba(0,0,0,0.2), rgba(0,0,0,0)); width: 100%; height: 2pt; position:fixed; z-index:10; top:5pt; pointer-events: none ""></div>";
            html += @"<div id=""board_artist"" style=""display:none; position:fixed; z-index:20; top:0 ;""><font size=""24pt"" color=white>test</font></div>";

            ////test
            ////test
            html += @"<input id=""open_optional"" type=""checkbox"">";
            html += @"<div id = ""menu"" >";
            html += @"<nav>";
            html += @"<ul>";
            int n = 0;
            html += @"<li onclick=""close_optional();"" style=""font-family: 'Segoe MDL2 Assets' "" >&#xe8bb</li>";
            html += @"<br>";
            html += @"<li onclick=""test(-1); close_optional();"" ><a href=""#p2"">ALL</a></li>";
            foreach (var x in genreList)
            {
                if (x !="")  html += $@"<li onclick=""test({n}); close_optional();"" >{x}</li>";
                ++n;
            }
            html += @"</ul>";
            html += @"</nav>";
            html += @"</div>";

            //カードの冒頭の空白
            //html += @"<div style=""width:100vw; height:0pt;display : inline-block;"" ></div>";

            //左右の余白
            html += @"<div class=""wrapper2"" style=""padding: 4pt 18pt 0 13pt;"" id=""wrapper2""  >";

            foreach (AlbumInfo info in db.list)
            {
                int hash = info.albumArtist.GetHashCode();
                string dbHash = info.GetHashCode().ToString("X8");
                int n2 = info.filePath.LastIndexOf('/');
                string s = info.filePath.Remove(n2);   //   最後の / の出現位置までをキープして、残りは削除
                string imageCacheFileName = @"./Temp/ImageCache/" + System.IO.Path.GetFileName(s) + ".jpg";
                string s2 = info.albumTitle.Replace("'", @"\'");
                s2 = s2.Replace(@"""", "&quot;");
                string s3= info.albumArtist.Replace("'", @"\'"); 
                html += $@"<div class=""card_group"" id=""{hash}""  >";
                html += $@"<section class=""card"" id=""{info.genreIndex}""  style=""display : inline-block;"">" + "\r\n";

                html += @"<figure class=""highlight"">";

                html += $@"<img class=""card_image"" onload=""finalImageLoad(this)"" onerror=""startImageLoadTimer(this)"" src=""{imageCacheFileName}"" alt=""""  onclick=""onImageClick('{s2}', '{dbHash}')"" draggable=""false"" >" + "\r\n";
                //html += $@"<figcaption class=""caption"" onclick=""this.parentNode.getElementsByClassName('card_image')[0].click();""><b>{info.albumArtist}</b><br><br>{info.albumTitle}</figcaption>";
                html += $@"<figcaption class=""caption"" ><b>{info.albumArtist}</b><br><br>{info.albumTitle}</figcaption>";
                html += @"</figure>";
                html += @"<div class=""card_content"">";
                html += $@"<p class=""card_text"">{info.albumTitle}</p> ";
                html += @"</div>";
                
                html += @"</section>" + "\r\n";
                html += @"</div>" + "\r\n";
            }
            for (int i = 0; i < 6; ++i)
            {
                html += $@"<section class=""cardx"" id=""c{i}"" name=""c{i}"" height=""0px"">";
                //html += $@"<img class=""card-img"" src=""{imageCacheFileName}"" alt=""""  onclick=""onImageClick('{info.albumTitle}')""  >";
                html += @"<div class=""card-content"">";
                //html += $@"<p class=""card-text"">{info.albumTitle}</p> ";
                html += @"</div>";
                html += @"</section>";
            }

            //左右の余白
            html += @"</div>";

            html += @"</div>"; //wrapper
            html += @"</div>"; // sub_container
            html += @"<section class=""sub_container"" id=""p2""  style=""overflow-y:hidden;  display:inline-block; background-color:#EDF1F5;"" >";

            ////////////////// pag2
            //////////////////
            ///
            html += @"<div style=""position: relative; top:10pt; left:20pt; display:inline-block;"">";
            html += @"<div style=""font-family:Segoe UI Semibold; font-size:18pt;color:#1C3B61;position: relative; top:0pt; left:0pt;  display:block;"">Genre</div>";
            
            html += @"<select class=""list_box_2"" onchange=""resetArtistList(this)"" size=60 name=""genre_name"" style=""position: relative; top:12pt; width: 260pt; height:85%;"" >";
            html += $@"<option class=""option_list"" value='0' "">[All genre]</option>";
            foreach (var x in genreList)
            {
                html += $@"<option class=""option_list"" value='1' "">{x}</option>";
            }
            html += @"</select>";
            html += @"</div>";

            html += @"<div style=""position: relative; top:10pt; left:80pt; display:inline-block;"">";
            html += @"<div style=""font-family:Segoe UI Semibold; font-size:18pt;color:#1C3B61;position: relative; top:0pt; left:0pt;  display:block;"">Artist</div>";
            html += @"<select class=""list_box_2"" id=""artistListForGenre"" onchange=""artist_selected(this.value)""  size=60 name=""artist_name"" style=""position: relative; top:12pt; left:0pt; width: 340pt; height:85%;"" >";
            html += @"</select>";
            html += @"</div>";


            html += @"</section>";

            html += @"</div>"; // super_container


            html += @"<script type=""text/javascript"">";
            html += @"function set_board_artist(name) {var board = document.getElementById('board_artist'); board.textContent =name; } ";
            html += @"function onImageClick(albumArtist, hash) { set_board_artist(albumArtist); window.chrome.webview.postMessage( JSON.stringify({action:'click', id:hash}) ); }" + "\r\n";
            html += @"</script>";

            html += @"</body>";
            html += @"</html>";

            File.WriteAllText(fileName, html);
            return html;
        }


        private void Button_Main_Click(object sender, RoutedEventArgs e)
        {
            if (readTask != null) readTask.Wait();
            navigation.Navigate(pageAll);
            buttonMain.BorderBrush = SystemColors.HighlightBrush; //Brushes.Black;
            //buttonArtist.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = Brushes.Transparent;
            //buttonGenre.Visibility = Visibility.Visible;
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            navigation.Navigate(pageSettings);
            buttonMain.BorderBrush = Brushes.Transparent;
            //buttonArtist.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = Brushes.Transparent;
            buttonSettings.BorderBrush = SystemColors.HighlightBrush;
            //buttonGenre.Visibility = Visibility.Hidden;
        }

        public void Button_Current_Click(object sender, RoutedEventArgs e)
        {
            if (readTask != null) readTask.Wait();
            navigation.Navigate(pageTracks);
            buttonMain.BorderBrush = Brushes.Transparent;
            //buttonArtist.BorderBrush = Brushes.Transparent;
            buttonCurrent.BorderBrush = SystemColors.HighlightBrush;
            buttonSettings.BorderBrush = Brushes.Transparent;
            //buttonGenre.Visibility = Visibility.Hidden;
        }

        public void Button_Genre_Click(object sender, RoutedEventArgs e)
        {
            if (readTask != null) readTask.Wait();
            if (!pageAll.IsEnabled) return;
            Type t = pageAll.GetType();
            System.Reflection.MethodInfo mi = t.GetMethod("onGenreClick");
            mi.Invoke(pageAll, null);
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

        private async static void downloadFile(Uri sourceUri, string outputFilePath, string tmpFileName, CancellationToken token)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(10000);
            HttpResponseMessage res = null;
            bool isCopied = false;

            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    res = await client.GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, token);
                }
                catch
                {
                    Thread.Sleep(200);
                    continue;
                }

                FileStream fileStream = null;
                try
                {
                    using (fileStream = File.Create(tmpFileName))
                    {
                        Stream httpStream;
                        try
                        {
                            using (httpStream = await res.Content.ReadAsStreamAsync())
                            {
                                try
                                {
                                    httpStream.CopyTo(fileStream); //たまにタイム・アウトする
                                }
                                catch
                                {
                                    fileStream.Flush();
                                    Thread.Sleep(200);
                                    continue;
                                }
                                isCopied = true;
                                break;

                            } // using
                        }
                        catch
                        {
                            Thread.Sleep(200);
                            continue;
                        }
                    } //using
                }
                catch
                {
                    Thread.Sleep(200);
                    continue;
                }
            }
            if (isCopied)
            {
                await Task.Run(() => System.IO.File.Move(tmpFileName, outputFilePath));
            }
        }

        private  static void CopyImageFile(AlbumDb db, string ip, string localCachePath, CancellationToken token)
        {
            //string ip = getIp();

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(10000);
            //Task<HttpResponseMessage> res = null;
            

            foreach (AlbumInfo info in db.list)
            {
                string path = info.filePath;

                int n = path.LastIndexOf('/');
                string s = path.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
                s = s.Replace("=", "%3D");
                
                Uri sourceUri = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);
                string imageCacheFileName = info.albumCacheImagePath;

                try
                {
                    
                    if (File.Exists(imageCacheFileName)) continue;
                    
                }
                catch
                {
                    continue;
                }
                //GetImageCacheDirectory
                string tmpFile = System.IO.Path.GetTempFileName();
                downloadFile(sourceUri, imageCacheFileName, tmpFile, token);
                Thread.Sleep(10);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //save setting
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
        }

        private void Back15Button_Click(object sender, RoutedEventArgs e)
        {
            _sendMpd(getIp(), "seekcur -15", false);
        }

        private void Skip15Button_Click(object sender, RoutedEventArgs e)
        {
            _sendMpd(getIp(), "seekcur +15", false);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //pageMain = new PageMainPlayer(this);
            pageAll = new PageAlbumsWebView(this);
            pageSettings = new PageSettings(this);
            pageTracks = new PageCurrent(this);
            pageOpening = new PageOpening(this);
            navigation.Navigate(pageOpening);
            cancellationSource = new CancellationTokenSource();

            
            await Task.Run(() => CreateAlbumDb(getIp(), this));

            //(string artist, string album, string title)? v;
            //v = await Task.Run(() => MainWindow.GetVolumioStatusSync(getIp(), true));

            navigationBar.Visibility = Visibility.Visible;
            operatingBar.Visibility = Visibility.Visible;

            navigation.Navigate(pageAll);
            pageAll.Visibility = Visibility.Hidden;
            pageAll.Visibility = Visibility.Visible;

            //setSelectedAlbum(v.album);
            //setSelectedArtist(v.artist);
            //string imageFileName = GetAlbumCacheImageFilePathAndName(v.artist, v.album);
            //BitmapImage bmp = getBitmapImageFromFileName(imageFileName);
            //setSelectedAlbumImage(bmp);
        }
    }
}
