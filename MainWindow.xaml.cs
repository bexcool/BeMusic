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
using System.Drawing;
using System.Drawing.Imaging;
using YoutubeExplode;
using Microsoft.Win32;
using BeMusic;

namespace BeMusic
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputAudio;
        private AudioFileReader audioFile;

        bool audioPlaying, timeSliderUserChanging = false, stopToChangeSound = false, repeatSong = false, soundNameAnimPlaying = false, canCheckSettings = false;

        DispatcherTimer timerUpdateCurrTime = new DispatcherTimer();
        DispatcherTimer timerCheckLastSounds = new DispatcherTimer();
        DispatcherTimer timerCheckSoundName = new DispatcherTimer();

        List<string> soundFileURLs = new List<string>();

        int currentSoundPlayingIndex = 0;

        float lastVolume = 0;

        Storyboard windowMouseEnterLeaveStoryBoard = new Storyboard();
        Storyboard musicCircle = new Storyboard();
        Storyboard soundName = new Storyboard();

        string currentPlaylist = "last_sounds.txt";

        YoutubeClient YouTubePlayer = new YoutubeClient();

        OpenFileDialog soundFileDialog = new OpenFileDialog();

        //GeniusClient geniusClient;

        public MainWindow()
        {
            checkApplicationFiles();
            alreadyStarted();
            InitializeComponent();
            setUpAnimations();
            setUpGenius();
            loadSettings();

            RepeatImage.Opacity = 0.3;
            lastVolume = (float)soundVolume_Slider.Value;

            outputAudio = new WaveOutEvent();
            outputAudio.PlaybackStopped += outputAudio_PlaybackStopped;

            soundName.Completed += MusicCircle_Completed;

            timerUpdateCurrTime.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerUpdateCurrTime.Tick += TimerUpdateCurrTime_Elapsed;
            timerUpdateCurrTime.Start();

            timerCheckLastSounds.Interval = new TimeSpan(0, 0, 1);
            timerCheckLastSounds.Tick += TimerCheckLastSounds_Elapsed;
            timerCheckLastSounds.Start();

            timerCheckSoundName.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerCheckSoundName.Tick += TimerCheckSoundName_Elapsed;
            timerCheckSoundName.Start();

            soundFileDialog.Filter = "Sound file (*.mp3;*.wav) | *.mp3;*.wav";
            soundFileDialog.FileOk += SoundFileDialog_FileOk;
        }

        private void loadSettings()
        {
            List<string> settingsData = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt").ToList();

            // 0 Volume

            if (audioFile == null) soundVolume_Slider.Value = double.Parse(settingsData.ToArray()[0].Replace('.', ','));

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

            // 1 Background image
            // Try if index exists

            try
            {
                BackgroundImage.ImageSource = new BitmapImage(new Uri(settingsData.ToArray()[1]));
            }
            catch
            {
                settingsData.Add("pack://application:,,,/BeMusic;component/Images/winter-mountain-snow-4k-01.jpg");

                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt", settingsData);
            }

            canCheckSettings = true;
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
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config")) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config");

            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
            {
                var createFile = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt");
                createFile.Close();
            }

            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt"))
            {
                var createdFile = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt");
                createdFile.Close();

                
                // 0 Volume
                // 1 Background picture

                List<string> settingsData = new List<string>();

                settingsData.Add("0.5");
                settingsData.Add("pack://application:,,,/BeMusic;component/Images/winter-mountain-snow-4k-01.jpg");

                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt", settingsData);
            }

            if (canCheckSettings) loadSettings();
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
                                getSongData();
                                
                                if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                                {
                                    stopToChangeSound = true;
                                    outputAudio.Stop();
                                }
                                if (audioFile != null) audioFile.Close();

                                audioFile = new AudioFileReader(filePath);

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
                            if (audioFile != null) audioFile.Close();
                            getSongData();
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

        private void TimerCheckSoundName_Elapsed(object sender, EventArgs e)
        {
            if (SongNameLabel.ActualWidth + SongNameLabel.Margin.Left + 10 > SongNameGrid.ActualWidth)
            {
                if (!soundNameAnimPlaying)
                {
                    soundName.Children.Clear();

                    ThicknessAnimationUsingKeyFrames soundNameA = new ThicknessAnimationUsingKeyFrames();

                    soundNameA.KeyFrames.Add(new LinearThicknessKeyFrame(new Thickness(10, SongNameLabel.Margin.Top, SongNameLabel.Margin.Right, SongNameLabel.Margin.Bottom), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                    soundNameA.KeyFrames.Add(new LinearThicknessKeyFrame(new Thickness(10, SongNameLabel.Margin.Top, SongNameLabel.Margin.Right, SongNameLabel.Margin.Bottom), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1))));
                    soundNameA.KeyFrames.Add(new LinearThicknessKeyFrame(new Thickness(-1 * SongNameLabel.ActualWidth - 100, SongNameLabel.Margin.Top, SongNameLabel.Margin.Right, SongNameLabel.Margin.Bottom), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(9))));
                    soundName.RepeatBehavior = RepeatBehavior.Forever;

                    soundName.Children.Add(soundNameA);
                    Storyboard.SetTarget(soundNameA, SongNameLabel);
                    Storyboard.SetTargetProperty(soundNameA, new PropertyPath(Label.MarginProperty));

                    soundName.Begin();
                    soundNameAnimPlaying = true;
                }
            }
            else
            {
                if (soundNameAnimPlaying)
                {
                    soundName.Pause();
                    var t2 = new Timer { Enabled = true, Interval = 1000 };
                    t2.Elapsed += (o, a) => { soundNameAnimPlaying = false; soundName.Stop(); t2.Stop(); };
                }
            }
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
                                if (audioFile != null) audioFile.Close();
                                getSongData();
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
                                if (audioFile != null) audioFile.Close();
                                getSongData();
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
                            if (audioFile != null) audioFile.Close();
                            getSongData();
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
                            if (audioFile != null) audioFile.Close();
                            getSongData();
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
                                getSongData();
                                
                                if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                                {
                                    stopToChangeSound = true;
                                    outputAudio.Stop();
                                }
                                if (audioFile != null) audioFile.Close();

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
                                getSongData();
                                audioFile = new AudioFileReader(filePath);

                                if (audioFile != null) audioFile.Close();

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
                            getSongData();
                            
                            if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                            {
                                stopToChangeSound = true;
                                outputAudio.Stop();
                            }
                            if (audioFile != null) audioFile.Close();

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
                            if (audioFile != null) audioFile.Close();
                            getSongData();
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

            List<string> settingsData = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt").ToList();

            // 0 Volume

            settingsData[0] = soundVolume_Slider.Value.ToString();

            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt", settingsData);
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

            ThicknessAnimation WMEL_SongAlbumNamePosA = new ThicknessAnimation();

            WMEL_SongAlbumNamePosA.From = new Thickness(SongAlbumNameLabel.Margin.Left, SongAlbumNameLabel.Margin.Top, SongAlbumNameLabel.Margin.Right, SongAlbumNameLabel.Margin.Bottom);
            WMEL_SongAlbumNamePosA.To = new Thickness(SongAlbumNameLabel.Margin.Left, SongAlbumNameLabel.Margin.Top, SongAlbumNameLabel.Margin.Right, 61);
            WMEL_SongAlbumNamePosA.AccelerationRatio = 0.5;
            WMEL_SongAlbumNamePosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SongAlbumNamePosA);
            Storyboard.SetTarget(WMEL_SongAlbumNamePosA, SongAlbumNameLabel);
            Storyboard.SetTargetProperty(WMEL_SongAlbumNamePosA, new PropertyPath(Label.MarginProperty));

            DoubleAnimation WMEL_BgImageBtnPosA = new DoubleAnimation();

            WMEL_BgImageBtnPosA.From = BackgroundBorder.Opacity;
            WMEL_BgImageBtnPosA.To = 0.7;
            WMEL_BgImageBtnPosA.AccelerationRatio = 0.5;
            WMEL_BgImageBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_BgImageBtnPosA);
            Storyboard.SetTarget(WMEL_BgImageBtnPosA, BackgroundBorder);
            Storyboard.SetTargetProperty(WMEL_BgImageBtnPosA, new PropertyPath(Border.OpacityProperty));

            DoubleAnimation WMEL_SettingsBtnPosA = new DoubleAnimation();

            WMEL_SettingsBtnPosA.From = SettingsButton.Opacity;
            WMEL_SettingsBtnPosA.To = 0.0;
            WMEL_SettingsBtnPosA.AccelerationRatio = 0.5;
            WMEL_SettingsBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SettingsBtnPosA);
            Storyboard.SetTarget(WMEL_SettingsBtnPosA, SettingsButton);
            Storyboard.SetTargetProperty(WMEL_SettingsBtnPosA, new PropertyPath(Button.OpacityProperty));

            DoubleAnimation WMEL_ExpndBtnPosA = new DoubleAnimation();

            WMEL_ExpndBtnPosA.From = ExpandShrinkButton.Opacity;
            WMEL_ExpndBtnPosA.To = 0.0;
            WMEL_ExpndBtnPosA.AccelerationRatio = 0.5;
            WMEL_ExpndBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_ExpndBtnPosA);
            Storyboard.SetTarget(WMEL_ExpndBtnPosA, ExpandShrinkButton);
            Storyboard.SetTargetProperty(WMEL_ExpndBtnPosA, new PropertyPath(Button.OpacityProperty));

            DoubleAnimation WMEL_OpenBtnPosA = new DoubleAnimation();

            WMEL_OpenBtnPosA.From = OpenSongButton.Opacity;
            WMEL_OpenBtnPosA.To = 0.0;
            WMEL_OpenBtnPosA.AccelerationRatio = 0.5;
            WMEL_OpenBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_OpenBtnPosA);
            Storyboard.SetTarget(WMEL_OpenBtnPosA, OpenSongButton);
            Storyboard.SetTargetProperty(WMEL_OpenBtnPosA, new PropertyPath(Button.OpacityProperty));

            DoubleAnimation WMEL_PinBtnPosA = new DoubleAnimation();

            WMEL_PinBtnPosA.From = PinButton.Opacity;
            WMEL_PinBtnPosA.To = 0.0;
            WMEL_PinBtnPosA.AccelerationRatio = 0.5;
            WMEL_PinBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_PinBtnPosA);
            Storyboard.SetTarget(WMEL_PinBtnPosA, PinButton);
            Storyboard.SetTargetProperty(WMEL_PinBtnPosA, new PropertyPath(Button.OpacityProperty));

            ThicknessAnimation WMEL_SongNameGridPosA = new ThicknessAnimation();

            WMEL_SongNameGridPosA.From = new Thickness(SongNameGrid.Margin.Left, SongNameGrid.Margin.Top, SongNameGrid.Margin.Right, SongNameGrid.Margin.Bottom);
            WMEL_SongNameGridPosA.To = new Thickness(SongNameGrid.Margin.Left, SongNameGrid.Margin.Top, SongNameGrid.Margin.Right, 38);
            WMEL_SongNameGridPosA.AccelerationRatio = 0.5;
            WMEL_SongNameGridPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SongNameGridPosA);
            Storyboard.SetTarget(WMEL_SongNameGridPosA, SongNameGrid);
            Storyboard.SetTargetProperty(WMEL_SongNameGridPosA, new PropertyPath(Grid.MarginProperty));

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

            ThicknessAnimation WMEL_SongAlbumNamePosA = new ThicknessAnimation();

            WMEL_SongAlbumNamePosA.From = new Thickness(SongAlbumNameLabel.Margin.Left, SongAlbumNameLabel.Margin.Top, SongAlbumNameLabel.Margin.Right, SongAlbumNameLabel.Margin.Bottom);
            WMEL_SongAlbumNamePosA.To = new Thickness(SongAlbumNameLabel.Margin.Left, SongAlbumNameLabel.Margin.Top, SongAlbumNameLabel.Margin.Right, 106);
            WMEL_SongAlbumNamePosA.AccelerationRatio = 0.5;
            WMEL_SongAlbumNamePosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SongAlbumNamePosA);
            Storyboard.SetTarget(WMEL_SongAlbumNamePosA, SongAlbumNameLabel);
            Storyboard.SetTargetProperty(WMEL_SongAlbumNamePosA, new PropertyPath(Label.MarginProperty));

            DoubleAnimation WMEL_BgImageBtnPosA = new DoubleAnimation();

            WMEL_BgImageBtnPosA.From = BackgroundBorder.Opacity;
            WMEL_BgImageBtnPosA.To = 0.3;
            WMEL_BgImageBtnPosA.AccelerationRatio = 0.5;
            WMEL_BgImageBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_BgImageBtnPosA);
            Storyboard.SetTarget(WMEL_BgImageBtnPosA, BackgroundBorder);
            Storyboard.SetTargetProperty(WMEL_BgImageBtnPosA, new PropertyPath(Border.OpacityProperty));

            DoubleAnimation WMEL_SettingsBtnPosA = new DoubleAnimation();

            WMEL_SettingsBtnPosA.From = SettingsButton.Opacity;
            WMEL_SettingsBtnPosA.To = 1.0;
            WMEL_SettingsBtnPosA.AccelerationRatio = 0.5;
            WMEL_SettingsBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SettingsBtnPosA);
            Storyboard.SetTarget(WMEL_SettingsBtnPosA, SettingsButton);
            Storyboard.SetTargetProperty(WMEL_SettingsBtnPosA, new PropertyPath(Button.OpacityProperty));

            DoubleAnimation WMEL_OpenBtnPosA = new DoubleAnimation();

            WMEL_OpenBtnPosA.From = OpenSongButton.Opacity;
            WMEL_OpenBtnPosA.To = 1.0;
            WMEL_OpenBtnPosA.AccelerationRatio = 0.5;
            WMEL_OpenBtnPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_OpenBtnPosA);
            Storyboard.SetTarget(WMEL_OpenBtnPosA, OpenSongButton);
            Storyboard.SetTargetProperty(WMEL_OpenBtnPosA, new PropertyPath(Button.OpacityProperty));

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

            ThicknessAnimation WMEL_SongNameGridPosA = new ThicknessAnimation();

            WMEL_SongNameGridPosA.From = new Thickness(SongNameGrid.Margin.Left, SongNameGrid.Margin.Top, SongNameGrid.Margin.Right, SongNameGrid.Margin.Bottom);
            WMEL_SongNameGridPosA.To = new Thickness(SongNameGrid.Margin.Left, SongNameGrid.Margin.Top, SongNameGrid.Margin.Right, 83);
            WMEL_SongNameGridPosA.AccelerationRatio = 0.5;
            WMEL_SongNameGridPosA.Duration = new Duration(TimeSpan.FromSeconds(0.2));

            windowMouseEnterLeaveStoryBoard.Children.Add(WMEL_SongNameGridPosA);
            Storyboard.SetTarget(WMEL_SongNameGridPosA, SongNameGrid);
            Storyboard.SetTargetProperty(WMEL_SongNameGridPosA, new PropertyPath(Grid.MarginProperty));

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
                RewindNextButton_Click(new object(), new RoutedEventArgs());
            }

            List<string> lastSounds = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

            lastSounds.RemoveAt(PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem));

            soundFileURLs.RemoveAt(PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem));

            PlaylistListView.Items.RemoveAt(PlaylistListView.Items.IndexOf(PlaylistListView.SelectedItem));

            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", lastSounds);
        }

        private void NewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            int index = 1;

            for (int i = 0; i < Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\").Length; i++)
            {
                if (Path.GetFileName(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\")[i]) == "NewPlaylist" + index + ".txt")
                {
                    index++;
                }
            }

            var createFile = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\NewPlaylist" + index + ".txt");
            createFile.Close();

            PlaylistsTabControl.Items.Add("NewPlaylist" + index);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow seettingsWindow = new SettingsWindow();

            seettingsWindow.ShowDialog();
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

        private void OpenSongButton_Click(object sender, RoutedEventArgs e)
        {
            soundFileDialog.ShowDialog();
        }

        private void SoundFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt"))
            {
                List<string> currentSoundFileURLs = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt").ToList();

                if (currentSoundFileURLs.Contains(soundFileDialog.FileName))
                {
                    currentSoundFileURLs.Remove(soundFileDialog.FileName);
                    currentSoundFileURLs.Add(soundFileDialog.FileName);
                }
                else
                {
                    currentSoundFileURLs.Add(soundFileDialog.FileName);
                }

                File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\last_sounds.txt", currentSoundFileURLs.ToArray());

                TimerCheckLastSounds_Elapsed(sender, new EventArgs());
            }
        }

        private void soundVolume_Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            List<string> settingsData = File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt").ToList();

            // 0 Volume

            settingsData[0] = soundVolume_Slider.Value.ToString();

            File.WriteAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\BeMusic\config\settings.txt", settingsData);
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

        private void MusicCircle_Completed(object sender, EventArgs e)
        {
            
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
                        getSongData();
                        
                        if (outputAudio.PlaybackState == PlaybackState.Paused || outputAudio.PlaybackState == PlaybackState.Playing)
                        {
                            stopToChangeSound = true;
                            outputAudio.Stop();
                        }

                        if (audioFile != null) audioFile.Close();

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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

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
                        if (audioFile != null) audioFile.Close();
                        getSongData();
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

        public void getSongData()
        {
            try
            {
                byte[] b = new byte[128];

                FileStream fs = new FileStream(soundFileURLs[currentSoundPlayingIndex], FileMode.Open);
                fs.Seek(-128, SeekOrigin.End);
                fs.Read(b, 0, 128);
                String sFlag = Encoding.Default.GetString(b, 0, 3);
                fs.Close();

                SongNameLabel.Content = Path.GetFileNameWithoutExtension(soundFileURLs[currentSoundPlayingIndex]);
                SongAlbumNameLabel.Content = "";

                if (sFlag.CompareTo("TAG") == 0)
                {
                    // Singer - title
                    if (Encoding.Default.GetString(b, 33, 30) != String.Empty && Encoding.Default.GetString(b, 3, 30) != String.Empty && !Encoding.Default.GetString(b, 33, 30).Contains("\0") && !Encoding.Default.GetString(b, 3, 30).Contains("\0")) SongNameLabel.Content = Encoding.Default.GetString(b, 33, 30) + " - " + Encoding.Default.GetString(b, 3, 30);
                    // Album
                    SongAlbumNameLabel.Content = Encoding.Default.GetString(b, 63, 30);

                    // Year of publish
                    //sYear = Encoding.Default.GetString(b, 93, 4);
                    // Comment
                    //sComm = Encoding.Default.GetString(b, 97, 30);
                }
            }
            catch
            {

            }

            /*
            try
            {
                TagLib.File file = TagLib.File.Create(soundFileURLs[currentSoundPlayingIndex]);
                var mStream = new MemoryStream();
                var firstPicture = file.Tag.Pictures.FirstOrDefault();
                if (firstPicture != null)
                {
                    byte[] pData = firstPicture.Data.Data;
                    mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
                    BitmapImage bmi = new BitmapImage();
                    bmi.StreamSource = mStream;
                    mStream.Dispose();
                    BackgroundImage.ImageSource = bmi; 
                }
                else
                {
                    // set "no cover" image
                }

                
                TagLib.File tagLibFile = new TagLib.Mpeg.AudioFile(soundFileURLs[currentSoundPlayingIndex]);

                MemoryStream ms = new MemoryStream(tagLibFile.Tag.Pictures[0].Data.Data);

                BitmapImage albumBitmap = new BitmapImage();
                albumBitmap.BeginInit();
                albumBitmap.StreamSource = ms;
                albumBitmap.EndInit();

                ms.Close();

                BackgroundImage.ImageSource = albumBitmap;
            }
            catch
            {

            }
            */
        }
    }
}
