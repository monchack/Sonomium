using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Sonomium
{
    /// <summary>
    /// PageAllAlbums.xaml の相互作用ロジック
    /// </summary>
    public partial class PageAllAlbums : Page
    {
        private MainWindow mainWindow;
        private CancellationTokenSource cancellationSource;
        private int albumArtSize = -1;
        private int albumArtResolution = -1;

        public class CardItem
        {
            public BitmapImage AlbumImage { get; set; }
            public string AlbumTitle { get; set; }
            public int AlbumImageWidth { get; set; }
            public int AlbumImageHeight { get; set; }
            public int AlbumCardWidth { get; set; }
            public string AlbumArtist { get; set; }
            public bool IsVisible { get; set; }
        }

        public PageAllAlbums(MainWindow _mainWindow) 
        {
            InitializeComponent();
            mainWindow = _mainWindow;
        }

        private bool downloadFile(HttpResponseMessage h, string uri, string outputFilePath)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(3000);

            string dbg = outputFilePath + " ";
            bool toDelete = false;
            FileStream fileStream = null;
            try
            {
                using (fileStream = File.Create(outputFilePath))
                {
                    Task<Stream> httpStream;
                    using (httpStream = h.Content.ReadAsStreamAsync())
                    {
                        httpStream.Wait();
                        if (!httpStream.Result.CanRead)
                        {
                            fileStream.Dispose();
                            File.Delete(outputFilePath);
                            return false;
                        }

                        httpStream.Result.ReadTimeout = 3000;
                        try
                        {
                            httpStream.Result.CopyTo(fileStream); //たまにタイム・アウトする
                        }
                        catch
                        {
                            toDelete = true;
                        }
                    } // using
                } //using
            }
            catch
            {
                toDelete = true;
            }
            finally
            {
                if (toDelete)
                {
                    try
                    {
                        fileStream.Dispose();
                        File.Delete(outputFilePath);
                    }
                    catch
                    {
                    }
                }
            }
            if (toDelete) return false;
            return true;
        }

        private HttpResponseMessage prepareDownload(string uri, int size)
        {
            if (uri == "")
            {
                return null;
            }
            string ip = mainWindow.getIp();

            int n = uri.LastIndexOf('/');
            string s = uri.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
            string fileName = System.IO.Path.GetFileName(s) + ".jpg";
            string imageCacheFileName = mainWindow.GetImageCacheDirectory() + fileName;
            s = s.Replace("=", "%3D");
            Uri sourceUri = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);

            try
            {
                if (File.Exists(imageCacheFileName)) return null;
            }
            catch
            {
                return null;
            }

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(3000);
            Task<HttpResponseMessage> res = null;

            string uri2 = @"http://" + ip + @"/albumart?path=/mnt/" + s;

            try
            {
                res = client.GetAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, cancellationSource.Token);
                res.Wait();
            }
            catch
            {
                return null;
            }
            return res.Result;
        }

        private BitmapImage getAlbumImage(HttpResponseMessage h, string uri, int size, Task prevTask)
        {
            if (uri == "")
            {
                return null;
            }
            string ip = mainWindow.getIp();

            int n = uri.LastIndexOf('/');
            string s = uri.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
            string fileName = System.IO.Path.GetFileName(s) + ".jpg";
            string imageCacheFileName = mainWindow.GetImageCacheDirectory() + fileName;
            s = s.Replace("=", "%3D");
            //Uri sourceUri = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);
            BitmapImage bitmap = null;
            int i = 0;

            if (h != null)
            {
                bool downloaded = false;
                for (i = 0; i < 5; ++i)
                {
                    downloaded = downloadFile(h, @"http://" + ip + @"/albumart?path=/mnt/" + s, imageCacheFileName);
                    if (downloaded) break;
                }
                if (!downloaded) return null;
            }
            for (i = 0; i < 5; ++i)
            {
                try
                {
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    if (mainWindow.getAlbumArtResolution() == 0) bitmap.DecodePixelWidth = size;
                    bitmap.UriSource = new Uri(@"file://" + imageCacheFileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                catch
                {
                    // キャッシュにファイルはあったが、bitmap作成に失敗
                    bitmap = null;
                }
                if (bitmap != null) break;
            }
            if (bitmap == null)
            {
                return null;
            }
            return bitmap;
        }

        private async Task getAndSetImage(AlbumInfo info, int size, Task prevTask)
        {
            BitmapImage s = null;
            HttpResponseMessage h = prepareDownload(info.filePath, size);

            // イメージ取得中、並行して次のイメージ取得にすすんでもらう
            await Task.Run(() =>
            {
                s = getAlbumImage(h, info.filePath, size, prevTask);
            });
            if (prevTask != null)
            {
                //listbox に追加するのは、前のタスクでのlistbox追加が終わってから
                try
                {
                    prevTask.Wait();
                }
                catch
                {
                }
            }
            try
            {
                CardItem ci = new CardItem() { AlbumImage = s, AlbumArtist = info.albumArtist, IsVisible = true, AlbumTitle = info.albumTitle, AlbumCardWidth = size, AlbumImageWidth = size, AlbumImageHeight = size };
                this.Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        albumImages.Items.Add(ci);
                    }
                    catch
                    {
                    }
                }));
            }
            catch
            {
            }
        }

        private void Fix_Album_Images()
        {
            foreach(CardItem ci in albumImages.Items)
            {
                if (ci.AlbumImage == null)
                {
                    //HttpResponseMessage h = prepareDownload(ci.info.filePath, size);
                }
            }
        }

        private async void Set_Album_Images()
        {
            AlbumDb db = mainWindow.getAlbumDb();

            albumImages.Items.Clear();
            int size1 = 160;
            int size2 = 160;
            if (mainWindow.getAlbumArtSize() == 0)
            {
                size1 = size2 = 132;
            }
            else if (mainWindow.getAlbumArtSize() == 2)
            {
                size1 = size2 = 196;
            }
            else if (mainWindow.getAlbumArtSize() == 3)
            {
                size1 = size2 = 240;
            }

            Task t = null;

            // ここで一旦 Set_Album_Images は制御を戻しつつ、以下を続行する
            await Task.Run(() =>
            {
                foreach (AlbumInfo info in db.list)
                {
                    t = getAndSetImage(info, size1, t);
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cancellationSource = new CancellationTokenSource();
            if (albumArtSize != mainWindow.getAlbumArtSize() || albumArtResolution != mainWindow.getAlbumArtResolution())
            {
                Set_Album_Images();
                albumArtSize = mainWindow.getAlbumArtSize();
                albumArtResolution = mainWindow.getAlbumArtResolution();
            }
        }

        private void AlbumImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (albumImages.SelectedItem == null) return;

            CardItem ci = (CardItem)albumImages.SelectedItem;

            string artist = ci.AlbumArtist;
            mainWindow.setSelectedArtist(artist);
            
            string s = ci.AlbumTitle;
            mainWindow.setSelectedAlbumImage(ci.AlbumImage);
            mainWindow.setSelectedAlbum(s);

            mainWindow.Button_Current_Click(null, null);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            cancellationSource.Cancel();
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
