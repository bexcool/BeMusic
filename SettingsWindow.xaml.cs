using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;

namespace BeMusic
{
    /// <summary>
    /// Interakční logika pro SettingWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        OpenFileDialog chooseImage = new OpenFileDialog();

        public SettingsWindow()
        {
            InitializeComponent();

            chooseImage.Filter = "Image file (*.png;*.jpg) | *.png;*.jpg";
            chooseImage.FileOk += ChooseImage_FileOk;

            List<string> settingsData = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt").ToList();

            BackgroundImageBrush.ImageSource = new BitmapImage(new Uri(settingsData[1]));
        }

        private void ChangeImageButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(BackgroundImageBrush.ImageSource);
            chooseImage.ShowDialog();
        }

        private void ChooseImage_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BackgroundImageBrush.ImageSource = new BitmapImage(new Uri(chooseImage.FileName));

            List<string> settingsData = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt").ToList();

            // 1 Background image

            settingsData[1] = BackgroundImageBrush.ImageSource.ToString();

            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt", settingsData);
        }

        private void DefaultBgImageButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundImageBrush.ImageSource = new BitmapImage(new Uri(@"pack://application:,,,/BeMusic;component/Images/winter-mountain-snow-4k-01.jpg"));

            List<string> settingsData = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt").ToList();

            // 1 Background image

            settingsData[1] = BackgroundImageBrush.ImageSource.ToString();

            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt", settingsData);
        }

        private void Label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://icons8.com");
        }

        private void Label_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://bexcool.eu");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}
