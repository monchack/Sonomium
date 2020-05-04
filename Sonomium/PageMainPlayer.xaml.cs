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

        public class CardItem
        {
            public BitmapImage AlbumImage { get; set; }
            public string AlbumTitle { get; set; }
        }

        public PageMainPlayer(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
            albumList = new List<string>();
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

            string album = mainWindow.sendMpd("find albumartist " + "\"" + artistList.SelectedItem.ToString() + "\"");
            StringReader sr = new StringReader(album);
            string line;
            string nextAlbum = "";
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("Album:"))
                {
                    string newItem = line.Replace("Album: ", "");
                    if (newItem == "") newItem = "(Unknown)";
                    if (!albumList.Contains(newItem))
                    {
                        albumList.Add(newItem);// WriteLine(line);

                        nextAlbum = newItem; ///////////////
                    }
                }
                if (line.Contains("file: "))
                {
                    if (nextAlbum != "")
                    {
                        BitmapImage bmp = getAlbumImage(line.Replace("file: ", ""));
                        albumImages.Items.Add(new CardItem() { AlbumImage = bmp, AlbumTitle=nextAlbum });
                        nextAlbum = "";
                    }
                }


            }
        }
    }
}
