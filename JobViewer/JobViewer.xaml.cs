using Renci.SshNet.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using System.Windows.Threading;
using SftpLibrary;

namespace JobViewer
{
    /// <summary>
    /// Interaction logic for JobViewer.xaml
    /// </summary>
    public partial class JobViewer : UserControl
    {
        public ObservableCollection<JobItem> JobItems { get; private set; } = new ObservableCollection<JobItem>();
        private SSHControl _sshControl = new SSHControl();
        public SSHControl SshControl { get => _sshControl;}
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public JobViewer()
        {
            InitializeComponent();
            //System.Threading.ThreadStart threadStart = new System.Threading.ThreadStart(ReceiveSSHData);
            //System.Threading.Thread thread = new System.Threading.Thread(threadStart);
            //thread.IsBackground = true;
            //thread.Start();

            dgStatus.ItemsSource = JobItems;
            _sshControl.Connect("ohpcvn", 22, "thannguyen", "noequalvn");

            Task.Run(() => ReceiveSSHData(_cts.Token));
        }

        private async Task ReceiveSSHData(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var newJobItems = _sshControl.GetStatus();
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        UpdateJobItems(newJobItems);
                    }));
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("SSH data receiving task was canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving SSH data: {ex}");
                }
                await Task.Delay(5000, token);
            }
        }

        private void UpdateJobItems(ObservableCollection<JobItem> newJobItems)
        {
            int i = 0;
            for (; i < newJobItems.Count; i++)
            {
                if (i < JobItems.Count)
                {
                    if (!JobItems[i].Equals(newJobItems[i]))
                    {
                        JobItems[i].UpdateFrom(newJobItems[i]);
                    }
                }
                else
                {
                    JobItems.Add(newJobItems[i]);
                }
            }
            while (JobItems.Count > newJobItems.Count)
            {
                JobItems.RemoveAt(JobItems.Count - 1);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            SshControl?.Disconnect();
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (dgStatus.SelectedItem is JobItem selectedJob)
            {
                string jobDetails = _sshControl.SendCommand($"qstat -f {selectedJob.JobID}");
                string detailsRex = @"(?<Key>Job Id|Job_Name|Job_Owner|resources_used.cput|Resource_List.abqlicense|resources_used.ncpus|Output_Path|jobdir|resources_used.walltime|ctime|etime|Submit_arguments|Submit_Host)[=:\s]+(?<Value>.+)";
                string output = "";
                foreach (Match match in Regex.Matches(jobDetails, detailsRex))
                {
                    output += $"{match.Groups["Key"].Value} : {match.Groups["Value"].Value}\n";
                }
                MessageBox.Show(output, "Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckJob_Click(object sender, RoutedEventArgs e)
        {
            if (dgStatus.SelectedItem is JobItem selectedJob)
            {
                _sshControl.ReadStatus(selectedJob);
            }
        }

        private void CancelJob_Click(object sender, RoutedEventArgs e)
        {
            if (dgStatus.SelectedItem is JobItem selectedJob)
            {
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to cancel job {selectedJob.JobID}?",
                                                          "Cancel Job", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    string cancelCommand = $"qdel {selectedJob.JobID}";
                    _sshControl.SendCommand(cancelCommand);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshJobList();
        }

        private void RefreshJobList()
        {
            var newJobs = _sshControl.GetStatus();
            JobItems.Clear();
            foreach (var job in newJobs)
            {
                JobItems.Add(job);
            }
        }

        private void dgStatus_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                dgStatus.UnselectAll();
                dgStatus.Focus();
                Keyboard.ClearFocus();
            }
        }

        public void UnselectAll()
        {
            dgStatus.UnselectAll();
        }
    }
}
