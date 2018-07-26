using System;
using System.ComponentModel;

namespace MergePDF
{
    enum InputFileStatus
    {
        Pending,
        OK,
        Error
    }

    class InputFile : INotifyPropertyChanged
    {
        private InputFileStatus status = InputFileStatus.Pending;
        private Exception cause;
        private int pageCount;

        public string Path { get; }

        public InputFileStatus Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        public Exception Exception
        {
            get { return cause; }
            set
            {
                cause = value;
                OnPropertyChanged("Exception");
            }
        }

        public int PageCount
        {
            get { return pageCount; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Page count must be positive.");
                }

                pageCount = value;
                OnPropertyChanged("PageCount");
            }
        }

        public InputFile(string path) => Path = path;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
