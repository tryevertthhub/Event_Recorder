using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Gma.System.MouseKeyHook;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Diagnostics;

namespace MediaRecorder
{
    public partial class MainWindow : Window
    {
        private IKeyboardMouseEvents m_GlobalHook;
        private List<Action> actions = new List<Action>();
        private bool isRecording = false;
        private bool isPlaying = false;
        private bool isDragging = false;

        // Mouse event flags
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_WHEEL = 0x0800;

        private const int HOTKEY_START_RECORD = 119; // F9
        private const int HOTKEY_STOP_RECORD = 120; // F10
        private const int HOTKEY_REPLAY = 121; // F11

        public double PlaybackSpeed { get; set; } = 1.0;
        private List<Action> loadedActions = null;

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        private string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        public MainWindow()
        {
            InitializeComponent();
            RegisterHotkeys();
            CreateDataDirectory(); // Ensure the data directory exists
        }

        private void RegisterHotkeys()
        {
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.KeyDown += GlobalHook_KeyDown;
        }

        private void GlobalHook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.F9)
            {
                if (!isRecording)
                {
                    StartRecording();
                }
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.F10)
            {
                if (isRecording)
                {
                    StopRecording();
                }
                else if (isPlaying)
                {
                    StopPlaying(); // Add method to stop playing
                }
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.F11)
            {
                if (!isPlaying)
                {
                    ReplayActions();
                }
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.F8)
            {
                this.Close();
            }
        }

        private void StopPlaying()
        {
            // Logic to stop playback
            isPlaying = false;
            this.status.Text = "Idle"; // Update status
            tbLog.AppendText("Playback stopped!\n");

            // Optional: You can also reset any UI elements if necessary
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "blueZzz.png");
            this.statusImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unregister the hotkeys when the window is closed
            m_GlobalHook.KeyDown -= GlobalHook_KeyDown;
            m_GlobalHook.Dispose();
            base.OnClosed(e);
        }


        // Ensure the data directory exists
        private void CreateDataDirectory()
        {
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
        }

        public void UpdateTitle(string newTitle)
        {
            title.Text = newTitle; // Update the title TextBox
        }

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            // Open the Library Window
            LibraryWindow libraryWindow = new LibraryWindow();
            libraryWindow.ShowDialog(); // Show it as a dialog

            // After the dialog is closed, check if a file was selected
            if (!string.IsNullOrEmpty(libraryWindow.SelectedFilePath))
            {
                lastRecordingFilePath = libraryWindow.SelectedFilePath;
                // PlayRecordedFile(libraryWindow.SelectedFilePath); // Play the selected file

            }
        }


        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            // Create and show the settings window
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }

        private void Toolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecording)
            {
                StartRecording();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
        }

        private void btnReplay_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                loadedActions = LoadActionsFromFile(lastRecordingFilePath);
                ReplayActions();
            }
        }

        private void StartRecording()
        {
            actions.Clear();
            tbLog.Text = ""; // Clear the TextBox when recording starts
            isRecording = true;
            m_GlobalHook = Hook.GlobalEvents();
            m_GlobalHook.MouseMove += OnMouseMove;
            m_GlobalHook.MouseDown += OnMouseDown;
            m_GlobalHook.MouseUp += OnMouseUp;
            m_GlobalHook.MouseWheelExt += OnMouseWheel; // Add scroll event capture

            tbLog.AppendText("Recording started!\n");
            this.status.Text = "Recording";
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "blueRecord.png");

            // Set the Source of the Image control
            this.statusImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        }

        private void StopRecording()
        {
            isRecording = false;
            m_GlobalHook.MouseMove -= OnMouseMove;
            m_GlobalHook.MouseDown -= OnMouseDown;
            m_GlobalHook.MouseUp -= OnMouseUp;
            m_GlobalHook.MouseWheelExt -= OnMouseWheel; // Remove scroll event capture
            m_GlobalHook.Dispose();


            tbLog.AppendText("Recording stopped!\n");
            this.status.Text = "Idle"; // Set status to Idle after stopping recording
        }

        // Save actions to the data directory


        private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isRecording && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                var newAction = new Action(ActionType.LEFT_DOWN, e.X, e.Y, DateTime.Now);
                actions.Add(newAction);
                isDragging = true; // Start dragging
                tbLog.AppendText($"LEFT_DOWN at ({e.X}, {e.Y})\n");
            }
        }

        private void OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isRecording)
            {
                if (isDragging)
                {
                    var newAction = new Action(ActionType.DRAG, e.X, e.Y, DateTime.Now);
                    actions.Add(newAction);
                    tbLog.AppendText($"DRAG at ({e.X}, {e.Y})\n");
                }
                else
                {
                    var newAction = new Action(ActionType.MOVE, e.X, e.Y, DateTime.Now);
                    actions.Add(newAction);
                    tbLog.AppendText($"MOVE at ({e.X}, {e.Y})\n");
                }
            }
        }

        private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isRecording && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                var newAction = new Action(ActionType.LEFT_UP, e.X, e.Y, DateTime.Now);
                actions.Add(newAction);
                isDragging = false; // End dragging
                tbLog.AppendText($"LEFT_UP at ({e.X}, {e.Y})\n");
            }
        }

        private void OnMouseWheel(object sender, MouseEventExtArgs e)
        {
            if (isRecording)
            {
                // Record scroll event (positive for up, negative for down)
                var newAction = new Action(ActionType.SCROLL, e.X, e.Y, DateTime.Now, e.Delta);
                actions.Add(newAction);
                tbLog.AppendText($"SCROLL at ({e.X}, {e.Y}) Delta: {e.Delta}\n");
            }
        }

        // Method to minimize the window
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Method to close the window
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveActions();
        }

        private List<Action> LoadActionsFromFile(string filePath)
        {
            var actions = new List<Action>();
            var actionsFromFile = File.ReadAllLines(filePath);

            foreach (var line in actionsFromFile)
            {
                var parts = line.Split(' ');
                var type = (ActionType)Enum.Parse(typeof(ActionType), parts[0]);
                var x = int.Parse(parts[1]);
                var y = int.Parse(parts[2]);
                var timeStamp = new DateTime(long.Parse(parts[3]));
                var delta = parts.Length > 4 ? int.Parse(parts[4]) : 0;

                actions.Add(new Action(type, x, y, timeStamp, delta));
            }

            return actions;
        }
        private void SaveActions()
        {
            // Create a SaveFileDialog to prompt the user for the file name and location
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Files (*.txt)|*.txt"; // Filter to save as .txt files
            saveFileDialog.DefaultExt = "txt"; // Default extension
            saveFileDialog.Title = "Save Recording As"; // Title of the dialog
            saveFileDialog.InitialDirectory = dataDirectory;
            // Show the SaveFileDialog and check if the user provided a file name
            bool? result = saveFileDialog.ShowDialog();

            if (result == true) // Only proceed if the user selected a valid file
            {
                string filePath = saveFileDialog.FileName; // Get the selected file path

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    foreach (var action in actions)
                    {
                        // Save the time as ticks to calculate delays later
                        sw.WriteLine($"{action.Type} {action.X} {action.Y} {action.TimeStamp.Ticks} {action.Delta}");
                    }
                }

                tbLog.AppendText($"Recording saved to: {filePath}\n");

                // Store the last recorded filename for replaying
                lastRecordingFilePath = filePath;
            }
            else
            {
                tbLog.AppendText("Recording save cancelled.\n");
            }
        }
        // This variable will store the last recording path
        private string lastRecordingFilePath;
        private CancellationTokenSource playbackCancellationTokenSource;
        // Modify ReplayActions to use lastRecordingFilePath

        //private async Task ReplayActions()
        //{
        //    if (loadedActions == null || loadedActions.Count == 0)
        //    {
        //        tbLog.AppendText("No preloaded actions available for replay.\n");
        //        return;
        //    }

        //    this.status.Text = "Playing...";
        //    isPlaying = true;
        //    playbackCancellationTokenSource = new CancellationTokenSource();

        //    Stopwatch stopwatch = new Stopwatch();
        //    stopwatch.Start();

        //    DateTime previousTime = loadedActions[0].TimeStamp;
        //    bool isDoubleClickDetected = false;

        //    try
        //    {
        //        for (int i = 0; i < loadedActions.Count; i++)
        //        {
        //            var action = loadedActions[i];

        //            if (playbackCancellationTokenSource.Token.IsCancellationRequested)
        //            {
        //                MessageBox.Show("Playback interrupted.");
        //                StopPlaying();
        //                return;
        //            }

        //            // Calculate delay based on action timestamps
        //            //  TimeSpan delay = action.TimeStamp - previousTime;
        //            TimeSpan delay = TimeSpan.FromMilliseconds((action.TimeStamp - previousTime).TotalMilliseconds / PlaybackSpeed);
        //            previousTime = action.TimeStamp;

        //            // Handle fast double-click events
        //            if (action.Type == ActionType.LEFT_DOWN && i < loadedActions.Count - 2)
        //            {
        //                var nextAction = loadedActions[i + 1];
        //                var nextNextAction = loadedActions[i + 2];

        //                // Check if the next actions are LEFT_UP and another LEFT_DOWN with a small interval
        //                if (nextAction.Type == ActionType.LEFT_UP &&
        //                    nextNextAction.Type == ActionType.LEFT_DOWN &&
        //                    (nextNextAction.TimeStamp - action.TimeStamp).TotalMilliseconds < 200)
        //                {
        //                    isDoubleClickDetected = true;
        //                }
        //            }

        //            // Adjust delay for rapid clicks (especially double clicks)
        //            if (isDoubleClickDetected && delay.TotalMilliseconds < 200)
        //            {
        //                await Task.Delay(200); // Enforce minimum delay for double-click simulation
        //                isDoubleClickDetected = false;
        //            }
        //            else
        //            {
        //                await Task.Delay((int)delay.TotalMilliseconds);
        //            }

        //            // Simulate the action
        //            SimulateAction(action.Type, action.X, action.Y, action.Delta);
        //        }
        //    }
        //    finally
        //    {
        //        isPlaying = false;
        //        this.status.Text = "Idle";
        //        string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "blueZzz.png");
        //        this.statusImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        //        MessageBox.Show("Replay finished!");
        //    }
        //}

        private async Task ReplayActions()
        {
            if (loadedActions == null || loadedActions.Count == 0)
            {
                tbLog.AppendText("No preloaded actions available for replay.\n");
                return;
            }

            this.status.Text = "Playing...";
            isPlaying = true;
            playbackCancellationTokenSource = new CancellationTokenSource();

            DateTime startTime = DateTime.Now;
            DateTime firstActionTime = loadedActions[0].TimeStamp;
            bool isDoubleClickDetected = false;

            try
            {
                for (int i = 0; i < loadedActions.Count; i++)
                {
                    var action = loadedActions[i];

                    if (playbackCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        MessageBox.Show("Playback interrupted.");
                        StopPlaying();
                        return;
                    }

                    // Calculate the target time for the current action based on playback speed
                    TimeSpan timeSinceFirstAction = action.TimeStamp - firstActionTime;
                    DateTime targetTime = startTime.AddMilliseconds(timeSinceFirstAction.TotalMilliseconds / PlaybackSpeed);

                    // Wait until the target time is reached
                    while (DateTime.Now < targetTime)
                    {
                        await Task.Delay(1); // Minimal delay to avoid high CPU usage
                    }

                    // Double-click detection
                    if (action.Type == ActionType.LEFT_DOWN && i < loadedActions.Count - 2)
                    {
                        var nextAction = loadedActions[i + 1];
                        var nextNextAction = loadedActions[i + 2];

                        if (nextAction.Type == ActionType.LEFT_UP &&
                            nextNextAction.Type == ActionType.LEFT_DOWN &&
                            (nextNextAction.TimeStamp - action.TimeStamp).TotalMilliseconds < 200)
                        {
                            isDoubleClickDetected = true;
                        }
                    }

                    // Enforce minimum delay for double-click simulation
                    if (isDoubleClickDetected && (DateTime.Now - targetTime).TotalMilliseconds < 100)
                    {
                        await Task.Delay(100);
                        isDoubleClickDetected = false;
                    }

                    // Simulate the action
                    SimulateAction(action.Type, action.X, action.Y, action.Delta);
                }
            }
            finally
            {
                isPlaying = false;
                this.status.Text = "Idle";
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "blueZzz.png");
                this.statusImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                MessageBox.Show("Replay finished!");
            }
        }



        private List<Action> AdjustActionTimestamps(List<Action> originalActions, double playbackSpeed)
        {
            if (originalActions == null || originalActions.Count < 2) return originalActions;

            var adjustedActions = new List<Action>();
            DateTime firstActionTime = originalActions[0].TimeStamp;
            adjustedActions.Add(new Action(originalActions[0].Type, originalActions[0].X, originalActions[0].Y, firstActionTime, originalActions[0].Delta));

            for (int i = 1; i < originalActions.Count; i++)
            {
                var originalInterval = originalActions[i].TimeStamp - originalActions[i - 1].TimeStamp;
                var adjustedInterval = TimeSpan.FromMilliseconds(originalInterval.TotalMilliseconds / playbackSpeed);

                DateTime adjustedTimestamp = adjustedActions[i - 1].TimeStamp + adjustedInterval;
                adjustedActions.Add(new Action(originalActions[i].Type, originalActions[i].X, originalActions[i].Y, adjustedTimestamp, originalActions[i].Delta));
            }

            return adjustedActions;
        }

        public async Task PlayRecordedFile(string lastRecordingFilePath)
        {
            loadedActions = LoadActionsFromFile(lastRecordingFilePath);
           // loadedActions = AdjustActionTimestamps(loadedActions, PlaybackSpeed);
            await ReplayActions();
        }
        // Helper method to simulate the mouse actions based on recorded types
        private void SimulateAction(ActionType type, int x, int y, int delta)
        {
            switch (type)
            {
                case ActionType.MOVE:
                    SetCursorPos(x, y);
                    break;
                case ActionType.LEFT_DOWN:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, 0);
                    break;
                case ActionType.LEFT_UP:
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
                    break;
                case ActionType.DRAG:
                    SetCursorPos(x, y); // Move cursor while holding down the left button
                    break;
                case ActionType.SCROLL:
                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)delta, 0);
                    break;
            }
        }

    }

    public class Action
    {
        public ActionType Type { get; }
        public int X { get; }
        public int Y { get; }
        public DateTime TimeStamp { get; }
        public int Delta { get; }

        public Action(ActionType type, int x, int y, DateTime timeStamp, int delta = 0)
        {
            Type = type;
            X = x;
            Y = y;
            TimeStamp = timeStamp;
            Delta = delta;
        }
    }

    public enum ActionType
    {
         MOVE,
        LEFT_DOWN,
        LEFT_UP,
        DRAG,
        SCROLL
    }
}