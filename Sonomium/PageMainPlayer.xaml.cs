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
using Microsoft.Web.WebView2.Core;
using System.Text.Json;

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
            this.webView.WebMessageReceived += onWebViewImageClicked;
            this.webView.NavigationCompleted += onWebViewNavigationCompleted;
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

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            await webView.ExecuteScriptAsync(@"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){ cards[i].style.display =""none"";} ");
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

            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName += "\\Sonomium\\albums.html";
            Uri uri = new Uri(fileName);
            webView.Source = uri;
          }

        private void ArtistList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            albumList.Clear();
            //albumImages.Items.Clear();
            webView.ExecuteScriptAsync(@"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){ cards[i].style.display =""none"";} ");
            mainWindow.setCursoredArtist(artistList.SelectedItem.ToString());

            int size1 = 160;
            int size2 = 160;
            int [] size_table = { 132, 160, 196, 240};
            size1 = size2 = size_table[mainWindow.getAlbumArtSize()];

            var albums = mainWindow.GetCursoredArtistAlbums();
            int n = albums[0].albumArtist.GetHashCode();
            foreach (var x in albums)
            {
                string path = MainWindow.GetAlbumCacheImageFilePathFromOriginalFilePath(x.filePath);
                BitmapImage bmp = mainWindow.getBitmapImageFromFileName(path);
                //albumImages.Items.Add(new CardItem() { AlbumImage = bmp, AlbumTitle = x.albumTitle, AlbumCardWidth = size1, AlbumImageWidth = size2, AlbumImageHeight = size2 });
                webView.ExecuteScriptAsync($@"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){{ if (cards[i].id == {n} ) cards[i].style.display =""block"";}} ");
            }
        }

        private void onWebViewNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            webView.ExecuteScriptAsync(@"var cards = document.getElementsByClassName('card_group');var len = cards.length;for (var i = 0; i < len; ++i){ cards[i].style.display =""none"";} ");
            webView.Visibility = Visibility.Visible;
        }

        private void onWebViewImageClicked(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonString = e.TryGetWebMessageAsString();
            ActionFromWebView aa = JsonSerializer.Deserialize<ActionFromWebView>(jsonString);

            if (aa.action == "click")
            {
                (string artistFromId, string albumFromId) = mainWindow.getAlbumArtistAndNameById(aa.id);

                mainWindow.setSelectedArtist(artistFromId);
                mainWindow.setSelectedAlbum(albumFromId);

                string imageFileName = mainWindow.GetAlbumCacheImageFilePathAndName(artistFromId, albumFromId);
                BitmapImage bmp = mainWindow.getBitmapImageFromFileName(imageFileName);
                mainWindow.setSelectedAlbumImage(bmp);
                mainWindow.Button_Current_Click(null, null);
            }
        }

        private void AlbumImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            if (albumImages.SelectedItem == null) return;

            string artist = artistList.SelectedItem.ToString();
            mainWindow.setSelectedArtist(artist);

            CardItem ci = (CardItem)albumImages.SelectedItem;
            string s = ci.AlbumTitle;
            mainWindow.setSelectedAlbumImage(ci.AlbumImage);
            mainWindow.setSelectedAlbum(s);

            mainWindow.Button_Current_Click(null, null);
            */
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            cancellationSource.Cancel();
        }
    }
}
