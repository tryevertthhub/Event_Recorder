using System.Windows;

namespace MediaRecorder
{
    public partial class SettingsWindow : Window
    {
        private MainWindow _mainWindow;
        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow; // Store reference to the MainWindow
            playbackSpeedSlider.Value = _mainWindow.PlaybackSpeed;
        }

        // Update the displayed value of the slider
        private void playbackSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ensure that the TextBlock is not null before updating its text
            if (playbackSpeedValue != null)
            {
                playbackSpeedValue.Text = $"Speed: {e.NewValue:F1}x"; // Format to 1 decimal place
            }
        }

        // Handle the save button click
        private void Cancle(object sender, RoutedEventArgs e)
        {
            // Save the playback speed setting
            double playbackSpeed = playbackSpeedSlider.Value;
            _mainWindow.PlaybackSpeed = playbackSpeed;

            // Optionally, close the settings window after saving
            this.Close();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save the playback speed setting
            double playbackSpeed = playbackSpeedSlider.Value;
            _mainWindow.PlaybackSpeed = playbackSpeed;

            // Optionally, close the settings window after saving
            this.Close();
        }
    }
}
