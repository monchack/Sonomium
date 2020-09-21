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
        
        public class TrackInfo
        {
            public string TrackTitle { get; set; }
            public string TrackDuration { get; set; }
            public string TrackFile { get; set; }
            public string TrackStatus { get; set; }
            public string TrackNumber { get; set; }
        }
        

        public PageCurrent(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null) return;
            if (mainWindow.getSelectedAlbum() == "") return;

            if (albumArtist.Text == mainWindow.getSelectedAlbum() && albumArtist.Text == mainWindow.getSelectedArtist())
            {
                return;
            }

            albumTitle.Text = mainWindow.getSelectedAlbum();
            albumArtist.Text = mainWindow.getSelectedArtist();
            albumImage.Source = mainWindow.getSelectedAlbumImage();

            trackList.Items.Clear();

            var tracks = mainWindow.GetCurrentAlbumTracks();
            for (int i = 0; i < tracks.Count; ++i)
            {
                string title = tracks[i].trackTitle;
                string file = tracks[i].filePath;
                if (title == "") title = "Track " + (i + 1).ToString();
                string duration;
                int n = tracks[i].length;
                TimeSpan ts = new TimeSpan(0,0,n);
                if (n > 3600) duration = ts.ToString(@"h\:mm\:ss");
                else duration = ts.ToString(@"m\:ss");
                //string st = "\uF5B0"; // PlaySolid
                string st = "\uEC4F"; // MusicNote
                trackList.Items.Add(new TrackInfo() { TrackNumber=(i+1).ToString(), TrackTitle = title, TrackDuration = duration, TrackFile=file, TrackStatus=st });
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
