using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Ookii.Dialogs.Wpf;
using System.ComponentModel;
using System.IO;

namespace WinUx
{
    /// <summary>
    /// Interaction logic for CustomExplorer.xaml
    /// </summary>
    public partial class CustomExplorer : UserControl, INotifyPropertyChanged
    {
        private string _currentDirectory;
        private string _previousDirectory = "";
        public event EventHandler<string> CurrentDirectoryChanged;
        private readonly IniFile _iniFile;

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                if (_currentDirectory != value)
                {
                    _currentDirectory = value;
                    CurrentDirectoryChanged?.Invoke(this, _currentDirectory);
                    _iniFile.Write("LastFolder", _currentDirectory);
                    OnPropertyChanged(nameof(CurrentDirectory));
                    UpdateWebBrowser();
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CustomExplorer()
        {
            InitializeComponent();
            wbLocal.LoadCompleted += wbLocal_LoadCompleted;
            _iniFile = new IniFile("settings.ini");
            string lastFolder = _iniFile.Read("LastFolder");
            if (Directory.Exists(lastFolder))
            {
                CurrentDirectory = lastFolder;
            }
            else
            {
                CurrentDirectory = Environment.CurrentDirectory;
            }
        }

        private void WebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void wbLocal_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            CurrentDirectory = e.Uri.LocalPath;    
        }

        private string GetCurrentWebBrowserDirectory()
        {
            if (wbLocal.Source != null)
            {
                string url = wbLocal.Source.AbsoluteUri;

                if (url.StartsWith("file:///"))
                {
                    // Convert "file:///" to a local path
                    return new Uri(url).LocalPath;
                }
            }
            return string.Empty;
        }

        private void UpdateWebBrowser()
        {
            if (!string.IsNullOrEmpty(CurrentDirectory))
            {
                _previousDirectory = GetCurrentWebBrowserDirectory();
                wbLocal.Navigate(CurrentDirectory);
                btnWbLocalUri.Content = CurrentDirectory;
                btnWbLocalUri.ToolTip = CurrentDirectory;
            }
        }

        private void btnWbLocalUri_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select a folder",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.SelectedPath;
                CurrentDirectory = selectedPath;
            }
        }

        private void btnGotoParent_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentDirectory))
            {
                DirectoryInfo parentDir = Directory.GetParent(CurrentDirectory);
                if (parentDir != null)
                {
                    CurrentDirectory = parentDir.FullName;
                }
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            wbLocal.Refresh();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            CurrentDirectory = Environment.CurrentDirectory;
        }

        private void btnGotoPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_previousDirectory != "" && CurrentDirectory != _previousDirectory)
            {
                CurrentDirectory = _previousDirectory;
            }
        }
    }
}
