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
       

        public MainWindow()
        {
            InitializeComponent();
            RegisterHotkeys();
            CreateDataDirectory(); // Ensure the data directory exists
        }

    }

}