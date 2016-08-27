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
using System.Diagnostics;

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

        private string filePath = null;
        public Action<float> setVolumeDelegateDAC;
        public Action<float> setVolumeDelegateDefault;
        private List<FileFormats> InputFileFormats;
        private int songNumber = 1;
        Thread recordingThread;
        
        //NAudio Varaibles
        IWaveIn sourceStream = null;
        WaveOut waveOut = null;
        MusicPlayer musicPlayer;


        public MainWindow()
        {
            //Initialize stuff.
            InitializeComponent();
            InitializeDispatcherTimer();
            InitializeVolumeBars();
            InitializeFileFormats();
            InitializeComboBoxes();
            InitializeRecordingThread();
            InitializeEventHandlers();

            musicPlayer = new MusicPlayer(this, cbMusicOutput.SelectedIndex);

        }

        /*************************************************************************************************************
         * Initialization.
         *************************************************************************************************************/

        /// <summary>
        /// Initialize event handlers for the comoboBoxes and progress bars.
        /// </summary>
        private void InitializeEventHandlers()
        {
            cbInputDevices.SelectionChanged += cbInputDevices_SelectedIndexChanged;
            cbOutputDevices.SelectionChanged += cbOutputDevices_SelectedIndexChanged;

            songProgressBar.MouseMove += songProgressBar_MouseMove;
            songProgressBar.MouseLeftButtonDown += songProgressBar_MouseLeftButtonDown;
            //songProgressBarBackground.MouseLeftButtonDown += songProgressBar_MouseLeftButtonDown;
            //songProgressBarBackground.MouseMove += songProgressBar_MouseMove;

            mainGrid.MouseMove += songProgressBar_MouseMove;

            playList.MouseDoubleClick += playList_SongSelected;
            cbMusicOutput.SelectionChanged += cbMusicOutput_SelectedIndexChanged;
        }

        /// <summary>
        /// Initialize the recording thread for the microphone.
        /// </summary>
        private void InitializeRecordingThread()
        {
            recordingThread = new Thread(AttachInputMicrophone);
            recordingThread.Start();
        }

        /// <summary>
        /// Initialize the comoboBoxes for the mic input, output, and music output.
        /// </summary>
        private void InitializeComboBoxes()
        {
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
        /// Initialize the allowed file formats to be played by this players.
        /// </summary>
        private void InitializeFileFormats()
        {
            InputFileFormats = new List<FileFormats>();
            InputFileFormats.Add(new FileFormats("MP3", ".mp3"));
            InputFileFormats.Add(new FileFormats("WAV", ".wav"));
            InputFileFormats.Add(new FileFormats("AIFF", ".aiff"));
        }

        /// <summary>
        /// Initialize the volume bars to 25%
        /// </summary>
        private void InitializeVolumeBars()
        {
            userVolume.Percentage = othersVolume.Percentage = 25;
        }

        /// <summary>
        /// Starts the disaptcher timer, that fires every 100ms. 
        /// For every tick, call playerTimer_Tick;
        /// </summary>
        private void InitializeDispatcherTimer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += playerTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(100);
            dispatcherTimer.Start();
        }

        /*************************************************************************************************************
         * Event Handlers.
         *************************************************************************************************************/

        /// <summary>
        /// Handles the event when the mouse left button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progressBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Enable moving mouse to change the value.
            _isPressed = true;
            _scrollBarSource = (IInputElement)e.Source;
            setProgressBarPercentage();
        }

        /// <summary>
        /// Mouse has stopped clicking on the progress bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Disable moving mouse to change the value.
            _isPressed = false;
            _scrollBarSource = null;
        }

        /// <summary>
        /// The mouse is moving on the progress bar. If it's pressed, set the percentage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progressBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPressed)
            {
                setProgressBarPercentage();
            }
        }

        /// <summary>
        /// Set the progress bar percentage. (Applies to both the song tracker and the volume bars)
        /// </summary>
        public void setProgressBarPercentage()
        {
            CircularProgressBar bar = (CircularProgressBar)_scrollBarSource;
            double angle = GetAngleR(Mouse.GetPosition(_scrollBarSource), bar.Radius + bar.StrokeThickness * 2);
            if (musicPlayer.streamExists() || bar.Name != "songProgressBar")
                bar.Percentage = (100) * angle / (2 * Math.PI);

        }

        /// <summary>
        /// Set the progress bar time when the mouse is moved and pressed on the song tracker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void songProgressBar_MouseMove(object sender, MouseEventArgs e)
        {
            CircularProgressBar bar = (CircularProgressBar)_scrollBarSource;
            if (_isPressed && bar.Name == "songProgressBar")
            {
                setProgressBarTime();
            }
        }

        /// <summary>
        /// Set the progress bar time when the left mouse button has been clicked on the progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void songProgressBar_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            setProgressBarTime();
        }

        /// <summary>
        /// Set the song progress bar to the proper time based on the percentage.
        /// </summary>
        public void setProgressBarTime()
        {            
            if (musicPlayer.streamExists())
            {
                songProgressBar.timeProgress = (songProgressBar.Percentage / 100) * musicPlayer.getTotalSongSeconds();
                musicPlayer.setCurrentTime(songProgressBar.timeProgress);
            }
        }

        /// <summary>
        /// The mouse is pressed down, set local varaible to true and set the percentage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPressed = true;
            setProgressBarPercentage();
        }

        /// <summary>
        /// The play/pause button has been clicked. Either pause or play the current song, depending if the current song is playing or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playPauseButtonClicked(object sender, MouseButtonEventArgs e)
        {
            if (!_isPlaying)
            {
                activatePlayButton();
                playBtnClicked(sender, e);
            }
            else
            {
                musicPlayer.pause();
                activatePauseButton();
            }
        }

        /// <summary>
        /// The play button has been clicked. Play the song!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playBtnClicked(object sender, EventArgs e)
        {
            if (musicPlayer.play())
            {
                totalTrackTime.Text = String.Format("{0:00}:{1:00}", (int)musicPlayer.getWaveStream("dac").TotalTime.TotalMinutes, musicPlayer.getWaveStream("dac").TotalTime.Seconds);
                activatePlayButton();
                setDefaultVolume((float)userVolume.Percentage / 100, (float)othersVolume.Percentage / 100);
            }

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

        /// <summary>
        /// A song has been selected from the listview by a double click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playList_SongSelected(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(playList);
            HitTestResult hit = VisualTreeHelper.HitTest(playList, pt);

            //ListViewHitTestInfo hit = playList.HitTest(e.Location);
            if (hit != null)
            {
                int songNumber = playList.SelectedIndex;
                musicPlayer.loadSongBySongNumber(songNumber);
                playBtnClicked(sender, e);

                setDefaultVolume((float)userVolume.Percentage / 100, (float)othersVolume.Percentage / 100);
            };
        }
        /// <summary>
        /// The combo box for the music output has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbMusicOutput_SelectedIndexChanged(object sender, EventArgs e)
        {
            musicPlayer.virtualCableDeviceNumber = cbMusicOutput.SelectedIndex;
            musicPlayer.loadSongBySongNumber(musicPlayer.currentSongNumber);
            EZDJ.Properties.Settings.Default.micOutputIndex = cbMusicOutput.SelectedIndex;
            EZDJ.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// The combo box for the output device has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void cbOutputDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            recordingThread = new Thread(AttachInputMicrophone);
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
            recordingThread = new Thread(AttachInputMicrophone);
            recordingThread.Start();

            EZDJ.Properties.Settings.Default.micInputIndex = cbInputDevices.SelectedIndex;
            EZDJ.Properties.Settings.Default.Save();
        }

        /*************************************************************************************************************
         * Methods.
         *************************************************************************************************************/

        /// <summary>
        /// Make the pause button active. 
        /// </summary>
        private void activatePauseButton()
        {
            playStopImage.Source = new BitmapImage(new Uri(@"Resources/Play.png", UriKind.RelativeOrAbsolute));
            _isPlaying = false;
        }

        /// <summary>
        /// Make the play button active.
        /// </summary>
        private void activatePlayButton()
        {
            playStopImage.Source = new BitmapImage(new Uri(@"Resources/Pause.png", UriKind.RelativeOrAbsolute));
            _isPlaying = true;
        }

        /// <summary>
        /// Get the angle of where the progress bar should be based on the position and radius of the progress bar.
        /// </summary>
        /// <param name="pos">Position of where the mouse was clicked</param>
        /// <param name="radius">Radius of the progress bar</param>
        /// <returns></returns>
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
        /// Open the settings grid which overlays on top of everything.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Show the settings grid.
        /// </summary>
        private void showSettings()
        {
            settingsGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hide the settings grid.
        /// </summary>
        private void hideSettings()
        {
            settingsGrid.Visibility = Visibility.Collapsed;
        }


        /// <summary>
        /// Set the volume for the dac and current default output sound device. 
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="volume2"></param>
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
        /// Stop all playback and recording of the mixer. Switch to the new input device and start recording.
        /// </summary>
        public void AttachInputMicrophone()
        {
            InitializeWaveOutWaveIn();

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

            //set the input waveIn to the input device selected
            WaveInProvider waveIn = new WaveInProvider(sourceStream);

            //waveOut = Where the mic output will go

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

        /// <summary>
        /// Initialize the wave out and wave in devices.
        /// </summary>
        private void InitializeWaveOutWaveIn()
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
            waveOut = new WaveOut();
        }

        /*************************************************************************************************************
         * Helper Methods.
         *************************************************************************************************************/

        /// <summary>
        /// Convert nano seconds to millisconds.
        /// </summary>
        /// <param name="nanoseconds"></param>
        /// <returns></returns>
        public static double Convert100NanosecondsToMilliseconds(double nanoseconds)
        {
            // One million nanoseconds in 1 millisecond, 
            // but we are passing in 100ns units...
            return nanoseconds * 0.0001;
        }

        /// <summary>
        /// The open file/song button has been clicked. Open up a file dialogue and process the file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                double.TryParse(so.Properties.System.Media.Duration.Value.ToString(), out nanoseconds);
                Console.WriteLine("NanaoSeconds: {0}", nanoseconds);

                double seconds = Convert100NanosecondsToMilliseconds(nanoseconds) / 1000;
                TimeSpan t = TimeSpan.FromSeconds(seconds);

                string duration = t.ToString(@"mm\:ss");
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                Song songToAdd = new Song(fileName, null, duration, filePath, true, songNumber);

                musicPlayer.addSong(songToAdd);
                addSongToPlayList(songToAdd);

            }

        }

        /// <summary>
        /// Add the song to the current song playist.
        /// </summary>
        /// <param name="songToAdd">The song you want to add to the playlist.</param>
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

            songNumber++;

        }


        /// <summary>
        /// The tick timer. It checks and updates the progress bar's percentage whether or not the song should be stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                songProgressBar.Percentage = 0;
                activatePauseButton();
            }
        }

        /// <summary>
        /// The app is being closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeApp(object sender, MouseButtonEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
            Application.Current.Shutdown();
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
