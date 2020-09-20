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

        private void onWebViewImageClicked(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonString = e.TryGetWebMessageAsString();
            ArtistAndAlbum aa = JsonSerializer.Deserialize<ArtistAndAlbum>(jsonString);
            string s = aa.albumArtist;

            mainWindow.setSelectedArtist(aa.albumArtist);
            mainWindow.setSelectedAlbum(aa.albumTitle);
        }

        public PageAlbumsWebView(MainWindow _mainWindow)
        {
            InitializeComponent();

            mainWindow = _mainWindow;
            InitializeAsync();
            this.webView.WebMessageReceived += onWebViewImageClicked;
        }

        void generateHtml()
        {
            string html = "";
            AlbumDb db = mainWindow.getAlbumDb();

            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName += "\\Sonomium\\albums.html";

            html += @"<html>";
            html += @"<head>";
            html += @"<title></title>";
            html += @"<style>";
            html += @"body { overscroll-behavior : none;} ";
            html += @".wrapper {  display: flex; flex-wrap : wrap ;  flex-direction: row; justify-content: space-between; }";
            html += @".card { width: 16vw; min-width:160px; background: #fff; border-width: 0px; float: left; text-align: center; }";
            html += @".cardx { width: 16vw; min-width:160px; height: 0px; background: #fff; border-width: 0px; float: left; text-align: center; }";
            html += @".card_image { border-radius: 5px 5px 5px 5px; width: 15vw; min-width:150px; height: 15vw; min-height: 150px; box-shadow: 3pt 3pt 5pt gray ;}";
            html += @".card_content { padding: 8px 0px 16px 0px;  }";
            html += @".card-title { font-size: 20px; margin-bottom: 40px; text-align: center; color: #333;}";
            html += @".card_text { color: #777; height:28pt;  font-size: 12px;   text-align: left; margin: 0vw 0.5vw 0vw 0.5vw;  overflow : hidden;display: -webkit-box;-webkit-box-orient: vertical;-webkit-line-clamp: 2; }";
            html += @"</style>";
            html += @"</head>";
            html += @"<body>";
            html += @"<div class=""wrapper"">";

            foreach (AlbumInfo info in db.list)
            {
                //string imageFileOnTheServer 
                int n = info.filePath.LastIndexOf('/');
                string s = info.filePath.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
                string imageCacheFileName = @"./Temp/ImageCache/" + System.IO.Path.GetFileName(s) + ".jpg";

                html += @"<section class=""card"">";
                html += $@"<img class=""card_image"" src=""{imageCacheFileName}"" alt=""""  onclick=""onImageClick('{info.albumTitle}', '{info.albumArtist}')""  >";
                html += @"<div class=""card_content"">";
                html += $@"<p class=""card_text"">{info.albumTitle}</p> ";
                html += @"</div>";
                html += @"</section>";
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

            html += @"</div>";
                
            html += @"<script type=""text/javascript"">";
            html += @"function onImageClick(albumTitle, albumArtist) { window.chrome.webview.postMessage( JSON.stringify({albumTitle:albumTitle, albumArtist:albumArtist}) ); }" + "\r\n";
            html += @"</script>";

            html += @"</body>";
            html += @"</html>";

            File.WriteAllText(fileName, html);
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            generateHtml();

            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName += "\\Sonomium\\albums.html";
            Uri uri = new Uri(fileName);
            webView.Source = uri;
        }
    }
}
