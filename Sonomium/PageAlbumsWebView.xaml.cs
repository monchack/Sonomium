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

using System.IO;

namespace Sonomium
{
    /// <summary>
    /// PageAlbumsWebView.xaml の相互作用ロジック
    /// </summary>
    public partial class PageAlbumsWebView : Page
    {
        private MainWindow mainWindow;

        private void onWebViewImageClicked(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            MessageBox.Show(e.TryGetWebMessageAsString());
        }

        public PageAlbumsWebView(MainWindow _mainWindow)
        {
            InitializeComponent();

            mainWindow = _mainWindow;
            InitializeAsync();
            this.webView.WebMessageReceived += onWebViewImageClicked;
            //webView.EnsureCoreWebView2Async(null);
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
            html += @".card { width: 200px; height: 240px; background: #fff; border-radius: 5px; box-shadow: 0 2px 5px #ccc; float: left; text-align: center; margin: 8px;}";
            html += @".cardx { width: 200px; height: 0px; background: #fff; border-radius: 5px; box-shadow: 0 2px 5px #ccc; float: left; text-align: center; margin: 8px;}";
            html += @".card-img { border-radius: 5px 5px 0 0; width: 180px; height: 180px;  }";
            html += @".card-content { padding: 2px; }";
            html += @".card-title { font-size: 20px; margin-bottom: 20px; text-align: center; color: #333;}";
            html += @".card-text { color: #777; font-size: 12px;  word-wrap: break-word; text-align: left; }";
            html += @"</style>";
            html += @"</head>";
            html += @"<body>";
            html += @"<div class=""wrapper"">";

            foreach (AlbumInfo info in db.list)
            {
                //string imageFileOnTheServer 
                int n = info.filePath.LastIndexOf('/');
                string s = info.filePath.Remove(n);   //   最後の / の出現位置までをキープして、残りは削除
                //string imageCacheFileName = mainWindow.GetImageCacheDirectory() + System.IO.Path.GetFileName(s) + ".jpg";
                string imageCacheFileName = @"./Temp/ImageCache/" + System.IO.Path.GetFileName(s) + ".jpg";



                //t = getAndSetImage(info, size1, t);
                html += @"<section class=""card"">";
                html += $@"<img class=""card-img"" src=""{imageCacheFileName}"" alt=""""  onclick=""onImageClick('{info.albumTitle}')""  >";
                html += @"<div class=""card-content"">";
                html += $@"<p class=""card-text"">{info.albumTitle}</p> ";
                html += @"</div>";
                html += @"</section>" + "\r\n";
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

            html += @"</div>" + "\r\n";
                
            html += @"<script type=""text/javascript"">" + "\r\n";
            html += @"function onImageClick(albumTitle) { window.chrome.webview.postMessage(albumTitle); }" + "\r\n";

            html += @"function onResize() {" + "\r\n";
            html += @"if (document.getElementById(""c3"").style.left < 100) {" + "\r\n";
            html += @"document.getElementById(""c3"").style.visibility  =""hidden"";" + "\r\n";
            html += @"}" + "\r\n";
            html += @"}" + "\r\n";
            html += @"window.onresize = onResize" + "\r\n";

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
            //webView.Source = new Uri(@"https://www.google.com/");
            //webView.
            //InitializeAsync();
            //webView.NavigateToString(@"<html><body>test</body></html>");
            //AlbumDb db = mainWindow.getAlbumDb();

            generateHtml();

            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName += "\\Sonomium\\albums.html";
            Uri uri = new Uri(fileName);
            webView.Source = uri;
        }
    }
}
