using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
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

namespace WinUx
{
    public partial class MainWindow : Window
    {
        private readonly IniFile _iniFile;
        public MainWindow()
        {
            InitializeComponent();
            ribbon.QuickAccessToolBar = null;
            ribbon.Margin = new Thickness(0, -5, 0, 0);


            CustomExplorer.CurrentDirectoryChanged += CustomExplorer_CurrentDirectoryChanged;
            _iniFile = new IniFile("settings.ini");
            string lastFolder = _iniFile.Read("LastFolder");
            if (Directory.Exists(lastFolder))
            {
                LinuxExplorer.SetWindowTargetPath(lastFolder);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            LinuxExplorer.Explorer_Unloaded();
        }

        private void ribbon_Loaded(object sender, RoutedEventArgs e)
        {
            Grid child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            if (child != null)
            {
                child.RowDefinitions[0].Height = new GridLength(5);
            }
        }

        private void Login()
        {
            //LoginForm loginForm = new LoginForm();

            //if (loginForm.ShowDialog() == true)
            //{
            //    string host = loginForm.HostName;
            //    int port = loginForm.Port;
            //    string username = loginForm.Username;
            //    string password = loginForm.Password;

            //    // Attempt to connect to SFTP server
            //    bool isConnected = LinuxExplorer.Login(host, port, username, password);

            //    if (!isConnected)
            //    {
            //        ShowLoginForm();
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("Login canceled!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    Close();
            //}

        }


        private void wbLocal_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void wbLocal_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    File.Move(file, System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetFileName(file)));
                }
            }
        }

        private void Window_PreviewMouseClick(object sender, MouseButtonEventArgs e)
        {
            if (!dgStatus.IsMouseOver)
            {
                dgStatus.UnselectAll();
                dgStatus.Focusable = false;
                Keyboard.ClearFocus();
            }
        }

        private void CustomExplorer_CurrentDirectoryChanged(object sender, string newPath)
        {
            LinuxExplorer.SetWindowTargetPath(newPath);
        }




        #region Menu bar
        private void MenuItem_Login_Click(object sender, RoutedEventArgs e)
        {
            LoginForm loginForm = new LoginForm();
            loginForm.Owner = this;

            loginForm.OnLoginAttempt += (host, port, username, password) =>
            {
                bool loginSuccess = LinuxExplorer.Login(host, port, username, password);

                if (loginSuccess)
                {
                    MessageBox.Show("Connected Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    loginForm.DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Login failed. Please check your credentials.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            loginForm.ShowDialog();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to exit?",
                                              "Exit Confirmation",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string author = GetAuthorName();
            MessageBox.Show($"WinUx - Custom File Explorer & SFTP Client\n" +
                            $"Version: 1.0.0\n" +
                            $"Developed by: {author}",
                            "About WinUx",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
        static string GetAuthorName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            return attribute?.Company ?? "Unknown";
        }
        #endregion Menu bar


    }
}
