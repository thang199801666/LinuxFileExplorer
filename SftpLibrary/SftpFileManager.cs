using Renci.SshNet.Sftp;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static System.Net.WebRequestMethods;
using System.Net.Mail;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Text;
using System.Threading;

namespace SftpLibrary
{
    public class SftpFileManager
    {
        private SftpClient _sftpClient;
        public event Action<string> OnErrorOccurred;
        private WeakReference<List<FileItem>> _fileCache = new WeakReference<List<FileItem>>(null);
        private string _currentDirectory;
        public event EventHandler<string> WorkingDirectoryChanged;
        private string _previousDir;

        public string CurrentDirectory
        {
            get => _sftpClient.WorkingDirectory;
        }

        public bool IsConnected { get => _sftpClient.IsConnected; }
        public string PreviousDir { get => _previousDir; set => _previousDir = value; }
        public string Host { get => _sftpClient.ConnectionInfo.Host; }
        public string UserName { get => _sftpClient.ConnectionInfo.Username;}

        public SftpFileManager()
        {
            
        }

        public bool Connect(string host,int port, string username, string password)
        {
            try
            {
                _sftpClient = new SftpClient(host, port, username, password);
                _sftpClient.Connect();

                if (_sftpClient.IsConnected)
                {
                    _currentDirectory = _sftpClient.WorkingDirectory;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Disconnect()
        {
            if (_sftpClient != null)
            {
                if (_sftpClient.IsConnected)
                {
                    _sftpClient.Disconnect();
                    _sftpClient?.Dispose();
                }
            }
        }

        private void RaiseError(string message)
        {
            OnErrorOccurred?.Invoke(message);
        }

        public void CurrentFolderFiles(ref ObservableCollection<FileItem> dataObject, bool showHiddenFiles = false)
        {
            if (dataObject == null || _sftpClient == null) return;
            try
            {
                IEnumerable<ISftpFile> files = _sftpClient.ListDirectory(_sftpClient.WorkingDirectory);
                foreach (var file in files)
                {
                    if (file.Name == "..")
                    {
                        dataObject.Add(new FileItem
                        {
                            Icon = IconManager.FindIconForFolder(false),
                            Name = file.Name,
                            FullPath = file.FullName,
                            Type = "Folder",
                            Size = file.IsDirectory ? 0 : file.Length,
                            Modified = file.LastWriteTime,
                            Permissions = GetPermissions(file)
                        });
                    }
                    else if (file.Name.StartsWith(".") && !showHiddenFiles)
                    {
                        continue;
                    }
                    else
                    {
                        dataObject.Add(new FileItem
                        {
                            Icon = file.IsDirectory ? IconManager.FindIconForFolder(false) : IconManager.FindIconForFilename(file.Name, false),
                            Name = file.Name,
                            FullPath = file.FullName,
                            Type = file.IsDirectory ? "Folder" : GetFileTypeFromExtension(file.FullName),//"file",
                            Size = file.IsDirectory ? 0 : file.Length,
                            Modified = file.LastWriteTime,
                            Permissions = GetPermissions(file)
                        });
                    }
                }
                files = null;
            }
            catch (System.Exception ex)
            {
                RaiseError($"Failed to list directory: {ex.Message}");
            }
        }

        public List<FileItem> GetFiles(string directory, bool showHiddenFiles = false)
        {
            List<FileItem> Items = new List<FileItem>();
            try
            {
                IEnumerable<ISftpFile> files = _sftpClient.ListDirectory(directory);
                foreach (var file in files)
                {
                    if (file.Name == "..")
                    {
                        Items.Add(new FileItem
                        {
                            Icon = IconManager.FindIconForFolder(false),
                            Name = file.Name,
                            FullPath = file.FullName,
                            Type = "Folder",
                            Size = file.IsDirectory ? 0 : file.Length,
                            Modified = file.LastWriteTime,
                            Permissions = GetPermissions(file)
                        });
                    }
                    else if (file.Name.StartsWith(".") && !showHiddenFiles)
                    {
                        continue;
                    }
                    else
                    {
                        Items.Add(new FileItem
                        {
                            Icon = file.IsDirectory ? IconManager.FindIconForFolder(false) : IconManager.FindIconForFilename(file.Name, false),
                            Name = file.Name,
                            FullPath = file.FullName,
                            Type = file.IsDirectory ? "Folder" : GetFileTypeFromExtension(file.FullName),//"file",
                            Size = file.IsDirectory ? 0 : file.Length,
                            Modified = file.LastWriteTime,
                            Permissions = GetPermissions(file)
                        });
                    }
                }
                files = null; 
            }
            catch (System.Exception ex)
            {
                RaiseError($"Failed to list directory: {ex.Message}");
            }
            return Items;
        }


        // Convert System.Drawing.Icon to System.Windows.Media.ImageSource
        private ImageSource ConvertIconToImageSource(System.Drawing.Icon icon)
        {
            if (icon == null) return null;

            using (var stream = new MemoryStream())
            {
                icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);  // Convert to PNG
                stream.Seek(0, SeekOrigin.Begin);

                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze(); // Ensure it's usable in WPF

                return bitmap;
            }
        }

        public bool FileExists(string remotePath)
        {
            try
            {
                var files = _sftpClient.ListDirectory(CurrentDirectory);
                return files.Any(f => f.Name == Path.GetFileName(remotePath) && !f.IsDirectory);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool FolderExists(string remotePath)
        {
            try
            {
                return _sftpClient.Exists(remotePath) && _sftpClient.GetAttributes(remotePath).IsDirectory;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string CreateFile()
        {
            string newFileName = "New File.txt";
            string newFilePath = $"{_sftpClient.WorkingDirectory}/{newFileName}";
            int count = 1;
            while (FileExists(newFilePath))
            {
                newFileName = $"New File({count}).txt";
                newFilePath = $"{_sftpClient.WorkingDirectory}/{newFileName}";
                count++;
            }
            _sftpClient.Create(newFilePath);
            return newFileName;
        }

        public string CreateDirectory()
        {
            string newFileName = "New Folder";
            string newFilePath = $"{_sftpClient.WorkingDirectory}/{newFileName}";
            int count = 1;
            while (FolderExists(newFilePath))
            {
                newFileName = $"New Folder({count})";
                newFilePath = $"{_sftpClient.WorkingDirectory}/{newFileName}";
                count++;
            }
            _sftpClient.CreateDirectory(newFilePath);
            return newFileName;
        }

        public void ChangeDirectory(string newPath)
        {
            try
            {
                PreviousDir = _sftpClient.WorkingDirectory;
                _sftpClient.ChangeDirectory(newPath);

                if (_sftpClient.WorkingDirectory != PreviousDir)
                {
                    _currentDirectory = _sftpClient.WorkingDirectory;
                    WorkingDirectoryChanged?.Invoke(this, _currentDirectory);
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to change directory: {ex.Message}");
            }
        }

        public void RenameFile(string oldPath, string newPath)
        {
            if (_sftpClient.IsConnected)
            {
                _sftpClient.RenameFile(oldPath, newPath);
            }
        }

        public void DeleteFile(string filePath)
        {
            if (_sftpClient.IsConnected)
            {
                _sftpClient.DeleteFile(filePath);
            }  
        }

        public void DeleteDirectory(string dirPath)
        {
            if (!_sftpClient.IsConnected) throw new InvalidOperationException("SFTP client is not connected.");
            string lastFolder = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (lastFolder == ".." || lastFolder == ".") return;
            _sftpClient.DeleteDirectory(dirPath);
        }

        public void DownloadFolder(string remotePath, string localPath)
        {
            try
            {
                if (!_sftpClient.IsConnected)
                {
                    throw new Exception("SFTP client is not connected.");
                }

                if (!Directory.Exists(localPath))
                {
                    Directory.CreateDirectory(localPath);
                }

                var files = _sftpClient.ListDirectory(remotePath);

                foreach (var file in files)
                {
                    string localFilePath = Path.Combine(localPath, file.Name);
                    string remoteFilePath = file.FullName;

                    if (file.IsDirectory)
                    {
                        if (file.Name != "." && file.Name != "..") // Ignore parent and current directory links
                        {
                            DownloadFolder(remoteFilePath, localFilePath); // Recursively download subfolders
                        }
                    }
                    else
                    {
                        using (var fileStream = System.IO.File.Create(localFilePath))
                        {
                            _sftpClient.DownloadFile(remoteFilePath, fileStream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading folder: {ex.Message}");
            }
        }

        public void DownloadFile(string remotePath, Stream localStream)
        {
            if (!_sftpClient.IsConnected) return;
            if (!_sftpClient.Exists(remotePath)) return;
            _sftpClient.DownloadFile(remotePath, localStream);
        }

        public string ReadFileFromSftp(string remoteFilePath)
        {
            try
            {
                using (Stream fileStream = _sftpClient.OpenRead(remoteFilePath))
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task UploadFileAsync(string localFilePath, string remoteFilePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    using (var fileStream = System.IO.File.OpenRead(localFilePath))
                    {
                        _sftpClient.UploadFile(fileStream, remoteFilePath);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UploadFileAsync(string localFilePath)
        {
            try
            {
                string remotePath = $"{_sftpClient.WorkingDirectory}/{System.IO.Path.GetFileName(localFilePath)}";
                using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                {
                    _sftpClient.UploadFile(fileStream, remotePath);
                }
            }
            catch (Exception ex)
            {
                // Log error instead of showing MessageBox
                System.IO.File.AppendAllText("log.log", $"Failed to upload {localFilePath}: {ex.Message}\n");
            }
        }

        public async Task DownloadFileAsync(string remotePath, string localPath, Action<double, string> progressCallback)
        {
            try
            {
                using (var sftpStream = _sftpClient.OpenRead(remotePath))
                using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 524288, true)) // 512 KB buffer
                {
                    long totalBytes = sftpStream.Length;
                    byte[] buffer = new byte[524288]; // 512 KB buffer
                    int bytesRead;
                    long totalRead = 0;

                    while ((bytesRead = await sftpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        string name = System.IO.Path.GetFileName(remotePath);
                        double progress = (double)totalRead / totalBytes * 100;
                        progressCallback?.Invoke(progress, $"Download : {name}");
                    }
                }
                progressCallback?.Invoke(0,"");
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to Download File: {ex.Message}");
            }
        }

        public async Task DownloadFolderAsync(string remotePath, string localPath, IProgress<double> progress = null)
        {
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            var files = _sftpClient.ListDirectory(remotePath).Where(f => f.Name != "." && f.Name != "..").ToList();
            double totalSize = files.Sum(f => f.IsRegularFile ? f.Length : 0);
            double downloadedSize = 0;

            foreach (var file in files)
            {
                string localFilePath = Path.Combine(localPath, file.Name);
                string remoteFilePath = file.FullName;

                try
                {
                    if (file.IsDirectory)
                    {
                        await DownloadFolderAsync(remoteFilePath, localFilePath, progress);
                    }
                    else
                    {
                        using (var fileStream = System.IO.File.Create(localFilePath))
                        {
                            _sftpClient.DownloadFile(remoteFilePath, fileStream);
                        }
                        downloadedSize += file.Length;
                        progress?.Report((downloadedSize / totalSize) * 100);
                    }
                }
                catch (Exception ex)
                {
                    RaiseError($"File: {file.Name} - {ex.Message}");
                }
            }
        }

        public void UploadFile(FileStream stream, string remotePath)
        {
            if (_sftpClient != null)
            {
                _sftpClient.UploadFile(stream, remotePath);
            }
        }

        private static string GetPermissions(ISftpFile file)
        {
            char[] result = new char[10];
            result[0] = file.IsDirectory ? 'd' : '-';

            result[1] = file.OwnerCanRead ? 'r' : '-';
            result[2] = file.OwnerCanWrite ? 'w' : '-';
            result[3] = file.OwnerCanExecute ? 'x' : '-';

            result[4] = file.GroupCanRead ? 'r' : '-';
            result[5] = file.GroupCanWrite ? 'w' : '-';
            result[6] = file.GroupCanExecute ? 'x' : '-';

            result[7] = file.OthersCanRead ? 'r' : '-';
            result[8] = file.OthersCanWrite ? 'w' : '-';
            result[9] = file.OthersCanExecute ? 'x' : '-';

            return new string(result);
        }

        static string GetFileTypeFromExtension(string fileName)
        {
            var extensionToType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".txt", "Text File" },
                { ".jpg", "JPEG Image" },
                { ".png", "PNG Image" },
                { ".pdf", "PDF Document" },
                { ".docx", "Word Document" },
                { ".xlsx", "Excel Spreadsheet" },
                { ".mp3", "MP3 Audio" },
                { ".mp4", "MP4 Video" },
                { ".exception", "EXCEPTION File"},
                { ".odb", "ODB File"},
                { ".sta", "Status File"},
                { ".sat", "Sat File"},
                { ".dat", "Data File"},
                { ".msg", "Message File"},
                { ".inp", "Input File"},
                { ".log", "Log File"},
                { ".py", "Python File"},
                { ".sh", "Shell Script File"},
                { ".bashrc", "BASHRC File"},
                { ".prt", "PRT File"},
                { ".pac", "PAC File"},
                { ".mdl", "MDL File"},
                { ".env", "ENV File"},
                { ".com", "MS-DOS Application"},
                { ".stt", "STT File"},
                { ".qlog", "QLOG File"},
                { ".sel", "SEL File"},
                { ".res", "RES File"},
                { ".src", "SRC File"},
                { ".abq", "ABQ File"},
                { ".que", "QUE File"}
            };

            string ext = System.IO.Path.GetExtension(fileName);
            return extensionToType.TryGetValue(ext, out string fileType) ? fileType : "File";
        }
    }

    public class SSHControl
    {
        SshClient _sshClient = null;
        ShellStream _shellStream = null;
        private DispatcherTimer idleTimer;
        private readonly TimeSpan idleTimeout = TimeSpan.FromSeconds(30);

        public bool IsConnected { get => _sshClient.IsConnected; }
        public ShellStream ShellStream { get => _shellStream; }

        public SSHControl()
        {

        }

        public void Disconnect()
        {
            try
            {
                if (_shellStream != null)
                {
                    _shellStream.Close();
                    _shellStream.Dispose();
                    _shellStream = null;
                }

                if (_sshClient != null)
                {
                    if (_sshClient.IsConnected)
                    {
                        _sshClient.Disconnect();
                    }
                    _sshClient.Dispose();
                    _sshClient = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting SSH: {ex.Message}");
            }
        }

        public void Connect(string host, int port, string username, string password)
        {
            _sshClient = new SshClient(host, username, password);
            _sshClient.Connect();
            if (_sshClient.IsConnected)
            {
                _shellStream = _sshClient.CreateShellStream("JobStatusShell", 80, 60, 800, 600, 65536);
            }
        }

        public string SendCommand(string command)
        {
            SshCommand inputCommand = _sshClient.CreateCommand(command);
            inputCommand.Execute();
            return inputCommand.Result;
        }

        public void ChangeShellDirectory(string path)
        {
            _shellStream.WriteLine($"cd {path}");
            _shellStream.Flush();
        }

        public void RunJob(string command)
        {
            try
            {
                if (_sshClient != null && _sshClient.IsConnected)
                {
                    string jobCommand = command + $" scratch=/home/{_sshClient.ConnectionInfo.Username}/Temp";
                    _shellStream.Write(jobCommand + "\n");
                    _shellStream.Flush();
                    string response;
                    do
                    {
                        response = ReadShellOutput(_shellStream);
                        Thread.Sleep(1000);
                    }
                    while (!string.IsNullOrWhiteSpace(response));
                }
                else
                {
                    Console.WriteLine("SSH client is not connected.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running job: {ex.Message}");
            }
        }

        private string ReadShellOutput(ShellStream shell)
        {
            StringBuilder output = new StringBuilder();
            string line;

            while ((line = shell.ReadLine(TimeSpan.FromSeconds(0.5))) != null)
            {
                output.AppendLine(line);
            }
            return output.ToString();
        }

        public string ReadStream()
        {
            string output = ShellStream.Read();
            return output;
        }

        public ObservableCollection<JobItem> GetStatus()
        {
            ObservableCollection<JobItem> items = new ObservableCollection<JobItem>();
            if (_sshClient == null) return items;
            string outputText = SendCommand("qstat -a");
            string _outputFullText = SendCommand("qstat");
            string[] myString = outputText.Split('\n');
            Regex jobRex = new Regex("\\d+");

            foreach (var item in myString)
            {
                Match match = jobRex.Match(item);
                if (match.Success)
                {
                    //JobItem job = new JobItem();
                    //string[] jobItems = Regex.Split(item, @"\s+");
                    //Match regFull = Regex.Match(_outputFullText, $"{match.Value}.{_sshClient.ConnectionInfo.Host}\\s+(job_.*?)\\s+(.*?)\\s+");
                    //job.JobID = match.Value;
                    //job.JobName = regFull.Groups[1].Value.Replace("job_", "");
                    //job.UserName = regFull.Groups[2].Value;
                    //job.Token = jobItems[6];
                    //job.Status = jobItems[9];
                    //job.TimeElapse = jobItems[10];
                    //jobItems = null;
                    //items.Add(job);
                    Match regFull = Regex.Match(_outputFullText, $"{match.Value}.{_sshClient.ConnectionInfo.Host}\\s+(job_.*?)\\s+(.*?)\\s+");
                    string[] jobItems = Regex.Split(item, @"\s+");
                    items.Add(new JobItem
                    {
                        JobID = match.Value,
                        JobName = regFull.Groups[1].Value.Replace("job_", ""),
                        UserName = regFull.Groups[2].Value,
                        Token = jobItems[6],
                        Status = jobItems[9],
                        TimeElapse = jobItems[10]
                    });
                    jobItems = null;
                }
            }
            myString = null;
            return items;
        }

        public bool CheckPathExists(string path)
        {
            string command = $"[ -e \"{path}\" ] && echo \"exists\" || echo \"not found\"";
            string result = _sshClient.RunCommand(command).Result.Trim();
            return result == "exists";
        }

        public void ReadStatus(JobItem job)
        {
            string pattern = $"find /home/{_sshClient.ConnectionInfo.Username} -regextype posix-extended -regex '/home/{_sshClient.ConnectionInfo.Username}/.*{job.JobID}.ohpcvn.simpsonmfg.com.*{job.JobName}.sta'";
            string filepath = SendCommand(pattern);
            if (filepath == "")
            {
                MessageBox.Show($"Could not find job: '{job.JobName}' status.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var fileContent = _sshClient.RunCommand($"cat {filepath}").Result;
            string stepPattern = @"Output Field Frame Number\s+(?<currentNum>\d+),\s+of\s+(?<totalNum>\d+),";
            MatchCollection matches = Regex.Matches(fileContent, stepPattern);
            if (matches.Count > 0)
            {
                Match lastMatch = matches[matches.Count - 1];
                string currentFrame = lastMatch.Groups["currentNum"].Value;
                string totalFrames = lastMatch.Groups["totalNum"].Value;

                MessageBox.Show($"Process {currentFrame}/{totalFrames}.", "Job Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
