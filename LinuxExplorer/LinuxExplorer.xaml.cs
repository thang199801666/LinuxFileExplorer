using SftpLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace LinuxExplorer
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Explorer : UserControl
    {
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection;
        private Point _startPoint;
        private FileItem selectedItem;

        public ObservableCollection<FileItem> _files;
        private bool _showHiddenFiles = false;
        SftpFileManager _sftpFileManager = new SftpFileManager();
        SSHControl _sshControl = new SSHControl();
        private string _tempFolder;
        private string _windowPath;

        private CancellationTokenSource _cts;

        public ICommand PasteCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand ShowHiddenFilesCommand { get; private set; }

        public Explorer()
        {
            InitializeComponent();
            DataContext = this;
            _files = new ObservableCollection<FileItem>();
            _files.CollectionChanged += FileItems_CollectionChanged;
            _files?.Clear();
            FilesListView.ItemsSource = _files;

            _sftpFileManager.OnErrorOccurred += HandleSftpError;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _tempFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SftpTemp");

            PasteCommand = new RelayCommand(PasteFilesFromClipboard);
            CopyCommand = new RelayCommand(async () => await CopyFilesToClipboardAsync());
            ShowHiddenFilesCommand = new RelayCommand(ExecuteToggleHiddenFiles);

            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            } 
        }

        public bool Login(string host, int port, string userName, string password)
        {
                
            bool sftpConnected = _sftpFileManager.Connect(host, port, userName, password);

            if (sftpConnected)
            {
                _sshControl.Connect(host, port, userName, password);

                _sftpFileManager.CurrentFolderFiles(ref _files, _showHiddenFiles);
                _sftpFileManager.WorkingDirectoryChanged += (sender, newDir) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        RefreshFilesList();
                        btnSFTPDir.Content = newDir;
                        btnSFTPDir.ToolTip = newDir;
                        if (_sshControl != null)
                        {
                            _sshControl.ChangeShellDirectory(newDir);
                        }
                    });
                };
                btnSFTPDir.Content = _sftpFileManager.CurrentDirectory;
                btnSFTPDir.ToolTip = _sftpFileManager.CurrentDirectory;
                return true;
            }
            return false;
        }

        private void FilesListView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyInitialSorting();
        }

        public void SetWindowTargetPath(string path)
        {
            _windowPath = path;
        }

        public void Explorer_Unloaded()
        {
            _sftpFileManager?.Disconnect();
            try
            {
                if (Directory.Exists(_tempFolder))
                {
                    Directory.Delete(_tempFolder, true);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
            }
        }

        private void WriteLog(string message)
        {
            try
            {
                string logFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, "log.log");
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                System.IO.File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // Avoid recursive errors if logging fails
            }
        }

        private void HandleSftpError(string errorMessage)
        {
            Dispatcher.Invoke(() => MessageBox.Show(errorMessage, "SFTP Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void FileItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //// Handle added items
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    foreach (FileItem item in e.NewItems)
            //    {
            //        Console.WriteLine($"Added: {item.Name}");
            //    }
            //}

            //// Handle removed items
            //if (e.Action == NotifyCollectionChangedAction.Remove)
            //{
            //    foreach (FileItem item in e.OldItems)
            //    {
            //        Console.WriteLine($"Removed: {item.Name}");
            //    }
            //}
            lbItemTotal.Content = $"/{_files.Count}";
        }

        private void RefreshFilesList()
        {
            _files?.Clear();
            _sftpFileManager.CurrentFolderFiles(ref _files, _showHiddenFiles);

            if (_lastHeaderClicked != null)
            {
                Sort(GetSortProperty(_lastHeaderClicked.Column), _lastDirection);
            }
        }

        #region Menu Item
        private void FilesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (FilesListView.Items.Count == 0)
            {
                e.Handled = true;
            }

            checkItem.IsEnabled = false;
            runItem.IsEnabled = false;
            copyItem.IsEnabled = false;
            renameItem.IsEnabled = false;
            downloadItem.IsEnabled = false;
            deleteItem.IsEnabled = false;

            selectedItem = (FileItem)FilesListView.SelectedItem;
            if (selectedItem != null)
            {
                copyItem.IsEnabled = true;
                renameItem.IsEnabled = true;
                downloadItem.IsEnabled = true;
                deleteItem.IsEnabled = true;
                if (selectedItem != null && selectedItem.Name.EndsWith(".inp"))
                {
                    checkItem.IsEnabled = true;
                    runItem.IsEnabled = true;
                }
            }
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            selectedItem = (FileItem)FilesListView.SelectedItem;
            if (selectedItem.Name.EndsWith(".inp"))
            { 
                string text = _sftpFileManager.ReadFileFromSftp(selectedItem.FullPath);
                INPReader reader = new INPReader(text);

                INPInformations inpDialog = new INPInformations(reader.Instance);
                inpDialog.ShowDialog();
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList selectedItems = FilesListView.SelectedItems;
            JobMonitor jobMonitor = new JobMonitor(selectedItems);
            jobMonitor.OnCommandSent += ReceiveCommand;
            jobMonitor.ShowDialog();
        }

        private void ReceiveCommand(string command)
        {
            _sshControl.RunJob(command);
        }

        private void ToggleHiddenFiles_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            _showHiddenFiles = menuItem.IsChecked;
            RefreshFilesList();
        }

        private void ExecuteToggleHiddenFiles()
        {
            ShowHiddenFiles = !ShowHiddenFiles;
        }

        private bool ShowHiddenFiles
        {
            get => _showHiddenFiles;
            set
            {
                _showHiddenFiles = value;
                OnPropertyChanged(nameof(ShowHiddenFiles));
                RefreshFilesList();
            }
        }

        private event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void FocusItemByName(string fileName)
        {
            var item = _files.FirstOrDefault(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                FilesListView.SelectedItem = item;
                FilesListView.ScrollIntoView(item);

                ListViewItem lvi = (ListViewItem)FilesListView.ItemContainerGenerator.ContainerFromItem(item);
                if (lvi != null)
                {
                    lvi.Focus();
                }
            }
        }

        private void NewFile_Click(object sender, RoutedEventArgs e)
        {
            string newFile = _sftpFileManager.CreateFile();
            RefreshFilesList();
            FocusItemByName(newFile);

            FileItem selectedFile = (FileItem)FilesListView.SelectedItem;
            selectedFile.IsEditing = true;
            selectedFile.EditableName = selectedFile.Name;
            FilesListView.Items.Refresh();
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            string newFile = _sftpFileManager.CreateDirectory();
            RefreshFilesList();
            FocusItemByName(newFile);

            FileItem selectedFile = (FileItem)FilesListView.SelectedItem;
            selectedFile.IsEditing = true;
            selectedFile.EditableName = selectedFile.Name;
            FilesListView.Items.Refresh();
        }

        private async Task CopyFilesToClipboardAsync()
        {
            // Copy files to clipboard
            //StringCollection fileCollection = new StringCollection();
            //fileCollection.AddRange(localFilePaths.ToArray());
            //Clipboard.SetFileDropList(fileCollection);
        }

        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsFileDropList())
            {
                StringCollection files = Clipboard.GetFileDropList();

                foreach (string file in files)
                {
                    string destinationPath = _sftpFileManager.CurrentDirectory + "/" + System.IO.Path.GetFileName(file);

                    try
                    {
                        using (var fileStream = System.IO.File.OpenRead(file))
                        {
                            _sftpFileManager.UploadFile(fileStream, destinationPath);
                        }
                        WriteLog($"File copied: {destinationPath}");
                    }
                    catch (Exception ex)
                    {
                        WriteLog($"Error copying file: {ex.Message}");
                    }
                }

                RefreshFilesList();
            }
            else
            {
                MessageBox.Show("Clipboard does not contain files.", "Paste Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PasteFilesFromClipboard()
        {
            PasteMenuItem_Click(null, null);
        }

        private void OpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", _tempFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Temp Folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void StartDownload(FileItem item)
        {
            string remotePath = item.FullPath;
            string localPath;
            if (_windowPath != null)
            {
                localPath = $"{_windowPath}\\{item.Name}";
            }
            else 
            {
                localPath = $"{_tempFolder}\\{item.Name}";
            }

            if (item.Type != "Folder")
            {
                try
                {
                    await _sftpFileManager.DownloadFileAsync(remotePath, localPath, (progress, text) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            processBar.ProgressValue = progress;
                            processBar.ProgressText = $"{progress:F2}% {text}";
                        });
                    });
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                    MessageBox.Show("Download Error!");
                }
            }
            else
            {
                var progressIndicator = new Progress<double>(value =>
                {
                    processBar.ProgressValue = value;
                });
                await Task.Run(() => _sftpFileManager.DownloadFolderAsync(remotePath, localPath, progressIndicator));
            }
            processBar.ProgressValue = 0;
        }

        private void DownloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var items = FilesListView.SelectedItems;
            foreach (FileItem item in items)
            {
                StartDownload(item);
            }
        }

        #endregion Menu Item

        #region key event
        private void FilesListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                RefreshFilesList(); 
            }
            if (e.Key == Key.F2 && FilesListView.SelectedItem is FileItem selectedFile)
            {
                selectedFile.IsEditing = true;
                selectedFile.EditableName = selectedFile.Name;
                FilesListView.Items.Refresh();
            }
            if (e.Key == Key.Delete)
            {
                DeleteSelectedFiles();
            }

            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                FilesListView.UnselectAll();
            }

            if (e.Key == Key.F8)
            {
                ViewHiddenFile.IsChecked = !_showHiddenFiles;
                _showHiddenFiles = ViewHiddenFile.IsChecked;
                RefreshFilesList();
            }

            if (e.Key == Key.Enter)
            {
                SelectFileItem();
            }
        }

        #endregion key event

        #region Clicking methods
        // Selection
        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbItemselected.Content = FilesListView.SelectedItems.Count;
        }

        // Dragging

        // Left click with delay
        private void FilesListView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedItem = (FileItem)FilesListView.SelectedItem;
        }

        // Single Click
        private void FilesListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (FilesListView.SelectedItem is FileItem selectedFile)
            //{
            //    if (selectedFile.Type.ToLower() != "directory") // Open only files
            //    {
            //        OpenSftpFile(selectedFile);
            //    }
            //}
        }

        private void OpenSftpFile(FileItem file)
        {
            try
            {
                string tempFilePath = System.IO.Path.Combine(_tempFolder, file.Name);
                using (var stream = System.IO.File.Create(tempFilePath)) // Create local temp file
                {
                    _sftpFileManager.DownloadFile(file.FullPath, stream);
                }

                Process.Start(new ProcessStartInfo(tempFilePath) { UseShellExecute = true }); // Open with default app
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Double click
        private void btnSFTPDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (_sftpFileManager == null) return;
            Label lbl = sender as Label;
            GotoPathDialog dialog = new GotoPathDialog(_sftpFileManager.CurrentDirectory);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string newPath = dialog.EnteredPath;
                if (!string.IsNullOrWhiteSpace(newPath) && _sftpFileManager.FolderExists(newPath))
                {
                    _sftpFileManager.ChangeDirectory(newPath);
                }
                else
                {
                    MessageBox.Show("Invalid or non-existent path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectFileItem();
        }

        #endregion Clicking methods

        #region Drad and drop
        private void FilesListView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void FilesListView_DragEnter(object sender, DragEventArgs e)
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

        private void FilesListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    _sftpFileManager.UploadFileAsync(file);
                }
                RefreshFilesList();
            }
        }

        private void FilesListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView)
            {
                DependencyObject clickedElement = (DependencyObject)e.OriginalSource;

                // Allow dragging from any part of the ListViewItem, including images
                ListViewItem item = FindAncestor<ListViewItem>(clickedElement);
                if (item != null && listView.SelectedItem is FileItem file)
                {
                    _startPoint = e.GetPosition(null);
                }
            }
        }

        private void FilesListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && FilesListView.SelectedItem is FileItem selectedFile)
            {
                //Point currentPosition = e.GetPosition(null);

                //if (Math.Abs(currentPosition.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                //    Math.Abs(currentPosition.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                //{
                //    StartDragFromSFTP(selectedFile);
                //}
            }
        }

        private void StartDragFromSFTP(FileItem file)
        {
            string tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), file.Name);

            Task.Run(async () =>
            {
                // Download the file from SFTP to the temp folder
                await _sftpFileManager.DownloadFileAsync(file.FullPath, tempFilePath, (progress, text) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Update progress bar UI (if applicable)
                        processBar.ProgressValue = progress;
                        processBar.ProgressText = $"{progress:F2}% {text}";
                    });
                });

                // After download, start drag operation
                Dispatcher.Invoke(() =>
                {
                    DataObject data = new DataObject(DataFormats.FileDrop, new string[] { tempFilePath });
                    DragDrop.DoDragDrop(FilesListView, data, DragDropEffects.Copy);
                });
            });
        }


        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T found)
                    return found;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
        #endregion

        #region Refresh
        private void RefreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RefreshFilesList();
        }
        #endregion Refresh

        #region Change Folder
        private void SelectFileItem()
        {
            FileItem selectedFile = (FileItem)FilesListView.SelectedItem;
            if (selectedFile == null || selectedFile.IsEditing == true) return;
            if (selectedFile.Type == "Folder")
            {
                _sftpFileManager.ChangeDirectory(_sftpFileManager.CurrentDirectory + "/" + selectedFile.Name);
            }
            else if (selectedFile.Type != "Folder")
            {
                if (selectedFile.Size > 50000000)
                {
                    var result = MessageBox.Show("Do you want to open the file larger than 50MB?", "Large File Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        OpenSftpFile(selectedFile);
                    }
                }
                else 
                {
                    OpenSftpFile(selectedFile);
                }
            }
        } 

        #endregion Change Folder

        #region Delete
        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            FilesListView.Items.Refresh();
            DeleteSelectedFiles();
        }

        private async void DeleteSelectedFiles()
        {
            var selectedFiles = FilesListView.SelectedItems.Cast<FileItem>().ToList();

            if (selectedFiles.Count == 0) return;

            // Confirm before deleting
            var result = MessageBox.Show($"Are you sure you want to delete {selectedFiles.Count} file(s)?",
                                         "Confirm Delete",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await DeleteFilesFromServerAsync(selectedFiles);
                RefreshFilesList(); // Refresh file list
            }
        }

        private async Task DeleteFilesFromServerAsync(List<FileItem> filesToDelete)
        {
            await Task.Run(() =>
            {
                try
                {
                    foreach (var file in filesToDelete)
                    {
                        if (file.Type == "Folder")
                        {
                            _sftpFileManager.DeleteDirectory(file.FullPath); // Delete directory recursively
                        }
                        else
                        {
                            _sftpFileManager.DeleteFile(file.FullPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error deleting files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });
        }
        #endregion Delete

        #region Rename
        // Press F2 to enter edit mode
        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FilesListView.SelectedItem is FileItem selectedFile)
            {
                selectedFile.IsEditing = true;
                selectedFile.EditableName = selectedFile.Name; // Keep original name
                FilesListView.Items.Refresh();
            }
        }

        private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Focus();

                string filename = System.IO.Path.GetFileNameWithoutExtension(textBox.Text);
                int extensionIndex = filename.Length;

                // Highlight only the filename (excluding extension)
                textBox.Select(0, extensionIndex);
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (FilesListView.SelectedItem is FileItem fileItem)
            //{
            //    fileItem.IsEditing = false;
            //}
            if (sender is TextBox textBox && FilesListView.SelectedItem is FileItem selectedFile)
            {
                string newName = textBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(newName) && newName != selectedFile.Name)
                {
                    if (RenameFileOnServer(selectedFile.Name, newName))
                        selectedFile.Name = newName;
                }
                else if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show($"Error: File name can be NULL.",
                                         "Error",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Error);
                }

                selectedFile.IsEditing = false;
                FilesListView.Items.Refresh();
            }
        }

        private void RenameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox && FilesListView.SelectedItem is FileItem selectedFile)
            {
                if (e.Key == Key.Enter)
                {
                    string newName = textBox.Text.Trim();

                    if (!string.IsNullOrWhiteSpace(newName) && newName != selectedFile.Name)
                    {
                        if (RenameFileOnServer(selectedFile.Name, newName))
                        {
                            selectedFile.Name = newName;
                        }
                    }

                    selectedFile.IsEditing = false;
                }
                else if (e.Key == Key.Escape)
                {
                    selectedFile.IsEditing = false;
                }
            }
        }

        private void StartRenaming(FileItem file)
        {
            file.IsEditing = true;
            FilesListView.Items.Refresh();
        }

        private bool RenameFileOnServer(string oldName, string newName)
        {
            try
            {
                string oldPath = $"{_sftpFileManager.CurrentDirectory}/{oldName}";
                string newPath = $"{_sftpFileManager.CurrentDirectory}/{newName}";
                _sftpFileManager.RenameFile(oldName, newPath);
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show($"Error: Can't Rename this File.",
                                         "Renaming Error",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Error);
                return false;
            }
        }

        #endregion Rename

        #region Sorting
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Column != null)
            {
                string sortBy = GetSortProperty(headerClicked.Column);

                // Toggle sort direction
                ListSortDirection direction = (_lastHeaderClicked == headerClicked && _lastDirection == ListSortDirection.Ascending)
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;

                // Perform sorting
                Sort(sortBy, direction);

                // Update header text with sorting arrow
                if (_lastHeaderClicked != null)
                    _lastHeaderClicked.Column.Header = _lastHeaderClicked.Column.Header.ToString().Replace(" ▲", "").Replace(" ▼", "");

                headerClicked.Column.Header = sortBy + (direction == ListSortDirection.Ascending ? " ▲" : " ▼");

                // Store last clicked header
                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
        }

        private double ConvertSizeToFloat(string size)
        {
            if (string.IsNullOrWhiteSpace(size))
                return 0; // Treat empty size as 0

            if (double.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;

            return 0; // Fallback if parsing fails
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(FilesListView.ItemsSource);
            if (view != null)
            {
                view.SortDescriptions.Clear();

                var sortedList = _files.ToList();

                // Sorting priority:
                // 1. ".." (Parent directory) always on top
                // 2. Folders before files
                // 3. Sorting based on the selected column

                IOrderedEnumerable<FileItem> orderedList = sortedList
                    .OrderBy(f => f.Name != "..")  // ".." always first
                    .ThenBy(f => f.Type != "Folder"); // Folders before files

                switch (sortBy)
                {
                    case "Name":
                        orderedList = direction == ListSortDirection.Ascending
                            ? orderedList.ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                            : orderedList.ThenByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase);
                        break;

                    case "Type":
                        orderedList = direction == ListSortDirection.Ascending
                            ? orderedList.ThenBy(f => f.Type, StringComparer.OrdinalIgnoreCase)
                            : orderedList.ThenByDescending(f => f.Type, StringComparer.OrdinalIgnoreCase);
                        break;

                    case "Size":
                        orderedList = direction == ListSortDirection.Ascending
                            ? orderedList.ThenBy(f => f.Type == "Folder" ? long.MaxValue : f.Size)
                            : orderedList.ThenByDescending(f => f.Type == "Folder" ? long.MaxValue : f.Size);
                        break;

                    case "Modified":
                        orderedList = direction == ListSortDirection.Ascending
                            ? orderedList.ThenBy(f => f.Modified)
                            : orderedList.ThenByDescending(f => f.Modified);
                        break;

                    case "Permissions":
                        orderedList = direction == ListSortDirection.Ascending
                            ? orderedList.ThenBy(f => f.Permissions, StringComparer.OrdinalIgnoreCase)
                            : orderedList.ThenByDescending(f => f.Permissions, StringComparer.OrdinalIgnoreCase);
                        break;
                }

                // Clear & refill the ObservableCollection
                _files.Clear();
                foreach (var item in orderedList)
                {
                    _files.Add(item);
                }

                view.Refresh();
            }
        }

        private GridViewColumnHeader GetFirstColumnHeader(ListView listView)
        {
            if (listView == null) return null;

            return FindVisualChildren<GridViewColumnHeader>(listView).FirstOrDefault();
        }

        // Helper method to find visual children
        private List<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            List<T> children = new List<T>();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    children.Add(tChild);

                children.AddRange(FindVisualChildren<T>(child));
            }

            return children;
        }

        private string GetSortProperty(GridViewColumn column)
        {
            if (column.Header.ToString().Contains("Name")) return nameof(FileItem.Name);
            if (column.Header.ToString().Contains("Type")) return nameof(FileItem.Type);
            if (column.Header.ToString().Contains("Size")) return nameof(FileItem.Size);
            if (column.Header.ToString().Contains("Modified")) return nameof(FileItem.Modified);
            if (column.Header.ToString().Contains("Permissions")) return nameof(FileItem.Permissions);
            return nameof(FileItem.Name); // Default sorting
        }

        // Sort name Column
        private async void ApplyInitialSorting()
        {
            await Task.Delay(100); // Ensure ListView is loaded

            var nameColumnHeader = FindColumnHeader("Name");
            if (nameColumnHeader != null)
            {
                _lastHeaderClicked = nameColumnHeader;
                _lastDirection = ListSortDirection.Ascending;

                nameColumnHeader.Column.Header = "Name ▲";

                Dispatcher.Invoke(() => Sort(nameof(FileItem.Name), ListSortDirection.Ascending));
            }
        }

        private GridViewColumnHeader FindColumnHeader(string columnName)
        {
            return FindVisualChildren<GridViewColumnHeader>(FilesListView)
                .FirstOrDefault(header => header.Column != null && header.Column.Header.ToString().Contains(columnName));
        }

        #endregion Sorting 

        #region Navigate button

        private void btnGotoParent_Click(object sender, RoutedEventArgs e)
        {
            if (_sftpFileManager == null) return;
            string newPath = _sftpFileManager.CurrentDirectory + "/..";
            _sftpFileManager.ChangeDirectory(newPath);
        }

        private void btnGotoPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_sftpFileManager == null && _sftpFileManager.PreviousDir == null) return;
            _sftpFileManager.ChangeDirectory(_sftpFileManager.PreviousDir);
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            if (_sftpFileManager == null) return;
            _sftpFileManager.ChangeDirectory($"/home/{_sftpFileManager.UserName}");
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshFilesList();
        }

        #endregion Navigate button 
    }
}
