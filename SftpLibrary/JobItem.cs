using System.ComponentModel;
using System.Runtime.CompilerServices;

public class JobItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string _jobID;
    private string _userName;
    private string _jobName;
    private string _token;
    private string _status;
    private string _timeElapse;

    public string JobID
    {
        get => _jobID;
        set { _jobID = value; OnPropertyChanged(); }
    }

    public string UserName
    {
        get => _userName;
        set { _userName = value; OnPropertyChanged(); }
    }

    public string JobName
    {
        get => _jobName;
        set { _jobName = value; OnPropertyChanged(); }
    }

    public string Token
    {
        get => _token;
        set { _token = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string TimeElapse
    {
        get => _timeElapse;
        set { _timeElapse = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void UpdateFrom(JobItem other)
    {
        JobID = other.JobID;
        UserName = other.UserName;
        JobName = other.JobName;
        Token = other.Token;
        Status = other.Status;
        TimeElapse = other.TimeElapse;
    }

    public override bool Equals(object obj)
    {
        return obj is JobItem item &&
               JobID == item.JobID &&
               UserName == item.UserName &&
               JobName == item.JobName &&
               Token == item.Token &&
               Status == item.Status &&
               TimeElapse == item.TimeElapse;
    }

    public override int GetHashCode()
    {
        return (JobID?.GetHashCode() ?? 0) ^
               (UserName?.GetHashCode() ?? 0) ^
               (JobName?.GetHashCode() ?? 0) ^
               (Token?.GetHashCode() ?? 0) ^
               (Status?.GetHashCode() ?? 0) ^
               (TimeElapse?.GetHashCode() ?? 0);
    }
}
