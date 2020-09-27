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
using Microsoft.Web.WebView2.Core;
using System.Text.Json;

using System.IO;

namespace Sonomium
{
    /// <summary>
    /// PageAlbumsWebView.xaml の相互作用ロジック
    /// </summary>
    /// 

    class ArtistAndAlbum
    {
        public string albumTitle { get; set; }
        public string albumArtist { get; set; }
    }

    public partial class PageAlbumsWebView : Page
    {
        private MainWindow mainWindow;

        private void onWebViewNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            this.webView.Visibility = Visibility.Visible;
        }

        private void onWebViewImageClicked(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonString = e.TryGetWebMessageAsString();
            ArtistAndAlbum aa = JsonSerializer.Deserialize<ArtistAndAlbum>(jsonString);
            string s = aa.albumArtist;

            mainWindow.setSelectedArtist(aa.albumArtist);
            mainWindow.setSelectedAlbum(aa.albumTitle);

            string imageFileName = mainWindow.GetAlbumCacheImageFilePathAndName(aa.albumArtist, aa.albumTitle);
            BitmapImage bmp = mainWindow.getBitmapImageFromFileName(imageFileName);
            mainWindow.setSelectedAlbumImage(bmp);
            mainWindow.Button_Current_Click(null, null);
        }

        public PageAlbumsWebView(MainWindow _mainWindow)
        {
            InitializeComponent();

            mainWindow = _mainWindow;
            //InitializeAsync();
            this.webView.WebMessageReceived += onWebViewImageClicked;
            this.webView.NavigationCompleted += onWebViewNavigationCompleted;
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName += "\\Sonomium\\albums.html";
            Uri uri = new Uri(fileName);
            webView.Source = uri;
        }
    }
}
