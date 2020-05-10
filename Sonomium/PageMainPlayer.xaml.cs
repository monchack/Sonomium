﻿using System;
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

namespace Sonomium
{
    /// <summary>
    /// PageMainPlayer.xaml の相互作用ロジック
    /// </summary>
    public partial class PageMainPlayer : Page
    {
        private MainWindow mainWindow;
        private List<string> albumList;
        private List<int> albumTimeList;
        private CancellationTokenSource cancellationSource;

        public class CardItem
        {
            public BitmapImage AlbumImage { get; set; }
            public string AlbumTitle { get; set; }
            public int AlbumImageWidth { get; set; }
            public int AlbumImageHeight { get; set; }
            public int AlbumCardWidth { get; set; }
        }

        public PageMainPlayer(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
            albumList = new List<string>();
            albumTimeList = new List<int>();
            cancellationSource = new CancellationTokenSource();
        }

        private void update_album_list()
        {
            string albumartist = mainWindow.sendMpd("list albumartist");

            StringReader sr = new StringReader(albumartist);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("AlbumArtist:"))
                {
                    string newItem = line.Replace("AlbumArtist: ", "");
                    if (newItem == "") newItem = "(Unknown)";
                    artistList.Items.Add(newItem);
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            update_album_list();
            if (mainWindow != null)
            {
                string s = mainWindow.getCursoredArtist();
                for (int i = 0; i < artistList.Items.Count; ++i)
                {
                    var v = artistList.Items.GetItemAt(i);
                    if (v.ToString() == s)
                    {
                        artistList.SelectedIndex = i;
                        artistList.ScrollIntoView(v);
                        break;
                    }
                }
            }
        }

        private async void downloadFile(string uri, string outputFilePath, CancellationToken token)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(3000);
            HttpResponseMessage res = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);

            using (var fileStream = File.Create(outputFilePath))
            {
                try
                {
                    using (var httpStream = await res.Content.ReadAsStreamAsync())
                    {
                        httpStream.CopyTo(fileStream);
                        fileStream.Flush();
                    }
                }
                catch (TaskCanceledException ) 
                {
                    // timeout
                    fileStream.Flush();
                    File.Delete(outputFilePath);
                }
            }
        }

        private BitmapImage getAlbumImage(string uri)
        {
            if (uri == "")
            {
                return null;
            }
            string ip = mainWindow.getIp();

            int n = uri.LastIndexOf('/');
            string s = uri.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
            s = s.Replace("=", "%3D");

            //キャッシュ
            string fileName = System.IO.Path.GetFileName(s) + ".jpg";
            string imageCacheFileName = mainWindow.GetImageCacheDirectory() + fileName;
            Uri sourceUri = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);

            if (!File.Exists(imageCacheFileName))
            {
                downloadFile(@"http://" + ip + @"/albumart?path=/mnt/" + s, imageCacheFileName, cancellationSource.Token);
            }

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(@"file://" + imageCacheFileName);
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
            }
            BitmapImage bitmap2 = new BitmapImage();
            bitmap2.BeginInit();
            bitmap2.UriSource = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);
            bitmap2.EndInit();
            return bitmap2;
        }

        private void ArtistList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            albumList.Clear();
            albumImages.Items.Clear();
            mainWindow.setCursoredArtist(artistList.SelectedItem.ToString());

            string album = mainWindow.sendMpd("find albumartist " + "\"" + artistList.SelectedItem.ToString() + "\"");
            StringReader sr = new StringReader(album);
            string line;
            string nextAlbum = "";

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


            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("Album:"))
                {
                    string newItem = line.Replace("Album: ", "");
                    if (newItem == "") newItem = "(Unknown)";
                    if (!albumList.Contains(newItem))
                    {
                        albumList.Add(newItem);
                        nextAlbum = newItem;
                    }
                }
                if (line.Contains("file: "))
                {
                    if (nextAlbum != "")
                    {
                        BitmapImage bmp = getAlbumImage(line.Replace("file: ", ""));
                        albumImages.Items.Add(new CardItem() { AlbumImage = bmp, AlbumTitle=nextAlbum, AlbumCardWidth=size1, AlbumImageWidth=size2, AlbumImageHeight=size2 });
                        nextAlbum = "";
                    }
                }
                if (line.Contains("Time: "))
                {
                    string s = line.Replace("Time: ", "");
                    int n = Int32.Parse(s);
                    albumTimeList.Add(n);
                   // TimeSpan ts = new TimeSpan(0,0,n);
                    //albumTimeList.Add(s.ToLowerInvariant)
                }
            }
        }

        private void AlbumImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            if (albumImages.SelectedItem == null) return;

            string artist = artistList.SelectedItem.ToString();
            mainWindow.setSelectedArtist(artist);

            CardItem ci = (CardItem)albumImages.SelectedItem;
            string s = ci.AlbumTitle;
            mainWindow.setSelectedAlbumImage(ci.AlbumImage);
            mainWindow.setSelectedAlbum(s);

            //mainWindow.addSelectedAlbuomToQue(3, false);
            mainWindow.Button_Current_Click(null, null);


        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            cancellationSource.Cancel();
        }
    }
}
