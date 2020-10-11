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

        private void init_artist_list()
        {
            AlbumDb db = mainWindow.getAlbumDb();
            var art = (from b in db.list
                       select b.albumArtist).Distinct(); // artistの重複を除去
            foreach (var c in art)
            {
                artistList.Items.Add(c);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (artistList.Items.Count == 0)
            {
                init_artist_list();
            }
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

        private void ArtistList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            albumList.Clear();
            albumImages.Items.Clear();
            mainWindow.setCursoredArtist(artistList.SelectedItem.ToString());

            int size1 = 160;
            int size2 = 160;
            int [] size_table = { 132, 160, 196, 240};
            size1 = size2 = size_table[mainWindow.getAlbumArtSize()];

            var albums = mainWindow.GetCursoredArtistAlbums();
            foreach (var x in albums)
            {
                string path = MainWindow.GetAlbumCacheImageFilePathFromOriginalFilePath(x.filePath);
                BitmapImage bmp = mainWindow.getBitmapImageFromFileName(path);
                albumImages.Items.Add(new CardItem() { AlbumImage = bmp, AlbumTitle = x.albumTitle, AlbumCardWidth = size1, AlbumImageWidth = size2, AlbumImageHeight = size2 });
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

            mainWindow.Button_Current_Click(null, null);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            cancellationSource.Cancel();
        }
    }
}
