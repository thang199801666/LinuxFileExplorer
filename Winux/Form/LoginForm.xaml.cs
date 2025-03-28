using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WinUx; // Import IniFile class

namespace WinUx
{
    public partial class LoginForm : Window
    {
        private IniFile iniFile = new IniFile("settings.ini");
        private Dictionary<string, string> userCredentials = new Dictionary<string, string>();
        public event Action<string, int, string, string> OnLoginAttempt;

        public LoginForm()
        {
            InitializeComponent();
            LoadCredentials();
        }

        private void LoadCredentials()
        {
            // Load usernames from the INI file and populate the ComboBox
            int userCount = 1;
            while (iniFile.KeyExists($"Username{userCount}", "Users"))
            {
                string username = iniFile.Read($"Username{userCount}", "Users");
                string password = iniFile.Read($"Password{userCount}", "Users");

                cmbUsername.Items.Add(username);
                userCredentials[username] = password;
                userCount++;
            }

            // Load Host and Port from INI
            txtHostName.Text = iniFile.Read("Host", "SFTP");
            txtPort.Text = iniFile.Read("Port", "SFTP");
        }

        private void cmbUsername_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUsername.SelectedItem != null)
            {
                string selectedUser = cmbUsername.SelectedItem.ToString();
                if (userCredentials.ContainsKey(selectedUser))
                {
                    txtPassword.Password = userCredentials[selectedUser];
                }
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string host = txtHostName.Text;
            int port = int.TryParse(txtPort.Text, out int parsedPort) ? parsedPort : 22;
            string username = cmbUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(host) || 
                string.IsNullOrWhiteSpace(username) || 
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please fill in all fields.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkRemember.IsChecked == true)
            {
                SaveCredentials(host, password, host, port.ToString());
            }

            if (OnLoginAttempt != null)
            {
                OnLoginAttempt(host, port, username, password);
            }
        }

        private void SaveCredentials(string username, string password, string host, string port)
        {
            int userCount = 1;
            while (iniFile.KeyExists($"Username{userCount}", "Users"))
            {
                string existingUser = iniFile.Read($"Username{userCount}", "Users");
                if (existingUser == username) break;
                userCount++;
            }

            iniFile.Write($"Username{userCount}", username, "Users");
            iniFile.Write($"Password{userCount}", password, "Users");
            iniFile.Write("Host", host, "SFTP");
            iniFile.Write("Port", port, "SFTP");
        }
    }
}
