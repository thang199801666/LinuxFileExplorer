using SftpLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace LinuxExplorer
{
    /// <summary>
    /// Interaction logic for JobMonitor.xaml
    /// </summary>
    public partial class JobMonitor : Window
    {
        public ObservableCollection<INPItem> INPItems { get; set; } = new ObservableCollection<INPItem>();
        public ObservableCollection<string> AbaqusVersions { get; set; } = new ObservableCollection<string> { "abq2023", "abq2025" };
        public ObservableCollection<int> NumCpuOptions { get; set; } = new ObservableCollection<int> { 32, 64, 128, 160 };
        public ObservableCollection<string> PrecisionOptions { get; set; } = new ObservableCollection<string> { "Single", "Double" };

        public event Action<string> OnCommandSent;
        public JobMonitor(System.Collections.IList items)
        {
            InitializeComponent();

            DataContext = this;

            if (items != null && items.Count > 0)
            {
                foreach (FileItem item in items)
                {
                    INPItems.Add(new INPItem(SendCommandToLinuxExplorer)
                    {
                        Name = item.Name,
                        AbaqusVersion = "abq2023",
                        NumCpus = 160,
                        Precision = "Double"
                    });
                }
                dgJobMonitor.ItemsSource = INPItems;
            }
        }

        public void SendCommandToLinuxExplorer(string command)
        {
            OnCommandSent?.Invoke(command);
        }
    }
}
