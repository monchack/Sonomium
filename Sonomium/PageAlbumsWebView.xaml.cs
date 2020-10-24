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

    class ActionFromWebView
    {
        public string action { get; set; }
        public string id { get; set; }
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

        public void onGenreClick()
        {
            webView.ExecuteScriptAsync(@"document.getElementById(""open_optional"").click();");
        }

        public void onCardSizeChanged()
        {
            int n = mainWindow.getAlbumArtSize();
            string card_class_name = "card_" + n.ToString();
            string cardx_class_name = "cardx_" + n.ToString();
            string img_class_name = "card_image_" + n.ToString();
            string highlight_class_name = "highlight_" + n.ToString();

            string html = "";
            html += $@"var cards_0; var cards_1; var cards_2; var cards_3; var len;";

            html += $@"cards_0 = document.querySelectorAll('.card_image_0');";
            html += $@"cards_0.forEach((elem) =>  {{  elem.className=""{img_class_name}"" }}); ";
            html += $@"cards_1 = document.querySelectorAll('.card_image_1');";
            html += $@"cards_1.forEach((elem) =>  {{  elem.className=""{img_class_name}"" }}); ";
            html += $@"cards_2 = document.querySelectorAll('.card_image_2');";
            html += $@"cards_2.forEach((elem) =>  {{  elem.className=""{img_class_name}"" }}); ";
            html += $@"cards_3 = document.querySelectorAll('.card_image_3');";
            html += $@"cards_3.forEach((elem) =>  {{  elem.className=""{img_class_name}"" }}); ";
            
            html += $@"cards_0 = document.querySelectorAll('.highlight_0');";
            html += $@"cards_0.forEach((elem) =>  {{  elem.className=""{highlight_class_name}"" }}); ";
            html += $@"cards_1 = document.querySelectorAll('.highlight_1');";
            html += $@"cards_1.forEach((elem) =>  {{  elem.className=""{highlight_class_name}"" }}); ";
            html += $@"cards_2 = document.querySelectorAll('.highlight_2');";
            html += $@"cards_2.forEach((elem) =>  {{  elem.className=""{highlight_class_name}"" }}); ";
            html += $@"cards_3 = document.querySelectorAll('.highlight_3');";
            html += $@"cards_3.forEach((elem) =>  {{  elem.className=""{highlight_class_name}"" }}); ";

            html += $@"cards_0 = document.querySelectorAll('.card_0');";
            html += $@"cards_0.forEach((elem) =>  {{  elem.className=""{card_class_name}"" }}); ";
            html += $@"cards_1 = document.querySelectorAll('.card_1');";
            html += $@"cards_1.forEach((elem) =>  {{  elem.className=""{card_class_name}"" }}); ";
            html += $@"cards_2 = document.querySelectorAll('.card_2');";
            html += $@"cards_2.forEach((elem) =>  {{  elem.className=""{card_class_name}"" }}); ";
            html += $@"cards_3 = document.querySelectorAll('.card_3');";
            html += $@"cards_3.forEach((elem) =>  {{  elem.className=""{card_class_name}"" }}); ";

            html += $@"cards_0 = document.querySelectorAll('cardx_0');";
            html += $@"cards_0.forEach((elem) =>  {{  elem.className=""{cardx_class_name}"" }}); ";
            html += $@"cards_1 = document.querySelectorAll('cardx_1');";
            html += $@"cards_1.forEach((elem) =>  {{  elem.className=""{cardx_class_name}"" }}); ";
            html += $@"cards_2 = document.querySelectorAll('cardx_2');";
            html += $@"cards_2.forEach((elem) =>  {{  elem.className=""{cardx_class_name}"" }}); ";
            html += $@"cards_3 = document.querySelectorAll('cardx_3');";
            html += $@"cards_3.forEach((elem) =>  {{  elem.className=""{cardx_class_name}"" }}); ";
            
            webView.ExecuteScriptAsync(html);
        }
    }
}
