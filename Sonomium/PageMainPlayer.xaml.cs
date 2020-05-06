using System;
using System.Collections.Generic;
using System.IO;
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
    /// PageMainPlayer.xaml の相互作用ロジック
    /// </summary>
    public partial class PageMainPlayer : Page
    {
        private MainWindow mainWindow;
        private List<string> albumList;
        private List<int> albumTimeList;

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
                    artistList.Items.Add(newItem);// WriteLine(line);
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

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(@"http://" + ip + @"/albumart?path=/mnt/" + s);
            bitmap.EndInit();
            return bitmap;
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

            int size1 = 200;
            int size2 = 200;
            if (mainWindow.getAlbumArtSize() == 0)
            {
                size1 = size2 = 120;
            }
            else if (mainWindow.getAlbumArtSize() == 2)
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
            //string s = artistList.SelectedItem.ToString() + "\" " + "album " + "\"" + ci.AlbumTitle;
            string s = ci.AlbumTitle;
            mainWindow.setSelectedAlbumImage(ci.AlbumImage);
            mainWindow.setSelectedAlbum(s);

            mainWindow.addSelectedAlbuomToQue();
        }
    }
}
