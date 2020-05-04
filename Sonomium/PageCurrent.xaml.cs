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

        public PageCurrent(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
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
            int i = 1;
            string title = "";
            string file = "";
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("file: "))
                {
                    file = line.Replace("file: ", "");
                    if (title != "")
                    {
                        trackList.Items.Add(new { Text = title, Value = file, });
                    }
                    title = "Track " + i.ToString();
                    ++i;
                }
                if (line.Contains("Title:"))
                {
                    title = line.Replace("Title: ", "");
                }
            }
            if (title != "")
            {
                trackList.Items.Add(new { Text = title, Value = file, });
            }

        }
    }
}
