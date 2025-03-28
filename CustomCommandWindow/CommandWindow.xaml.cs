using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace CustomCommandWindow
{
    public partial class CommandWindow : UserControl
    {
        private SshClient sshClient;
        private ShellStream shellStream;
        private List<string> commandHistory = new List<string>();
        private int historyIndex = -1;
        private int promptStartIndex = 0; // Stores where user input starts

        public CommandWindow()
        {
            InitializeComponent();
            ConnectToSSH();
            CommandTextBox.PreviewKeyDown += CommandTextBox_PreviewKeyDown;
            CommandTextBox.TextChanged += CommandTextBox_TextChanged;
        }

        private void ConnectToSSH()
        {
            try
            {
                sshClient = new SshClient("ohpcvn", "thannguyen", "noequalvn");
                sshClient.Connect();

                if (sshClient.IsConnected)
                {
                    shellStream = sshClient.CreateShellStream("cmd", 80, 24, 800, 600, 1024);
                    AppendOutput("Connected to SSH server.\n");

                    Task.Run(() => ReadShellStream());
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"SSH Connection Error: {ex.Message}\n");
            }
        }

        private async Task ReadShellStream()
        {
            byte[] buffer = new byte[4096];
            StringBuilder outputBuilder = new StringBuilder();

            while (sshClient.IsConnected)
            {
                try
                {
                    int bytesRead = await shellStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string output = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Dispatcher.Invoke(() => AppendOutput(output));
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => AppendOutput($"Error reading SSH output: {ex.Message}\n"));
                    break;
                }
            }
        }

        private void CommandTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent moving the cursor before the prompt
            if (CommandTextBox.CaretIndex < promptStartIndex)
            {
                e.Handled = true;
                CommandTextBox.CaretIndex = CommandTextBox.Text.Length; // Force cursor back
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                string command = GetUserInput();

                if (!string.IsNullOrEmpty(command))
                {
                    commandHistory.Add(command);
                    historyIndex = commandHistory.Count;
                    SendCommand(command);
                }
            }
            else if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                // Prevent deleting text before the prompt
                if (CommandTextBox.CaretIndex <= promptStartIndex)
                {
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Up)
            {
                if (historyIndex > 0)
                {
                    historyIndex--;
                    ReplaceUserInput(commandHistory[historyIndex]);
                }
            }
            else if (e.Key == Key.Down)
            {
                if (historyIndex < commandHistory.Count - 1)
                {
                    historyIndex++;
                    ReplaceUserInput(commandHistory[historyIndex]);
                }
                else
                {
                    ReplaceUserInput("");
                }
            }
        }

        private void CommandTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Prevent modification of old text
            if (CommandTextBox.CaretIndex < promptStartIndex)
            {
                CommandTextBox.Text = CommandTextBox.Text.Substring(0, promptStartIndex);
                CommandTextBox.CaretIndex = CommandTextBox.Text.Length;
            }
        }

        private string GetUserInput()
        {
            return CommandTextBox.Text.Substring(promptStartIndex).Trim();
        }

        private void SendCommand(string command)
        {
            if (shellStream != null && sshClient.IsConnected)
            {
                shellStream.WriteLine(command);
                AppendOutput($"> {command}\n");
            }
            else
            {
                AppendOutput("SSH connection lost.\n");
            }
        }

        private void AppendOutput(string output)
        {
            CommandTextBox.AppendText(output);
            promptStartIndex = CommandTextBox.Text.Length; // Update where user input starts
            CommandTextBox.CaretIndex = CommandTextBox.Text.Length;
            CommandTextBox.ScrollToEnd();
        }

        private void ReplaceUserInput(string command)
        {
            // Remove previous user input while keeping the prompt
            CommandTextBox.Text = CommandTextBox.Text.Substring(0, promptStartIndex);
            CommandTextBox.AppendText(command);
            CommandTextBox.CaretIndex = CommandTextBox.Text.Length;
        }
    }
}
