using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SftpLibrary
{
    public class FileItem : INotifyPropertyChanged, IDisposable
    {
        private bool _isEditing;
        private string _name;
        private string _fileType;
        private long _size;
        private string _editableName;
        private ImageSource _icon;
        private bool _disposed = false;
        public string FullPath { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalization
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    (_icon as IDisposable)?.Dispose();
                    _icon = null;
                }

                _disposed = true;
            }
        }

        ~FileItem()
        {
            Dispose(false);
        }

        public ImageSource Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    (_icon as IDisposable)?.Dispose();
                    _icon = value;
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public long Size
        {
            get => Type == "Folder" ? 0 : _size;
            set => _size = value;
        }

        public string Type
        {
            get => _fileType;
            set => _fileType = value;
        }

        public DateTime Modified { get; set; }
        public string Permissions { get; set; }


        public string EditableName
        {
            get => _editableName;
            set
            {
                if (_editableName != value)
                {
                    _editableName = value;
                    OnPropertyChanged(nameof(EditableName));
                }
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
