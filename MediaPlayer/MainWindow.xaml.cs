using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using System.Windows.Media.Imaging;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }
        Random random = new Random();
        private string _currentPlaying = string.Empty;
        private bool _playing = false;
        private string _shortName
        {
            get
            {
                var info = new FileInfo(_currentPlaying);
                var name = Path.GetFileNameWithoutExtension(info.Name);
                return name;
            }
        }
        DispatcherTimer _timer;
        bool _isDragging = false;
        ObservableCollection<MediaItem> _playList = new ObservableCollection<MediaItem>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string filename = "playlist.txt";
            if (File.Exists(filename) && new FileInfo(filename).Length != 0)
            {
                foreach (string line in File.ReadLines(filename))
                {
                    MediaItem media = new MediaItem(line);
                    _playList.Add(media);
                }
            }

            if (File.Exists("playing.txt") && new FileInfo("playing.txt").Length != 0)
            {
                string path = File.ReadLines("playing.txt").First();
                string position = File.ReadLines("playing.txt").Skip(1).First();
                startMedia(path);
                highlightPlayingMedia();
                player.Position = TimeSpan.FromSeconds(Convert.ToDouble(position));
            }
            playlistListView.ItemsSource = _playList;

            
        }
        private string[] openMp4Files()
        {
            var screen = new OpenFileDialog();
            screen.Multiselect = true;
            screen.Filter = "Media (*.mp4;*.mp3)|*.mp4;*.mp3";
            if (screen.ShowDialog() == true)
            {
                return screen.FileNames;
            }
            return null;
        }

        private void startMedia(string path)
        {
            _currentPlaying = path;
            if (Path.GetExtension(path) == ".mp3")
            {
                noMediaCover.Visibility = Visibility.Hidden;
                mp3Img.Visibility = Visibility.Visible;
                Uri pathMp3 = new Uri(Path.GetFullPath("image/mp3Thumb.jpg"), UriKind.Absolute);
                var bitmap = new BitmapImage(pathMp3);
                mp3Img.Source = bitmap;
            }
            else
            {
                noMediaCover.Visibility = Visibility.Visible;
                mp3Img.Visibility = Visibility.Hidden;
            }
            titleTextBlock.Text = _shortName;
            player.Source = new Uri(_currentPlaying, UriKind.Absolute);
            player.Play();

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 1, 0); ;
            _timer.Tick += _timer_Tick;
            _timer.Start();
            _playing = true;
        }

        private void addMediaFile_Click(object sender, RoutedEventArgs e)
        {
            string[] paths = openMp4Files();
            if (paths == null) return;

            foreach (string path in paths)
            {
                if (_playList.Any(file => file.Path == path)) continue;
                MediaItem media = new MediaItem(path);
                _playList.Add(media);
            }
            startMedia(paths[0]);
            highlightPlayingMedia();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            if (_isDragging) return;
            progressSlider.Value = player.Position.TotalSeconds;
        }

        private void player_MediaOpened(object sender, RoutedEventArgs e)
        {
            int hours = player.NaturalDuration.TimeSpan.Hours;
            int minutes = player.NaturalDuration.TimeSpan.Minutes;
            int seconds = player.NaturalDuration.TimeSpan.Seconds;
            fullTime.Text = hours == 0 ? $"{minutes:00}:{seconds:00}" : $"{hours}:{minutes:00}:{seconds:00}";

            playIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Pause;
            _playing = true;
            // cập nhật max value của slider
            progressSlider.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void togglePlaying()
        {
            playIcon.Kind = _playing ? MahApps.Metro.IconPacks.PackIconMaterialKind.Play : MahApps.Metro.IconPacks.PackIconMaterialKind.Pause;
            if (_playing)
            {
                player.Pause();
            }
            else
            {
                player.Play();
            }

            _playing = !_playing;
        }
        private void playBtn_Click(object sender, RoutedEventArgs e)
        {
            togglePlaying();
        }

        private void progressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDragging)
            {
                player.Position = TimeSpan.FromSeconds(progressSlider.Value);
                player.Play();
                player.Pause();
            }

            int hours = player.Position.Hours;
            int minutes = player.Position.Minutes;
            int seconds = player.Position.Seconds;
            currentPosition.Text = hours == 0 ? $"{minutes:00}:{seconds:00}" : $"{hours}:{minutes:00}:{seconds:00}";
        }
        private void progressSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isDragging = true;
            if (_playing) player.Pause();
        }

        private void progressSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isDragging = false;
            player.Position = TimeSpan.FromSeconds(progressSlider.Value);
            if (_playing) player.Play();
        }
        private void highlightPlayingMedia()
        {
            foreach (MediaItem media in _playList)
            {

                if (_currentPlaying == media.Path)
                {
                    media.Playing = 1;

                }
                else
                {
                    media.Playing = 0;
                }
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MediaItem selectedItem = (MediaItem)playlistListView.SelectedItem;
            startMedia(selectedItem.Path);
            highlightPlayingMedia();
        }

        private void removeFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var file = (MediaItem)playlistListView.SelectedItem;
            _playList.Remove(file);
        }

        private void playMediaAtIndex(int index)
        {
            startMedia(_playList[index].Path);
            highlightPlayingMedia();
        }

        private int currentMediaIndex()
        {
            for (int i = 0; i < _playList.Count; i++)
            {
                if (_playList[i].Path == _currentPlaying) return i;
            }
            return -1;
        }

        private void playNextMedia()
        {
            int curIndex = currentMediaIndex();
            if (curIndex == _playList.Count - 1 || curIndex == -1) return;
            playMediaAtIndex(curIndex + 1);
        }
        

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            playNextMedia();
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            int curIndex = currentMediaIndex();
            if (curIndex == 0 || curIndex == -1) return;
            playMediaAtIndex(curIndex - 1);
        }
        private int generateRandomInt(int range)
        {
            return random.Next(range);
        }
        private void player_MediaEnded(object sender, RoutedEventArgs e)
        {
            if ( (bool)shuffleCheck.IsChecked)
            {
                if (_playList.Count <= 1) return;
                int randomMediaIndex = generateRandomInt(_playList.Count);
                while (randomMediaIndex == currentMediaIndex())
                {
                    randomMediaIndex = generateRandomInt(_playList.Count);
                }
                playMediaAtIndex(randomMediaIndex);
                return;
            }
            playNextMedia();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_NEXT);
            UnregisterHotKey(_windowHandle, HOTKEY_PREV);
            UnregisterHotKey(_windowHandle, HOTKEY_STOP);

            string filename = "playlist.txt";
            if (!File.Exists(filename))
            {
                using (FileStream fs = File.Create(filename))
                {
                    StreamWriter sw = new StreamWriter(fs);
                    foreach (MediaItem media in _playList)
                    {
                        sw.WriteLine(media.Path);
                    }
                    fs.Close();
                }
            }
            using (FileStream fs = File.Open(filename, FileMode.Truncate, FileAccess.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    // discard the contents of the file by setting the length to 0
                    fs.SetLength(0);

                    foreach (MediaItem media in _playList)
                    {
                        sw.WriteLine(media.Path);
                    }
                }
                fs.Close();
            }

            //Save current playing
            if (_currentPlaying != string.Empty)
            {
                if (!File.Exists("playing.txt"))
                {
                    using (FileStream fs = File.Create("playing.txt"))
                    {
                        StreamWriter sw = new StreamWriter(fs);
                        sw.WriteLine(_currentPlaying);
                        sw.WriteLine(player.Position.TotalSeconds);
                        fs.Close();
                    }
                }
                using (FileStream fs = File.Open("playing.txt", FileMode.Truncate, FileAccess.ReadWrite))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        fs.SetLength(0);
                        sw.WriteLine(_currentPlaying);
                        sw.WriteLine(player.Position.TotalSeconds);
                    }
                    fs.Close();
                }
            }
            
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_NEXT = 1;
        private const int HOTKEY_PREV = 2;
        private const int HOTKEY_STOP = 3;
        //Modifiers:
        private const uint MOD_CONTROL = 0x0002; //CTRL
        //CAPS LOCK:
        private const uint VK_CAPITAL = 0x14;
        private const uint VK_A = 0x41;
        private const uint VK_S = 0x53;
        private const uint VK_D = 0x44;
        private IntPtr _windowHandle;
        private HwndSource _source;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_NEXT, MOD_CONTROL, VK_D); //CTRL + A
            RegisterHotKey(_windowHandle, HOTKEY_STOP, MOD_CONTROL, VK_S); //CTRL + S
            RegisterHotKey(_windowHandle, HOTKEY_PREV, MOD_CONTROL, VK_A); //CTRL + S
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_NEXT:
                            {
                                int vkey = ((int)lParam >> 16) & 0xFFFF;
                                if (vkey == VK_D)
                                {
                                    playNextMedia();
                                }
                                handled = true;
                                break;
                            }
                        case HOTKEY_STOP:
                            {
                                int vkey = ((int)lParam >> 16) & 0xFFFF;
                                if (vkey == VK_S)
                                {
                                    togglePlaying();
                                }
                                break;
                            }
                        case HOTKEY_PREV:
                            {
                                int vkey = ((int)lParam >> 16) & 0xFFFF;
                                if (vkey == VK_A)
                                {
                                    int curIndex = currentMediaIndex();
                                    if (curIndex == 0 || curIndex == -1) break;
                                    playMediaAtIndex(curIndex - 1);
                                }
                                break;
                            }
                    }
                    break;
            }
            return IntPtr.Zero;
        }
    }
}
