using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MergePDF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<InputFile> inputFiles = new ObservableCollection<InputFile>();
        private readonly PdfMerger merger = new PdfMerger();

        public MainWindow()
        {
            InitializeComponent();

            inputFiles.CollectionChanged += FileListChanged;
            lstDocuments.ItemsSource = inputFiles;
        }

        private void FileListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            mniMerge.IsEnabled = inputFiles.Count > 1;

            txtFileCount.Text = string.Format(Properties.Resources.FileListCount, inputFiles.Count);
        }

        private void AddDocuments(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = Properties.Resources.AddDialogTitle,
                DefaultExt = ".pdf",
                Filter = Properties.Resources.AddDialogFilter,
                Multiselect = true,
                RestoreDirectory = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (string name in dlg.FileNames)
                {
                    inputFiles.Add(new InputFile(name));
                }
            }
        }

        private void RemoveDocuments(object sender, RoutedEventArgs e)
        {
            while (lstDocuments.SelectedIndex > -1)
            {
                inputFiles.RemoveAt(lstDocuments.SelectedIndex);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            mniMerge.IsEnabled = enabled;
            mniRemove.IsEnabled = enabled && lstDocuments.SelectedIndex > -1;
            mniMerge.IsEnabled = enabled && inputFiles.Count > 1;
            mniImport.IsEnabled = enabled;
        }

        private async void PerformMerge(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Title = Properties.Resources.MergeDialogTitle,
                    DefaultExt = ".pdf",
                    Filter = Properties.Resources.MergeDialogFilter,
                    FileName = "Output.pdf",
                    RestoreDirectory = true
                };

                if (dlg.ShowDialog() == true)
                {
                    SetButtonsEnabled(false);

                    IProgress<int> progress = new Progress<int>(MergeProgressUpdate);
                    await merger.MergeAsync(inputFiles, dlg.FileName, progress);

                    MessageBox.Show(this, Properties.Resources.MergeCompletedMessage, Properties.Resources.MergeCompletedTitle,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void MergeProgressUpdate(int processed)
        {
            pbStatus.Value = processed / (double)inputFiles.Count;
            txtFileCount.Text = string.Format(Properties.Resources.MergeProgress, processed, inputFiles.Count);
        }

        private void ListBoxDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (lstDocuments.SelectedItem is InputFile input && input.Status == InputFileStatus.Error)
            {
                MessageBox.Show(input.Exception.Message, Properties.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ImportFileList(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = Properties.Resources.ImportDialogTitle,
                    DefaultExt = ".csv",
                    Filter = Properties.Resources.ImportDialogFilter,
                    RestoreDirectory = true
                };

                if (dlg.ShowDialog() == true)
                {
                    SetButtonsEnabled(false);

                    inputFiles.Clear();

                    pbStatus.IsIndeterminate = true;
                    await Task.Run(() => LoadFileNamesFromFile(dlg.FileName));
                    pbStatus.IsIndeterminate = false;

                    if (inputFiles.Count > 0)
                    {
                        MessageBox.Show(this, string.Format(Properties.Resources.ImportFilesFound, inputFiles.Count),
                            "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(this, Properties.Resources.NoFilesFound, "Import",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void LoadFileNamesFromFile(string filename)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string ext = Path.GetExtension(line);
                        if (string.Equals(".pdf", ext, StringComparison.OrdinalIgnoreCase))
                        {
                            Dispatcher.Invoke(() => inputFiles.Add(new InputFile(line)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Properties.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mniRemove.IsEnabled = mniAdd.IsEnabled && lstDocuments.SelectedIndex > -1;
        }

        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
