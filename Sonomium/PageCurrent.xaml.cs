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

        public class TrackInfo
        {
            public string TrackTitle { get; set; }
            public string TrackDuration { get; set; }
            public string TrackFile { get; set; }
            public string TrackStatus { get; set; }
        }

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

            string s = "find albumartist " + "\"" + mainWindow.getSelectedArtist() + "\"" + " album " + "\"" + mainWindow.getSelectedAlbum() + "\"";
            string track = mainWindow.sendMpd(s);
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
                string duration;
                int n = trackTimeList[i];
                TimeSpan ts = new TimeSpan(0,0,n);
                if (n > 3600) duration = ts.ToString(@"h\:mm\:ss");
                else duration = ts.ToString(@"m\:ss");
                //string st = "\uF5B0"; // PlaySolid
                string st = "\uEC4F"; // MusicNote
                trackList.Items.Add(new TrackInfo() { TrackTitle = title, TrackDuration = duration, TrackFile=file, TrackStatus=st });
            }
        }

        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int n = trackList.SelectedIndex;
            if (mainWindow!=null) mainWindow.addSelectedAlbuomToQue(n+1, true);
        }

        private void Button_PlayNow_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null) mainWindow.addSelectedAlbuomToQue(1, true);
        }

        private void Button_PlayLater_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null) mainWindow.addSelectedAlbuomToQue(1, false);
        }
    }
}
