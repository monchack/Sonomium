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
        private double currentPos = 0;
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
            public string AlbumImageBackup { get; set; }
            public bool IsVisible { get; set; }
        }

        public PageAllAlbums(MainWindow _mainWindow) 
        {
            InitializeComponent();
            mainWindow = _mainWindow;
            cancellationSource = new CancellationTokenSource();
        }

        private async void downloadFile(string uri, string outputFilePath, CancellationToken token)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(3000);
            HttpResponseMessage res;
            string dbg = outputFilePath + " ";
            try
            {
                bool toDelete = false;
                res = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
                if (!res.IsSuccessStatusCode)
                {
                    return;
                }

                FileStream fileStream = null;
                try
                {
                    using (fileStream = File.Create(outputFilePath))
                    {
                        using (Stream httpStream = await res.Content.ReadAsStreamAsync())
                        {
                            if (!httpStream.CanRead)
                            {
                                fileStream.Dispose();
                                File.Delete(outputFilePath);
                                return;
                            }
                            await httpStream.CopyToAsync(fileStream);
                        }
                    }
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
            }
            catch (TaskCanceledException)
            {
                // http cancel
            }
        }

        private BitmapImage getAlbumImage(string uri, int size)
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

            if (!File.Exists(imageCacheFileName))
            {
                downloadFile(@"http://" + ip + @"/albumart?path=/mnt/" + s, imageCacheFileName, cancellationSource.Token);
            }
            else
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.DownloadCompleted += (sender, args) =>
                    {
                        albumImages.Items.Refresh();
                    };
                    if (mainWindow.getAlbumArtResolution() == 0) bitmap.DecodePixelWidth = size;
                    bitmap.UriSource = new Uri(@"file://" + imageCacheFileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    // // キャッシュにファイルはあったが、bitmap作成に失敗
                    //fileName += "!";
                    try
                    {
                        File.Delete(imageCacheFileName);
                        downloadFile(@"http://" + ip + @"/albumart?path=/mnt/" + s, imageCacheFileName, cancellationSource.Token);
                    }
                    catch
                    {
                    }
                }
            }
            // キャッシュがなかったか、キャッシュからのbitmap生成に失敗した
            BitmapImage bitmap2 = new BitmapImage();
            bitmap2.BeginInit();
            bitmap2.DownloadCompleted += (sender, args) =>
            {
                albumImages.Items.Refresh();
            };
            bitmap2.UriSource = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);
            bitmap2.EndInit();
            return bitmap2;
        }

        private string getAlbumImageFromCache(string uri)
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

            if (!File.Exists(imageCacheFileName))
            {
                return "";
            }
            return imageCacheFileName;
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

            await Task.Run(() =>
            {

                foreach (AlbumInfo info in db.list)
                {
                    BitmapImage s = getAlbumImage(info.filePath, size1);
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        albumImages.Items.Add(new CardItem() { AlbumImage = s, IsVisible = true, AlbumTitle = info.albumTitle, AlbumCardWidth = size1, AlbumImageWidth = size2, AlbumImageHeight = size2 });
                    }));
                }
            });
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (albumArtSize != mainWindow.getAlbumArtSize() || albumArtResolution != mainWindow.getAlbumArtResolution())
            {
                Set_Album_Images();
                albumArtSize = mainWindow.getAlbumArtSize();
                albumArtResolution = mainWindow.getAlbumArtResolution();
            }

            var border = VisualTreeHelper.GetChild(albumImages, 0) as Border;
            if (border != null)
            {
                var scrollViewer = border.Child as ScrollViewer;
                scrollViewer.ScrollChanged += scrollViewer_ScrollChanged;
            }
        }

        private void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }

        void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var border = VisualTreeHelper.GetChild(albumImages, 0) as Border;
            double offset = 0;
            if (border == null) return;
            
            var scrollViewer = border.Child as ScrollViewer;
            offset = scrollViewer.VerticalOffset / scrollViewer.ScrollableHeight;

            int count = albumImages.Items.Count;
           

            for (int i = 0; i < count; ++i)
            {
                var item = albumImages.Items.GetItemAt(i) as CardItem;
                double d = (double)i / count;
                if (d > offset - 0.1 && d < offset + 0.1)
                {
                    // visibile
                    //var item = albumImages.Items.GetItemAt(i) as CardItem;
                    //item.AlbumImage = item.AlbumImageBackup;
                    //item.IsVisible = true;
                    
                }
                else
                {
                   //tem.AlbumImage = null;
                   // item.IsVisible = false;
                }


            }
            //if (currentPos == 0) albumImages.Items.Refresh();
            //if (currentPos - offset < -0.1) albumImages.Items.Refresh();
            //if (offset - currentPos > 0.1) albumImages.Items.Refresh();
            currentPos = offset;
            //albumImages.Items.Refresh();
            ///DoEvents();
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
            System.GC.Collect();
            
        }
    }
}
