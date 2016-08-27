using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using Microsoft.WindowsAPICodePack.Shell;
using NAudio.CoreAudioApi;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace EZDJ
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private bool _isPlaying = true;
        private bool _isPressed = false;
        private bool _settingsOpen = false;
        private IInputElement _scrollBarSource = null;

        //NAudio Varaibles
        private string filePath = null;
        public Action<float> setVolumeDelegateDAC;
        public Action<float> setVolumeDelegateDefault;
        private List<FileFormats> InputFileFormats;
        private int songNumber = 1;
        IWaveIn sourceStream = null;
        WaveOut waveOut = null;
        MusicPlayer musicPlayer;
        Thread recordingThread;


        public MainWindow()
        {
            InitializeComponent();


            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += playerTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(100);
            dispatcherTimer.Start();

            CircularProgressBar userVolumeProgress = (CircularProgressBar)userVolumeGrid.Children[1];
            CircularProgressBar othersVolumeProgress = (CircularProgressBar)othersVolumeGrid.Children[1];
            userVolumeProgress.Percentage = othersVolumeProgress.Percentage = 25;

            InputFileFormats = new List<FileFormats>();
            InputFileFormats.Add(new FileFormats("MP3", ".mp3"));
            InputFileFormats.Add(new FileFormats("WAV", ".wav"));
            InputFileFormats.Add(new FileFormats("AIFF", ".aiff"));

            populateInputDevices();
            populateOutputDevices();

            //populate the combobox with current input devices (mic, digital cable, etc.)
            List<NAudio.Wave.WaveOutCapabilities> sources = new List<NAudio.Wave.WaveOutCapabilities>();

            for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                sources.Add(NAudio.Wave.WaveOut.GetCapabilities(i));

            }

            cbInputDevices.SelectedIndex = EZDJ.Properties.Settings.Default.micInputIndex;
            cbOutputDevices.SelectedIndex = EZDJ.Properties.Settings.Default.micOutputIndex;

            try
            {
                cbMusicOutput.SelectedIndex = EZDJ.Properties.Settings.Default.micOutputIndex;
            }
            catch
            {
                cbMusicOutput.SelectedIndex = 0;
                EZDJ.Properties.Settings.Default.micOutputIndex = 0;
                EZDJ.Properties.Settings.Default.Save();
            }

            recordingThread = new Thread(attachInputMicrophone);
            recordingThread.Start();

            cbInputDevices.SelectionChanged += cbInputDevices_SelectedIndexChanged;
            cbOutputDevices.SelectionChanged += cbOutputDevices_SelectedIndexChanged;
            songProgressBar.MouseMove += songProgressBar_MouseMove;
            songProgressBar.MouseLeftButtonDown += songProgressBar_MouseLeftButtonDown;

            //initialize browser youtube stuff here.
            /*
            webClient = new WebClient();
            browser = new WebBrowser();
            browser.Navigate("http://www.youtube-mp3.org/");
            browser.DocumentCompleted += browser_DocumentCompleted;
            browser.ScriptErrorsSuppressed = true;
            tbYoutubeAddURL.KeyUp += tbYoutubeAddURL_KeyUp;
            //loadingIcon.Image = Image.FromFile(@"C:\Users\Mark\Documents\Visual Studio 2013\Projects\YouTube to MP3\images\loading.gif");
            */

            musicPlayer = new MusicPlayer(this, cbMusicOutput.SelectedIndex);
            //youTubeDownloader = new YouTubeDownloader(this, musicPlayer);

            //volumeSlider.Volume = (float)0.254;

            playList.MouseDoubleClick += playList_SongSelected;
            cbMusicOutput.SelectionChanged += cbMusicOutput_SelectedIndexChanged;
        }


        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Enable moving mouse to change the value.
            _isPressed = true;
            _scrollBarSource = (IInputElement)e.Source;
            setProgressBarPercentage();
        }

        private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Disable moving mouse to change the value.
            _isPressed = false;
            _scrollBarSource = null;
        }

        private void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPressed)
            {
                setProgressBarPercentage();
            }
        }

        public void setProgressBarPercentage()
        {
            CircularProgressBar bar = (CircularProgressBar)_scrollBarSource;
            Grid barParentGrid = (Grid)bar.Parent;
            CircularProgressBar progressBar = (CircularProgressBar)barParentGrid.Children[1];

            double angle = GetAngleR(Mouse.GetPosition(_scrollBarSource), bar.Radius + bar.StrokeThickness * 2);
            progressBar.Percentage = (100) * angle / (2 * Math.PI);

        }

        public void songProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPressed)
            {
                setProgressBarTime();
            }
        }

        public void songProgressBar_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            setProgressBarTime();
        }

        public void setProgressBarTime()
        {
            songProgressBar.timeProgress = (songProgressBar.Percentage / 100) * musicPlayer.getTotalSongSeconds();
            if (musicPlayer.streamExists())
            {
                musicPlayer.setCurrentTime(songProgressBar.timeProgress);
            }
        }

        private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPressed = true;

            CircularProgressBar bar = (CircularProgressBar)_scrollBarSource;
            Grid barParentGrid = (Grid)bar.Parent;
            CircularProgressBar progressBar = (CircularProgressBar)barParentGrid.Children[1];
            double angle = GetAngleR(Mouse.GetPosition(_scrollBarSource), bar.Radius + bar.StrokeThickness * 2);
            progressBar.Percentage = (100) * angle / (2 * Math.PI);
        }

        private void playStopImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (!_isPlaying)
            {
                activatePlayButton();
                btnPlay_Click(sender, e);
            }
            else
            {
                musicPlayer.pause();
                activatePauseButton();

            }
        }
        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (musicPlayer.play())
            {
                totalTrackTime.Text = String.Format("{0:00}:{1:00}", (int)musicPlayer.getWaveStream("dac").TotalTime.TotalMinutes, musicPlayer.getWaveStream("dac").TotalTime.Seconds);
                activatePlayButton();
                setDefaultVolume((float)userVolume.Percentage / 100, (float)othersVolume.Percentage / 100);
            }

        }
        private void activatePauseButton()
        {
            playStopImage.Source = new BitmapImage(new Uri(@"Resources/Play.png", UriKind.RelativeOrAbsolute));
            _isPlaying = false;
        }

        private void activatePlayButton()
        {
            playStopImage.Source = new BitmapImage(new Uri(@"Resources/Pause.png", UriKind.RelativeOrAbsolute));
            _isPlaying = true;
        }

        public static double GetAngleR(Point pos, double radius)
        {
            //Calculate out the distance(r) between the center and the position
            Point center = new Point(radius, radius);
            double xDiff = center.X - pos.X;
            double yDiff = center.Y - pos.Y;
            double r = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);

            //Calculate the angle
            double angle = Math.Acos((center.Y - pos.Y) / r);
            if (pos.X < radius)
                angle = 2 * Math.PI - angle;
            if (Double.IsNaN(angle))
                return 0.0;
            else
                return angle;
        }

        /// <summary>
        /// Drag the window when the mouse button is down anywhere on the grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void myGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.Source is Grid)
            {
                Grid sourceGrid = (Grid)e.Source;
                if (sourceGrid.Name == "mainGrid")
                    if (e.ChangedButton == MouseButton.Left)
                        this.DragMove();
            }
        }

        private void closeApp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown(0);
            Environment.Exit(0);

        }

        private void openSettings(object sender, MouseButtonEventArgs e)
        {
            if (!_settingsOpen)
            {
                settingsIcon.Source = new BitmapImage(new Uri(@"Resources/backArrow.png", UriKind.RelativeOrAbsolute));
                _settingsOpen = true;
                showSettings();
            }
            else
            {
                settingsIcon.Source = new BitmapImage(new Uri(@"Resources/settings-icon.png", UriKind.RelativeOrAbsolute));
                _settingsOpen = false;
                hideSettings();

            }
        }

        private void showSettings()
        {
            settingsGrid.Visibility = Visibility.Visible;
        }

        private void hideSettings()
        {
            settingsGrid.Visibility = Visibility.Collapsed;
        }



        private void playList_SongSelected(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(playList);
            HitTestResult hit = VisualTreeHelper.HitTest(playList, pt);

            //ListViewHitTestInfo hit = playList.HitTest(e.Location);
            if (hit != null)
            {

                int songNumber = playList.SelectedIndex;
                musicPlayer.loadSongBySongNumber(songNumber);
                btnPlay_Click(sender, e);

                setDefaultVolume((float)userVolume.Percentage / 100, (float)othersVolume.Percentage / 100);
            };
        }

        private void setDefaultVolume(float volume, float volume2)
        {
            if (setVolumeDelegateDAC != null)
            {
                setVolumeDelegateDAC(volume2);
            }
            if (setVolumeDelegateDefault != null)
            {
                setVolumeDelegateDefault(volume);
            }
        }

        /// <summary>
        /// Populates the combo boxes with the current availble output devices (speakers, headphones, monitors, etc.)
        /// </summary>
        private void populateOutputDevices()
        {
            for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                var capabilities = WaveOut.GetCapabilities(deviceId);

                string productName = capabilities.ProductName;
                int index = capabilities.ProductName.IndexOf("(");
                if (index > 0)
                    productName = productName.Substring(0, index);

                cbOutputDevices.Items.Add(String.Format("{0}", productName));
                cbMusicOutput.Items.Add(String.Format("{0}", productName));
            }
            if (cbOutputDevices.Items.Count > 0)
            {
                cbOutputDevices.SelectedIndex = 0;
                cbMusicOutput.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Populates the combo boxe with the input devices (mic, etc.)
        /// </summary>
        private void populateInputDevices()
        {
            MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
            MMDeviceCollection deviceCol = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            Collection<MMDevice> devices = new Collection<MMDevice>();

            foreach (MMDevice device in deviceCol)
            {
                devices.Add(device);
            }

            //Give each item in the list of devices a name and a value. Add it to the combobox.
            foreach (var item in devices)
            {
                ComboboxItem cbItem = new ComboboxItem();

                string productName = item.FriendlyName;
                int index = item.FriendlyName.IndexOf("(");
                if (index > 0)
                    productName = productName.Substring(0, index);

                cbItem.Text = productName;
                cbItem.Value = item;

                cbInputDevices.Items.Add(cbItem);
            }

        }

        /// <summary>
        /// Stop all playback and recording of the mixer. Switch to the new input device and start recording.
        /// </summary>
        public void attachInputMicrophone()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
            if (sourceStream != null)
            {
                sourceStream.StopRecording();
                sourceStream.Dispose();
                sourceStream = null;
            }

            System.Threading.Thread.Sleep(300);//to prevent looping of sound when switching to the virtual audio cable

            int deviceNumber = -1;

            if (!cbInputDevices.Dispatcher.CheckAccess())
            {
                Action action = () => deviceNumber = cbInputDevices.SelectedIndex;
                cbInputDevices.Dispatcher.Invoke(action);
            }
            else
            {
                deviceNumber = cbInputDevices.SelectedIndex;
            }

            // int deviceNumber = cbInputDevices.SelectedIndex;

            //get the selected input device
            // sourceStream = new WaveIn();
            //sourceStream.DeviceNumber = deviceNumber;
            //sourceStream.WaveFormat = new NAudior.Wave.WaveFormat(96000, NAudio.Wave.WaveIn.GetCapabilities(deviceNumber).Channels);
            //sourceStream.WaveFormat = new WaveFormat(16000, 1);
            if (deviceNumber == -1)
            {
                sourceStream = new WasapiCapture((MMDevice)cbInputDevices.Items[0]);
            }
            else
            {
                if (!cbInputDevices.Dispatcher.CheckAccess())
                {
                    Action action = () => sourceStream = new WasapiCapture((MMDevice)(cbInputDevices.SelectedItem as ComboboxItem).Value);
                    cbInputDevices.Dispatcher.Invoke(action);
                }
                else
                {
                    sourceStream = new WasapiLoopbackCapture((MMDevice)cbInputDevices.SelectedItem);

                }

            }

            //sourceStream.WaveFormat = new WaveFormat(8000, 2);

            //set the input waveIn to the input device selected
            WaveInProvider waveIn = new WaveInProvider(sourceStream);


            //waveOut = new NAudio.Wave.DirectSoundOut();

            //waveOut = Where the mic output will go
            waveOut = new WaveOut();
            if (!cbOutputDevices.Dispatcher.CheckAccess())
            {
                Action action = () => waveOut.DeviceNumber = cbOutputDevices.SelectedIndex; ;
                cbOutputDevices.Dispatcher.Invoke(action);
            }
            else
            {
                waveOut.DeviceNumber = cbOutputDevices.SelectedIndex;

            }
            //waveOut.DeviceNumber = 0;//digital audio cable
            waveOut.DesiredLatency = 120;
            waveOut.Init(waveIn);


            waveOut.Play();
            sourceStream.StartRecording();

        }

        private void cbMusicOutput_SelectedIndexChanged(object sender, EventArgs e)
        {
            musicPlayer.virtualCableDeviceNumber = cbMusicOutput.SelectedIndex;
            musicPlayer.loadSongBySongNumber(musicPlayer.currentSongNumber);
            EZDJ.Properties.Settings.Default.micOutputIndex = cbMusicOutput.SelectedIndex;
            EZDJ.Properties.Settings.Default.Save();

        }

        void cbOutputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            recordingThread = new Thread(attachInputMicrophone);
            recordingThread.Start();

            EZDJ.Properties.Settings.Default.micOutputIndex = cbOutputDevices.SelectedIndex;
            EZDJ.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// When the combo box is changed, change the recording thread to a new one. 
        /// Also save the choice for when the program is opened again.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">Event argument</param>
        public void cbInputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            recordingThread = new Thread(attachInputMicrophone);
            recordingThread.Start();

            EZDJ.Properties.Settings.Default.micInputIndex = cbInputDevices.SelectedIndex;
            EZDJ.Properties.Settings.Default.Save();
        }

        public static double Convert100NanosecondsToMilliseconds(double nanoseconds)
        {
            // One million nanoseconds in 1 millisecond, 
            // but we are passing in 100ns units...
            return nanoseconds * 0.0001;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string allExtensions = "*.mp3;*.aiff;*.wav";
            //string allExtensions = string.Join(";", (from f in InputFileFormats select "*" + f.Extension).ToArray());
            openFileDialog.Filter = String.Format("All Supported Files|{0}|All Files (*.*)|*.*", allExtensions);
            openFileDialog.FilterIndex = 1;

            bool? result = openFileDialog.ShowDialog();
            if (result ?? false)
            {
                // Filename to process was passed to RunWorkerAsync(), so it's available
                // here in DoWorkEventArgs object.
                filePath = openFileDialog.FileName;


                ShellFile so = ShellFile.FromFilePath(filePath);
                double nanoseconds;
                double.TryParse(so.Properties.System.Media.Duration.Value.ToString(),
                out nanoseconds);
                Console.WriteLine("NanaoSeconds: {0}", nanoseconds);
                //if (nanoseconds > 0)
                //{
                double seconds = Convert100NanosecondsToMilliseconds(nanoseconds) / 1000;
                TimeSpan t = TimeSpan.FromSeconds(seconds);

                string duration = t.ToString(@"mm\:ss");
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                //}

                Song songToAdd = new Song(fileName, null, duration, filePath, true, songNumber);

                //musicPlayer.loadSong(fileName);
                musicPlayer.addSong(songToAdd);
                addSongToPlayList(songToAdd);


            }
            // attachInputMicrophone();
        }

        public void addSongToPlayList(Song songToAdd)
        {
            ListViewItem songItem = new ListViewItem();
            songItem.Content = songToAdd.title;

            if (!playList.Dispatcher.CheckAccess())
            {
                Action action = () => playList.Items.Add(songItem);
                playList.Dispatcher.Invoke(action);
            }
            else
            {
                playList.Items.Add(songItem);
            }

            //tbYoutubeAddURL.Text = "";
            songNumber++;

            /*
            if (songToAdd.imageURL != null)
            {
                picYouTubePicture.ImageLocation = songToAdd.imageURL;
            }
            else
            {
                picYouTubePicture.ImageLocation = "";
            }
            */

            //setProgressBarState(false);
            //setLoadingIconState(false);

        }


        private void playerTimer_Tick(object sender, EventArgs e)
        {
            //if (waveOutToMe != null && fileWaveStreamDAC != null && waveOutToSkype != null)
            if (musicPlayer.streamExists() && musicPlayer.getPlaybackState() != PlaybackState.Stopped)
            {
                TimeSpan elapsedTime = (musicPlayer.getPlaybackState() == PlaybackState.Stopped) ? TimeSpan.Zero : musicPlayer.getCurrentSongTime();
                if (musicPlayer.getCurrentSongPosition() >= musicPlayer.getCurrentSongLength())
                {
                    //button3_Click(sender, e);
                    musicPlayer.stop();
                }
                else
                {
                    //trackFileLocation.Value = (int)currentTime.TotalMilliseconds * 10;
                    float totalTime = (float)musicPlayer.getWaveStream("dac").TotalTime.TotalMinutes;
                    float percentage = ((float)elapsedTime.TotalMinutes / totalTime) * 100;
                    songProgressBar.Percentage = percentage;
                    songProgressBar.timeProgress = elapsedTime.TotalSeconds;
                    currentTrackTime.Text = String.Format("{0:00}:{1:00}", (int)elapsedTime.TotalMinutes, elapsedTime.Seconds);
                    
                }
            }
            else
            {
                songProgressBar.Percentage= 0;
                activatePauseButton();
            }
        }

        ~MainWindow()
        {
            filePath = null;
            setVolumeDelegateDAC = null;
            setVolumeDelegateDefault = null;
            InputFileFormats = null;
            songNumber = 0;
            sourceStream = null;
            waveOut = null;
            musicPlayer = null;
        }

    }

}
