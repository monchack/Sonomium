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

using System.IO;


namespace Sonomium
{
    /// <summary>
    /// PageCurrent.xaml の相互作用ロジック
    /// </summary>
    public partial class PageCurrent : Page
    {
        private MainWindow mainWindow;
        private List<string> trackTitleList;
        private List<int> trackTimeList;
        private List<string> trackFileList;

        public PageCurrent(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
            trackTitleList = new List<string>();
            trackFileList = new List<string>();
            trackTimeList = new List<int>();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null) return;
            if (mainWindow.getSelectedAlbum() == "") return;
            string s = mainWindow.getSelectedAlbum();
            //string s = artistList.SelectedItem.ToString() + "\" " + "album " + "\"" + ci.AlbumTitle;
            string track = mainWindow.sendMpd("find albumartist " + "\"" + s + "\"");
            StringReader sr = new StringReader(track);

            albumImage.Source = mainWindow.getSelectedAlbumImage();

            string line;
            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("file: "))
                {
                    string file = line.Replace("file: ", "");
                    trackFileList.Add(file);
                    trackTimeList.Add(-1); // 仮の登録
                    string title = "Track " + (i+1).ToString();
                    trackTitleList.Add(title); // 仮の登録
                    ++i;
                }
                if (line.Contains("Time: "))
                {
                    string time = line.Replace("Time: ", "");
                    int n = Int32.Parse(time);
                    trackTimeList[i-1] = n;
                }
                if (line.Contains("Title:"))
                {
                    string title = line.Replace("Title: ", "");
                    trackTitleList[i - 1] = title;
                }
            }
            for (i = 0; i < trackFileList.Count; ++i)
            {
                string title = trackTitleList[i];
                string file = trackFileList[i];
                trackList.Items.Add(new { Text = title, Value = file, });
            }
        }
    }
}
