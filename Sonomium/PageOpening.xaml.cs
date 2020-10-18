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

namespace Sonomium
{
    /// <summary>
    /// PageOpening.xaml の相互作用ロジック
    /// </summary>
    public partial class PageOpening : Page
    {
        private MainWindow mainWindow;

        public PageOpening(MainWindow _mainWindow)
        {
            mainWindow = _mainWindow;
            InitializeComponent();
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            string html = "";
            html += @"<html>";
            html += @"<head>";
            html += @"<style>";

            html += @".ball_wrapper { position: absolute; top: 50%; left:50%; }";
            html += @".title { position: absolute; top:35%;  left:50%;  }";
            html += @"body { background-color:#EDF1F5; }";
            html += @".ball {";
            html += @"  border-radius: 50%;";
            html += @"   background-color: #333;";
            html += @"    position: absolute;";
            html += @"   width: 6pt;";
            html += @"  height: 6pt;";
            html += @"    opacity: .2;";
            html += @"    transform: translate(-50%, -50%);";
            html += @"    box-shadow: 0 10px 25px 0 rgba(0, 0, 0, .5);";
            html += @"}";
            html += @".ball_1 {   animation: ball_anim 2.8s linear 0s infinite;  left:-70pt; }";
            html += @" .ball_2 {  animation: ball_anim 2.8s linear 0.6s infinite;  left:-30pt; }";
            html += @" .ball_3 {  animation: ball_anim 2.8s linear 1.2s infinite; left:10pt; }";
            html += @".ball_4 {  animation: ball_anim 2.8s linear 1.8s infinite; left:50pt; }";
            html += @"@keyframes ball_anim {";
            html += @"  0% { width: 22pt; height: 22pt; opacity: 0.8; }";
            html += @"  70% { width: 8pt; height: 8pt; opacity: 0.3; }";
            html += @"  100% { width: 6pt; height: 6pt; opacity: .2;  }";
            html += @"}";

            html += @"</style>";
            html += @"</head>";
            html += @"<body>";

            html += @"<div class=""title"">";
            html += @"<div style=""font-size:30pt; letter-spacing: 10pt; transform: translate(-50%, -50%); color:#1C3B61; position: absolute; font-family:'Segoe UI Semibold'; "">SONOMIAUX</div>";
            html += @"</div>";
            html += @"<div class=""ball_wrapper"">";
            html += @"   <span class=""ball ball_1"" ></span>";
            html += @"   <span class=""ball ball_2"" ></span>";
            html += @"   <span class=""ball ball_3"" ></span>";
            html += @"   <span class=""ball ball_4"" ></span>";
            html += @"</div>";

            html += @"</body>";
            html += @"</html>";
            webView.NavigateToString(html);// = html;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeAsync();
        }
    }
}
