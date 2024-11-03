using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MediaRecorder
{
    public partial class LibraryWindow : Window
    {
        private string selectedFilePath;
        private string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        public string SelectedFilePath { get; private set; }
        public LibraryWindow()
        {
            InitializeComponent();
            LoadRecordedFiles();
        }

        // Load recorded files from the data directory
        private void LoadRecordedFiles()
        {
            if (Directory.Exists(dataDirectory))
            {
                var recordedFiles = Directory.GetFiles(dataDirectory, "*.txt"); // Load only .txt files

                foreach (var file in recordedFiles)
                {
                    recordedList.Items.Add(Path.GetFileName(file)); // Add file names to the ListBox
                }
            }
            else
            {
                MessageBox.Show("No recorded files found.");
            }
        }

        // Event triggered when a recorded file is selected
        private void recordedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recordedList.SelectedItem != null)
            {
                string selectedFile = recordedList.SelectedItem.ToString();
                selectedFilePath = Path.Combine(dataDirectory, selectedFile);

                // Display file details
                fileDetails.Text = $"Selected File: {selectedFile}";
                playbackLog.Text = File.ReadAllText(selectedFilePath); // Show file content in log

                // Calculate and display recorded time
                recordedTime.Text = GetRecordedTime(selectedFilePath);

                btnPlay.IsEnabled = true; // Enable the play button
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.UpdateTitle(selectedFile);

            }
        }

        // Event triggered when the Play button is clicked
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                SelectedFilePath = selectedFilePath; // Set the selected file path
                this.Close(); // Close LibraryWindow
            }
        }

        // Method to play the recorded actions
        private void PlayRecordedFile(string filePath)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.PlayRecordedFile(filePath); // Use MainWindow's replay functionality

            playbackLog.AppendText("\nPlayback started...");
        }

        // Method to get recorded time from the action file
        private string GetRecordedTime(string filePath)
        {
            // Ensure the file exists and is not empty
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                return "Duration: 0 sec";

            var lines = File.ReadAllLines(filePath);
            long? startTime = null;
            long? endTime = null;

            foreach (var line in lines)
            {
                // Split the line into parts
                var parts = line.Split(' ');

                // Ensure there's enough parts to check (the third part for the timestamp)
                if (parts.Length < 3)
                    continue;

                // Attempt to parse the third part as a long (the ticks)
                if (long.TryParse(parts[2], out long tickValue))
                {
                    // Log for debugging
                    Console.WriteLine($"Parsed tick value: {tickValue}");

                    // Initialize start and end time
                    if (startTime == null || tickValue < startTime)
                    {
                        startTime = tickValue; // Set start time
                    }
                    if (endTime == null || tickValue > endTime)
                    {
                        endTime = tickValue; // Update end time
                    }
                }
                else
                {
                    // Log for debugging if parsing fails
                    Console.WriteLine($"Failed to parse tick value from line: {line}");
                }
            }

            // If start or end time is not found, return duration as 0
            if (startTime == null || endTime == null)
                return "Duration: 0 sec";

            // Calculate the duration in seconds
            var durationTicks = endTime.Value - startTime.Value;
            var duration = TimeSpan.FromTicks(durationTicks);
            return $"Duration: {Math.Round(duration.TotalSeconds)} sec";
        }
    }
}
