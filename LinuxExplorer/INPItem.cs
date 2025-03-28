using LinuxExplorer;
using System;
using System.ComponentModel;
using System.Windows.Input;

public class INPItem : INotifyPropertyChanged
{
    private string _abaqusVersion;
    private int _numCpus;
    private string _precision;
    public string Name { get; set; }

    public string AbaqusVersion
    {
        get => _abaqusVersion;
        set
        {
            if (_abaqusVersion != value)
            {
                _abaqusVersion = value;
                OnPropertyChanged(nameof(AbaqusVersion));
            }
        }
    }

    public int NumCpus
    {
        get => _numCpus;
        set
        {
            if (_numCpus != value)
            {
                _numCpus = value;
                OnPropertyChanged(nameof(NumCpus));
            }
        }
    }

    public string Precision
    {
        get => _precision;
        set
        {
            if (_precision != value)
            {
                _precision = value;
                OnPropertyChanged(nameof(Precision));
            }
        }
    }

    public ICommand RunCommand { get; }
    private readonly Action<string> _sendCommandToJobMonitor;

    public INPItem(Action<string> sendCommandToJobMonitor)
    {
        _sendCommandToJobMonitor = sendCommandToJobMonitor;
        RunCommand = new RelayCommand(ExecuteRunCommand);
    }

    private void ExecuteRunCommand()
    {
        string command = $"{AbaqusVersion} -j {Name} -cpus {NumCpus} {Precision}";
        _sendCommandToJobMonitor?.Invoke(command);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
