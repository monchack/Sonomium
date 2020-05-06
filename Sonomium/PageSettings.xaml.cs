﻿using System;
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
    /// PageSettings.xaml の相互作用ロジック
    /// </summary>
    public partial class PageSettings : Page
    {
        private MainWindow mainWindow;

        public PageSettings(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
        }

        private void IpBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Net.IPAddress ip;

            if (!System.Net.IPAddress.TryParse(ipBox.Text, out ip))
            {
                // invalid
            }
            if (mainWindow!=null) mainWindow.setIp(ipBox.Text);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                ipBox.Text = mainWindow.getIp();
                if (mainWindow.getAlbumArtSize() == 0) buttonAlbumArtSmall.IsChecked = true;
                else if (mainWindow.getAlbumArtSize() == 2) buttonAlbumArtLarge.IsChecked = true;
                else buttonAlbumArtNormal.IsChecked = true;
            }
            string s = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private void ButtonAlbumArt_Checked(object sender, RoutedEventArgs e)
        {
            if (buttonAlbumArtSmall.IsChecked == true) { mainWindow.setAlbumArtSize(0); }
            else if (buttonAlbumArtLarge.IsChecked == true) { mainWindow.setAlbumArtSize(2); }
            else mainWindow.setAlbumArtSize(1);
        }
    }
}
