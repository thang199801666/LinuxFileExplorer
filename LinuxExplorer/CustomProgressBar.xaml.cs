using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace LinuxExplorer
{
    public partial class CustomProgressBar : UserControl, INotifyPropertyChanged
    {
        private double _progressValue;
        public double ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                ProgressText = $"{value:F2}%"; // Update Text
                OnPropertyChanged(nameof(ProgressValue));
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        private string _progressText = "0%";
        public string ProgressText
        {
            get { return _progressText; }
            set
            {
                _progressText = value;
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public CustomProgressBar()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
