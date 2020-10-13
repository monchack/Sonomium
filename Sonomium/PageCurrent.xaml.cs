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

        private void update_artist_album((string artist, string album, string title)? t)
        {
            if (mainWindow == null) return;
            if (t == null && mainWindow.getSelectedAlbum() == "") return;

            trackList.Items.Clear();
            List<Sonomium.TrackInfo> tracks;

            if (t != null)
            {
                mainWindow.setSelectedArtist(t?.artist);
                mainWindow.setSelectedAlbum(t?.album);
                string imageFileName = mainWindow.GetAlbumCacheImageFilePathAndName(t?.artist, t?.album);
                BitmapImage bmp = mainWindow.getBitmapImageFromFileName(imageFileName);
                mainWindow.setSelectedAlbumImage(bmp);
            }
            albumTitle.Text = mainWindow.getSelectedAlbum();
            albumArtist.Text = mainWindow.getSelectedArtist();
            albumImage.Source = mainWindow.getSelectedAlbumImage();

            if (t != null) tracks = mainWindow.GetAlbumTracks((t?.artist, t?.album));
            else tracks = mainWindow.GetCurrentAlbumTracks();

            
            for (int i = 0; i < tracks.Count; ++i)
            {
                string title = tracks[i].trackTitle;
                string file = tracks[i].filePath;
                if (title == "") title = "Track " + (i + 1).ToString();
                string duration;
                int n = tracks[i].length;
                TimeSpan ts = new TimeSpan(0, 0, n);
                if (n > 3600) duration = ts.ToString(@"h\:mm\:ss");
                else duration = ts.ToString(@"m\:ss");
                //string st = "\uF5B0"; // PlaySolid
                string st = "\uEC4F"; // MusicNote
                trackList.Items.Add(new TrackInfo() { TrackNumber = (i + 1).ToString(), TrackTitle = title, TrackDuration = duration, TrackFile = file, TrackStatus = st });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            update_artist_album(null);
        }

        private void TrackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int n = trackList.SelectedIndex;
            if (n == -1)
            {
                return;
            }
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

        private async void SelectNowPlayed_Click(object sender, RoutedEventArgs e)
        {
            (string artist, string album, string title)? v;
            buttonGetServerPlaying.IsEnabled = false;
            v = await Task.Run(()=>MainWindow.GetVolumioStatusSync(mainWindow.getIp(), true));
            if (v != null)
            {
                update_artist_album(v);
            }
            buttonGetServerPlaying.IsEnabled = true;
        }
    }
}
