using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Genius;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.Owin.Security.OAuth;

namespace BeMusic
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputAudio;
        private AudioFileReader audioFile;

        bool audioPlaying, timeSliderUserChanging = false, stopToChangeSound = false, repeatSong = false;

        DispatcherTimer timerUpdateCurrTime = new DispatcherTimer();
        DispatcherTimer timerCheckLastSounds = new DispatcherTimer();

        List<string> soundFileURLs = new List<string>();

        int currentSoundPlayingIndex = 0;

        float lastVolume = 0;

        Storyboard windowMouseEnterLeaveStoryBoard = new Storyboard();
        Storyboard musicCircle = new Storyboard();

        string currentPlaylist = "last_sounds.txt";

        //GeniusClient geniusClient;

        public MainWindow()
        {
            checkApplicationFiles();
            alreadyStarted();
            InitializeComponent();
            setUpAnimations();
            setUpGenius();

            RepeatImage.Opacity = 0.3;
            lastVolume = (float)soundVolume_Slider.Value;

            outputAudio = new WaveOutEvent();
            outputAudio.PlaybackStopped += outputAudio_PlaybackStopped;

            timerUpdateCurrTime.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerUpdateCurrTime.Tick += TimerUpdateCurrTime_Elapsed;
            timerUpdateCurrTime.Start();

            timerCheckLastSounds.Interval = new TimeSpan(0, 0, 1);
            timerCheckLastSounds.Tick += TimerCheckLastSounds_Elapsed;
            timerCheckLastSounds.Start();
        }

        public class PlaylistItem
        {
            public string Name { get; set; }

            public string creationDate { get; set; }

            public string URL { get; set; }
        }

        private async void setUpGenius()
        {
            

            //var geniusClient = new GeniusClient("q02Ct9XQ3WArvsffhy_CAfTpwtNF1QaQaHkppYzzSHADM16aSV6f3R3iRT9Z8Slw");

            //Console.WriteLine(await geniusClient.SongClient.GetSong(7996726).Result);
        }

        public void checkApplicationFiles()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic")) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic");

            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
            {
                var createFile = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt");
                createFile.Close();
            }
        }

        private void alreadyStarted()
        {
            int filesRemoved = 0;

            soundFileURLs.AddRange(Environment.GetCommandLineArgs());
            soundFileURLs.RemoveAt(0);

            List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

            for (int i = 0; i < lastSounds.Count; i++)
            {
                if (!File.Exists(lastSounds[i]))
                {
                    filesRemoved++;
                    lastSounds.RemoveAt(i);
                }
            }

            if (filesRemoved > 0)
            {
                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds);

                soundFileURLs.Clear();
                soundFileURLs.AddRange(lastSounds);

                currentSoundPlayingIndex -= filesRemoved;
            }

            if (soundFileURLs.Count == 1)
            {
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                {
                    if (lastSounds.Contains(soundFileURLs[0]))
                    {
                        lastSounds.Remove(soundFileURLs[0]);
                        lastSounds.Add(soundFileURLs[0]);
                    }
                    else
                    {
                        lastSounds.Add(soundFileURLs[0]);
                    }

                    File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds.ToArray());
                }
            }

            if (Process.GetProcessesByName("BeMusic").Length > 1)
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        private void playSound_button_Click(object sender, RoutedEventArgs e)
        {
            if (audioFile != null)
            {
                if (!audioPlaying)
                {
                    if (outputAudio.PlaybackState == PlaybackState.Stopped)
                    {
                        string filePath = soundFileURLs[currentSoundPlayingIndex];

                        if (File.Exists(filePath))
                        {
                            if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                            {
                                SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                                audioFile = new AudioFileReader(filePath);

                                if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                                {
                                    stopToChangeSound = true;
                                    outputAudio.Stop();
                                }

                                outputAudio.Init(audioFile);

                                audioFile.Volume = (float)soundVolume_Slider.Value;
                                soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                                TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                            }
                        }

                        soundTime_Slider.Value = 0;
                    }
                    audioFile.Volume = (float)soundVolume_Slider.Value;
                    PlayImage.Source = new BitmapImage(new Uri(@"/Images/pause_96px.png", UriKind.Relative));
                    audioPlaying = true;

                    musicCircle.Resume();

                    outputAudio.Play();
                }
                else
                {
                    PlayImage.Source = new BitmapImage(new Uri(@"/Images/icons8-play-96.png", UriKind.Relative));
                    audioPlaying = false;

                    musicCircle.Pause();

                    outputAudio.Pause();
                }
            }
        }

        private void outputAudio_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!stopToChangeSound)
            {
                Console.WriteLine("Stop to change sound");
                if (repeatSong)
                {
                    Console.WriteLine("Repeat");
                    string filePath = soundFileURLs[currentSoundPlayingIndex];

                    if (File.Exists(filePath))
                    {
                        if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                        {
                            SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                            audioFile = new AudioFileReader(filePath);

                            if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                            {
                                stopToChangeSound = true;
                                outputAudio.Stop();
                            }
                            outputAudio.Init(audioFile);

                            soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                            audioFile.Volume = (float)soundVolume_Slider.Value;
                            TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];

                            PlayImage.Source = new BitmapImage(new Uri(@"/Images/pause_96px.png", UriKind.Relative));
                            audioPlaying = true;

                            musicCircle.Resume();
                            outputAudio.Play();
                        }
                    }
                }
                else
                {
                    RewindNextButton_Click(sender, new RoutedEventArgs());
                }
            }
            stopToChangeSound = false;
        }

        private void TimerUpdateCurrTime_Elapsed(object sender, EventArgs e)
        {
            if (audioFile != null && outputAudio != null && !timeSliderUserChanging)
            {
                soundTime_Slider.Value = audioFile.CurrentTime.TotalSeconds;
            }

            if (audioFile != null) currentTimeLabel.Content = audioFile.CurrentTime.ToString().Split(new char[] { '.' })[0];
        }

        private void TimerCheckLastSounds_Elapsed(object sender, EventArgs e)
        {
            int filesRemoved = 0;
            checkApplicationFiles();

            List<string> fileSoundLines = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();
            
            for (int i = 0; i < fileSoundLines.Count; i++)
            {
                if (!File.Exists(fileSoundLines[i]))
                {
                    filesRemoved++;
                    fileSoundLines.RemoveAt(i);
                }
            }

            if (filesRemoved > 0)
            {
                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", fileSoundLines);

                soundFileURLs.Clear();
                soundFileURLs.AddRange(fileSoundLines);

                if (PlaylistsTabControl.SelectedIndex == 0)
                {
                    PlaylistListView.Items.Clear();

                    foreach (string soundURL in fileSoundLines)
                    {
                        PlaylistListView.Items.Add(Path.GetFileNameWithoutExtension(soundURL));
                    }
                }

                currentSoundPlayingIndex -= filesRemoved;
            }

            if (fileSoundLines.Count != 0)
            {
                if (!Enumerable.SequenceEqual(fileSoundLines, soundFileURLs))
                {
                    try
                    {
                        if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                        {
                            stopToChangeSound = true;
                            outputAudio.Stop();
                        }
                        
                        soundFileURLs.Clear();
                        soundFileURLs.AddRange(fileSoundLines);

                        if (PlaylistsTabControl.SelectedIndex == 0)
                        {
                            PlaylistListView.Items.Clear();

                            foreach (string soundURL in fileSoundLines)
                            {
                                PlaylistListView.Items.Add(Path.GetFileNameWithoutExtension(soundURL));
                            }
                        }

                        currentSoundPlayingIndex = soundFileURLs.Count - 1;

                        string filePath = soundFileURLs[currentSoundPlayingIndex];

                        if (File.Exists(filePath))
                        {
                            if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                            {
                                SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                                audioFile = new AudioFileReader(filePath);

                                outputAudio.Init(audioFile);

                                soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                                audioFile.Volume = (float)soundVolume_Slider.Value;
                                TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];

                                PlayImage.Source = new BitmapImage(new Uri(@"/Images/pause_96px.png", UriKind.Relative));
                                audioPlaying = true;

                                musicCircle.Resume();
                                outputAudio.Play();
                            }
                        }
                    }
                    catch
                    {
                        string errorFile = soundFileURLs[currentSoundPlayingIndex];

                        if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                        {
                            stopToChangeSound = true;
                            outputAudio.Stop();
                        }

                        List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

                        if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                        {
                            lastSounds.RemoveAt(currentSoundPlayingIndex);
                            PlaylistListView.Items.RemoveAt(currentSoundPlayingIndex);

                            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds.ToArray());
                        }

                        soundFileURLs.Clear();
                        soundFileURLs.AddRange(lastSounds);

                        PlaylistListView.Items.Clear();

                        if (PlaylistsTabControl.SelectedIndex == 0)
                        {
                            foreach (string soundURL in lastSounds)
                            {
                                PlaylistListView.Items.Add(Path.GetFileNameWithoutExtension(soundURL));
                            }
                        }

                        currentSoundPlayingIndex = soundFileURLs.Count - 1;

                        string filePath = soundFileURLs[currentSoundPlayingIndex];

                        if (File.Exists(filePath))
                        {
                            if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                            {
                                SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                                audioFile = new AudioFileReader(filePath);

                                outputAudio.Init(audioFile);

                                soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                                audioFile.Volume = (float)soundVolume_Slider.Value;
                                TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                            }
                        }

                        MessageBox.Show("Error occured while playing file \"" + Path.GetFileNameWithoutExtension(errorFile) + "\"", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", soundFileURLs);
            }

            /*if () Pro playlisty (zjištění kolik tam je souborů*/
        }

        private void windowMoveGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var move = sender as Grid;
            var win = Window.GetWindow(move);
            win.DragMove();
        }

        private void BeMusicWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BeMusicWindow_Loaded(object sender, RoutedEventArgs e)
        {
            soundFileURLs.Clear();
            soundFileURLs.AddRange(Environment.GetCommandLineArgs());
            soundFileURLs.AddRange(File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList());
            soundFileURLs.RemoveAt(0);

            if (soundFileURLs.Count >= 1)
            {
                try
                {
                    currentSoundPlayingIndex = soundFileURLs.Count - 1;

                    string filePath = soundFileURLs[currentSoundPlayingIndex];

                    if (File.Exists(filePath))
                    {
                        if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                        {
                            SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                            audioFile = new AudioFileReader(filePath);

                            outputAudio.Init(audioFile);

                            outputAudio.Play();
                            outputAudio.Pause();

                            soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                            audioFile.Volume = (float)soundVolume_Slider.Value;
                            TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                        }
                    }
                }
                catch
                {
                    string errorFile = soundFileURLs[currentSoundPlayingIndex];

                    if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                    {
                        stopToChangeSound = true;
                        outputAudio.Stop();
                    }

                    List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                    {
                        lastSounds.RemoveAt(currentSoundPlayingIndex);

                        File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds.ToArray());
                    }

                    soundFileURLs.Clear();
                    soundFileURLs.AddRange(lastSounds);

                    currentSoundPlayingIndex = soundFileURLs.Count - 1;

                    string filePath = soundFileURLs[currentSoundPlayingIndex];

                    if (File.Exists(filePath))
                    {
                        if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                        {
                            SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                            audioFile = new AudioFileReader(filePath);

                            outputAudio.Init(audioFile);

                            soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                            audioFile.Volume = (float)soundVolume_Slider.Value;
                            TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                        }
                    }

                    MessageBox.Show("Error occured while playing file \"" + Path.GetFileNameWithoutExtension(errorFile) + "\"", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RewindBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundFileURLs.Count > 0)
            {
                if (audioFile.CurrentTime.TotalSeconds < 2)
                {
                    if (currentSoundPlayingIndex - 1 < 0) currentSoundPlayingIndex = soundFileURLs.Count - 1;
                    else currentSoundPlayingIndex -= 1;

                    try
                    {
                        string filePath = soundFileURLs[currentSoundPlayingIndex];

                        if (File.Exists(filePath))
                        {
                            if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                            {
                                SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                                audioFile = new AudioFileReader(filePath);

                                if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                                {
                                    stopToChangeSound = true;
                                    outputAudio.Stop();
                                }
                                outputAudio.Init(audioFile);

                                soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                                audioFile.Volume = (float)soundVolume_Slider.Value;
                                TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];

                                PlayImage.Source = new BitmapImage(new Uri(@"/Images/pause_96px.png", UriKind.Relative));
                                audioPlaying = true;

                                musicCircle.Resume();
                                outputAudio.Play();
                            }
                        }
                    }
                    catch
                    {
                        string errorFile = soundFileURLs[currentSoundPlayingIndex];

                        if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                        {
                            stopToChangeSound = true;
                            outputAudio.Stop();
                        }

                        List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

                        if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                        {
                            lastSounds.RemoveAt(currentSoundPlayingIndex);

                            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds.ToArray());
                        }

                        soundFileURLs.Clear();
                        soundFileURLs.AddRange(lastSounds);

                        currentSoundPlayingIndex = soundFileURLs.Count - 1;

                        string filePath = soundFileURLs[currentSoundPlayingIndex];

                        if (File.Exists(filePath))
                        {
                            if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                            {
                                SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                                audioFile = new AudioFileReader(filePath);

                                outputAudio.Init(audioFile);

                                soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                                audioFile.Volume = (float)soundVolume_Slider.Value;
                                TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                            }
                        }

                        MessageBox.Show("Error occured while playing file \"" + Path.GetFileNameWithoutExtension(errorFile) + "\"", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    audioFile.CurrentTime = new TimeSpan(0, 0, 0);
                }
            }
        }

        private void RewindNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundFileURLs.Count > 0)
            {
                if (currentSoundPlayingIndex + 1 > soundFileURLs.Count - 1) currentSoundPlayingIndex = 0;
                else currentSoundPlayingIndex += 1;

                try
                {
                    string filePath = soundFileURLs[currentSoundPlayingIndex];

                    if (File.Exists(filePath))
                    {
                        if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                        {
                            SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                            audioFile = new AudioFileReader(filePath);

                            if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                            {
                                stopToChangeSound = true;
                                outputAudio.Stop();
                            }
                            outputAudio.Init(audioFile);

                            soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                            audioFile.Volume = (float)soundVolume_Slider.Value;
                            TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];

                            PlayImage.Source = new BitmapImage(new Uri(@"/Images/pause_96px.png", UriKind.Relative));
                            audioPlaying = true;

                            musicCircle.Resume();
                            outputAudio.Play();
                        }
                    }
                }
                catch
                {
                    string errorFile = soundFileURLs[currentSoundPlayingIndex];

                    stopToChangeSound = true;
                    outputAudio.Stop();

                    List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                    {
                        lastSounds.RemoveAt(currentSoundPlayingIndex);

                        File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds.ToArray());
                    }

                    soundFileURLs.Clear();
                    soundFileURLs.AddRange(lastSounds);

                    currentSoundPlayingIndex = soundFileURLs.Count - 1;

                    string filePath = soundFileURLs[currentSoundPlayingIndex];

                    if (File.Exists(filePath))
                    {
                        if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                        {
                            SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                            audioFile = new AudioFileReader(filePath);

                            outputAudio.Init(audioFile);

                            soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                            audioFile.Volume = (float)soundVolume_Slider.Value;
                            TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                        }
                    }

                    MessageBox.Show("Error occured while playing file \"" + Path.GetFileNameWithoutExtension(errorFile) + "\"", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void soundTime_Slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            timeSliderUserChanging = true;
        }

        private void ExpandShrinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (BeMusicWindow.WindowState == WindowState.Normal)
            {
                ExpandShrinkImage.Source = new BitmapImage(new Uri(@"/Images/compress_96px.png", UriKind.Relative));
                BeMusicWindow.WindowState = WindowState.Maximized;
            }
            else if (BeMusicWindow.WindowState == WindowState.Maximized)
            {
                ExpandShrinkImage.Source = new BitmapImage(new Uri(@"/Images/expand_96px.png", UriKind.Relative));
                BeMusicWindow.WindowState = WindowState.Normal;
            }
        }

        private void BeMusicWindow_StateChanged(object sender, EventArgs e)
        {
            if (BeMusicWindow.WindowState == WindowState.Normal)
            {
                ExpandShrinkImage.Source = new BitmapImage(new Uri(@"/Images/expand_96px.png", UriKind.Relative));
            }
            else if (BeMusicWindow.WindowState == WindowState.Maximized)
            {
                ExpandShrinkImage.Source = new BitmapImage(new Uri(@"/Images/compress_96px.png", UriKind.Relative));
            }
        }

        private void BeMusicWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                playSound_button_Click(sender, new RoutedEventArgs());
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (soundVolume_Slider.Value != 0)
            {
                lastVolume = audioFile.Volume;
                soundVolume_Slider.Value = 0;
            }
            else
            {
                audioFile.Volume = lastVolume;
                soundVolume_Slider.Value = lastVolume;
            }
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (repeatSong)
            {
                RepeatImage.Opacity = 0.3;
                repeatSong = false;
            }
            else
            {
                RepeatImage.Opacity = 1;
                repeatSong = true;
            }
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Topmost)
            {
                Topmost = true;
                PinImage.Source = new BitmapImage(new Uri(@"/Images/pin_96px.png", UriKind.Relative));
            }
            else
            {
                Topmost = false;
                PinImage.Source = new BitmapImage(new Uri(@"/Images/pin_border_96px.png", UriKind.Relative));
            }
        }

        private void BeMusicWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            AppContentScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

            windowMouseEnterLeaveStoryBoard.Children.Clear();

            DoubleAnimation WMEL_ExpndBtnPosA = new DoubleAnimation();

            WMEL_ExpndBtnPosA.From = PinButton.Opacity;
            WMEL_ExpndBtnPosA.To = 0.0;
            WMEL_ExpndBtnPosA.AccelerationRatio = 0.5;
            WMEL_ExpndBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_ExpndBtnPosA);
            Storyboard.SetTarget(WMEL_ExpndBtnPosA, ExpandShrinkButton);
            Storyboard.SetTargetProperty(WMEL_ExpndBtnPosA, new PropertyPath(Button.OpacityProperty));

            DoubleAnimation WMEL_PinBtnPosA = new DoubleAnimation();

            WMEL_PinBtnPosA.From = PinButton.Opacity;
            WMEL_PinBtnPosA.To = 0.0;
            WMEL_PinBtnPosA.AccelerationRatio = 0.5;
            WMEL_PinBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_PinBtnPosA);
            Storyboard.SetTarget(WMEL_PinBtnPosA, PinButton);
            Storyboard.SetTargetProperty(WMEL_PinBtnPosA, new PropertyPath(Button.OpacityProperty));

            ThicknessAnimation WMEL_SongNamePosA = new ThicknessAnimation();

            WMEL_SongNamePosA.From = new Thickness(SongNameLabel.Margin.Left, SongNameLabel.Margin.Top, SongNameLabel.Margin.Right, SongNameLabel.Margin.Bottom);
            WMEL_SongNamePosA.To = new Thickness(10, 0, 0, 48);
            WMEL_SongNamePosA.AccelerationRatio = 0.5;
            WMEL_SongNamePosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SongNamePosA);
            Storyboard.SetTarget(WMEL_SongNamePosA, SongNameLabel);
            Storyboard.SetTargetProperty(WMEL_SongNamePosA, new PropertyPath(Label.MarginProperty));

            ThicknessAnimation WMEL_BorGradPosA = new ThicknessAnimation();

            WMEL_BorGradPosA.From = new Thickness(GradientBorder.Margin.Left, GradientBorder.Margin.Top, GradientBorder.Margin.Right, GradientBorder.Margin.Bottom);
            WMEL_BorGradPosA.To = new Thickness(0, 0, 0, 35);
            WMEL_BorGradPosA.AccelerationRatio = 0.5;
            WMEL_BorGradPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_BorGradPosA);
            Storyboard.SetTarget(WMEL_BorGradPosA, GradientBorder);
            Storyboard.SetTargetProperty(WMEL_BorGradPosA, new PropertyPath(Border.MarginProperty));

            ThicknessAnimation WMEL_BorPosA = new ThicknessAnimation();

            WMEL_BorPosA.From = new Thickness(ControlsBorder.Margin.Left, ControlsBorder.Margin.Top, ControlsBorder.Margin.Right, ControlsBorder.Margin.Bottom);
            WMEL_BorPosA.To = new Thickness(0, 0, 0, -45);
            WMEL_BorPosA.AccelerationRatio = 0.5;
            WMEL_BorPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_BorPosA);
            Storyboard.SetTarget(WMEL_BorPosA, ControlsBorder);
            Storyboard.SetTargetProperty(WMEL_BorPosA, new PropertyPath(Border.MarginProperty));

            windowMouseEnterLeaveStoryBoard.Begin();
        }

        private void BeMusicWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            AppContentScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

            windowMouseEnterLeaveStoryBoard.Children.Clear();

            DoubleAnimation WMEL_ExpndBtnPosA = new DoubleAnimation();

            WMEL_ExpndBtnPosA.From = PinButton.Opacity;
            WMEL_ExpndBtnPosA.To = 1.0;
            WMEL_ExpndBtnPosA.AccelerationRatio = 0.5;
            WMEL_ExpndBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_ExpndBtnPosA);
            Storyboard.SetTarget(WMEL_ExpndBtnPosA, ExpandShrinkButton);
            Storyboard.SetTargetProperty(WMEL_ExpndBtnPosA, new PropertyPath(Button.OpacityProperty));

            DoubleAnimation WMEL_PinBtnPosA = new DoubleAnimation();

            WMEL_PinBtnPosA.From = PinButton.Opacity;
            WMEL_PinBtnPosA.To = 1.0;
            WMEL_PinBtnPosA.AccelerationRatio = 0.5;
            WMEL_PinBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_PinBtnPosA);
            Storyboard.SetTarget(WMEL_PinBtnPosA, PinButton);
            Storyboard.SetTargetProperty(WMEL_PinBtnPosA, new PropertyPath(Button.OpacityProperty));

            ThicknessAnimation WMEL_SongNamePosA = new ThicknessAnimation();

            WMEL_SongNamePosA.From = new Thickness(SongNameLabel.Margin.Left, SongNameLabel.Margin.Top, SongNameLabel.Margin.Right, SongNameLabel.Margin.Bottom);
            WMEL_SongNamePosA.To = new Thickness(10, 0, 0, 93);
            WMEL_SongNamePosA.AccelerationRatio = 0.5;
            WMEL_SongNamePosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SongNamePosA);
            Storyboard.SetTarget(WMEL_SongNamePosA, SongNameLabel);
            Storyboard.SetTargetProperty(WMEL_SongNamePosA, new PropertyPath(Label.MarginProperty));

            ThicknessAnimation WMEL_BorGradPosA = new ThicknessAnimation();

            WMEL_BorGradPosA.From = new Thickness(GradientBorder.Margin.Left, GradientBorder.Margin.Top, GradientBorder.Margin.Right, GradientBorder.Margin.Bottom);
            WMEL_BorGradPosA.To = new Thickness(0, 0, 0, 80);
            WMEL_BorGradPosA.AccelerationRatio = 0.5;
            WMEL_BorGradPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_BorGradPosA);
            Storyboard.SetTarget(WMEL_BorGradPosA, GradientBorder);
            Storyboard.SetTargetProperty(WMEL_BorGradPosA, new PropertyPath(Border.MarginProperty));

            ThicknessAnimation WMEL_BorPosDA = new ThicknessAnimation();

            WMEL_BorPosDA.From = new Thickness(ControlsBorder.Margin.Left, ControlsBorder.Margin.Top, ControlsBorder.Margin.Right, ControlsBorder.Margin.Bottom);
            WMEL_BorPosDA.To = new Thickness(0, 0, 0, 0);
            WMEL_BorPosDA.AccelerationRatio = 0.5;
            WMEL_BorPosDA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_BorPosDA);
            Storyboard.SetTarget(WMEL_BorPosDA, ControlsBorder);
            Storyboard.SetTargetProperty(WMEL_BorPosDA, new PropertyPath(Border.MarginProperty));

            windowMouseEnterLeaveStoryBoard.Begin();
        }

        private void BeMusicWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (BeMusicWindow.WindowState == WindowState.Maximized)
            {
                SongPlayerGrid.Height = BeMusicWindow.ActualHeight - 45;
            }
            else
            {
                SongPlayerGrid.Height = BeMusicWindow.ActualHeight - 31;
            }
        }

        private void PlaylistsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistsTabControl.SelectedIndex == 0)
            {
                foreach (string soundURL in File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                {
                    PlaylistListView.Items.Add(Path.GetFileNameWithoutExtension(soundURL));
                }
            }
        }

        private void PlaylistListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //PlaylistItem playlistItem = (PlaylistItem)PlaylistListView.SelectedItem;

            
        }

        private void soundVolume_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioFile != null)
            {
                audioFile.Volume = (float)soundVolume_Slider.Value;

                if (soundVolume_Slider.Value > 1.5)
                {
                    SoundImage.Source = new BitmapImage(new Uri(@"/Images/full_volume_96px.png", UriKind.Relative));
                }
                else if (soundVolume_Slider.Value > 0.75)
                {
                    SoundImage.Source = new BitmapImage(new Uri(@"/Images/medium_volume_96px.png", UriKind.Relative));
                }
                else if (soundVolume_Slider.Value > 0)
                {
                    SoundImage.Source = new BitmapImage(new Uri(@"/Images/low_volume_96px.png", UriKind.Relative));
                }
                else if (soundVolume_Slider.Value == 0)
                {
                    SoundImage.Source = new BitmapImage(new Uri(@"/Images/mute_volume_96px.png", UriKind.Relative));
                }
            }
        }

        private void RemoveFromPlaylist_MI_Click(object sender, RoutedEventArgs e)
        {
           if (currentSoundPlayingIndex == PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem))
            {
                if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                {
                    stopToChangeSound = true;
                    outputAudio.Stop();
                }
            }

            List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

            lastSounds.RemoveAt(PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem));

            soundFileURLs.RemoveAt(PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem));

            PlaylistListView.Items.RemoveAt(PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem));

            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds);
        }

        private void NewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            //var createFile = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt");
            //createFile.Close();
        }

        private void soundTime_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (audioFile != null)
            {
                if (soundTime_Slider.Value == audioFile.TotalTime.TotalSeconds)
                {
                    audioFile.CurrentTime = audioFile.TotalTime;
                    outputAudio.Stop();
                }
                else
                {
                    audioFile.CurrentTime = new TimeSpan(0, 0, Convert.ToInt32(soundTime_Slider.Value));
                }
            }

            timeSliderUserChanging = false;
        }

        public void setUpAnimations()
        {
            DoubleAnimation musicCircleA = new DoubleAnimation();

            musicCircleA.From = 0;
            musicCircleA.To = 360;
            musicCircle.RepeatBehavior = RepeatBehavior.Forever;
            musicCircleA.Duration = new Duration(TimeSpan.FromSeconds(5));

            musicCircle.Children.Add(musicCircleA);
            Storyboard.SetTarget(musicCircleA, SoundImageBorder);
            Storyboard.SetTargetProperty(musicCircleA, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            musicCircle.Begin();
            musicCircle.Pause();
        }

        protected void PlaylistListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                currentSoundPlayingIndex = PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem);

                string filePath = soundFileURLs[currentSoundPlayingIndex];

                if (File.Exists(filePath))
                {
                    if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                    {
                        SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                        audioFile = new AudioFileReader(filePath);

                        if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                        {
                            stopToChangeSound = true;
                            outputAudio.Stop();
                        }

                        outputAudio.Init(audioFile);

                        soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                        audioFile.Volume = (float)soundVolume_Slider.Value;
                        TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                        PlayImage.Source = new BitmapImage(new Uri(@"/Images/pause_96px.png", UriKind.Relative));

                        audioPlaying = true;

                        musicCircle.Resume();
                        outputAudio.Play();
                    }
                }
            }
            catch
            {
                string errorFile = soundFileURLs[currentSoundPlayingIndex];

                if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                {
                    stopToChangeSound = true;
                    outputAudio.Stop();
                }

                List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
                {
                    lastSounds.RemoveAt(currentSoundPlayingIndex);

                    File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds.ToArray());
                }

                soundFileURLs.Clear();
                soundFileURLs.AddRange(lastSounds);

                currentSoundPlayingIndex = soundFileURLs.Count - 1;

                string filePath = soundFileURLs[currentSoundPlayingIndex];

                if (File.Exists(filePath))
                {
                    if (Path.GetExtension(filePath) == ".mp3" || Path.GetExtension(filePath) == ".wav")
                    {
                        SongNameLabel.Content = Path.GetFileNameWithoutExtension(filePath);
                        audioFile = new AudioFileReader(filePath);

                        outputAudio.Init(audioFile);

                        soundTime_Slider.Maximum = audioFile.TotalTime.TotalSeconds;
                        audioFile.Volume = (float)soundVolume_Slider.Value;
                        TotalTimeLabel.Content = audioFile.TotalTime.ToString().Split(new char[] { '.' })[0];
                    }
                }

                MessageBox.Show("Error occured while playing file \"" + Path.GetFileNameWithoutExtension(errorFile) + "\"", "File error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
