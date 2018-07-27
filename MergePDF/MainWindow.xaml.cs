using Microsoft.Win32;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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

        public MainWindow()
        {
            InitializeComponent();

            inputFiles.CollectionChanged += FileListChanged;
            lstDocuments.ItemsSource = inputFiles;
        }

        private void FileListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            btnMerge.IsEnabled = inputFiles.Count > 1;
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
                foreach (string fname in dlg.FileNames)
                {
                    InputFile file = new InputFile(fname);
                    inputFiles.Add(file);
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
            btnAdd.IsEnabled = enabled;
            btnRemove.IsEnabled = enabled && lstDocuments.SelectedIndex > -1;
            btnMerge.IsEnabled = enabled && inputFiles.Count > 1;
            btnImport.IsEnabled = enabled;
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
                    IProgress<double> progress = new Progress<double>(percent => { pbStatus.Value = percent; });
                    await Task.Run(() => ProcessFiles(dlg.FileName, progress));

                    MessageBox.Show(Properties.Resources.MergeCompletedMessage, Properties.Resources.MergeCompletedTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void ProcessFiles(string outputFile, IProgress<double> progress)
        {
            int processed = 0;
            int total = 0;

            PdfDocument output = new PdfDocument();
            foreach (InputFile input in lstDocuments.Items)
            {
                try
                {
                    using (PdfDocument doc = PdfReader.Open(input.Path, PdfDocumentOpenMode.Import))
                    {
                        input.PageCount = doc.PageCount;

                        for (int i = 0; i < input.PageCount; ++i)
                        {
                            output.AddPage(doc.Pages[i]);
                        }
                    }

                    input.Status = InputFileStatus.OK;
                    ++processed;
                }
                catch (Exception ex)
                {
                    input.Exception = ex;
                    input.Status = InputFileStatus.Error;
                }
                finally
                {
                    ++total;
                    progress.Report(total / (double)inputFiles.Count);
                }
            }

            if (processed > 0)
            {
                output.Save(outputFile);
            }

            output.Dispose();
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
                        MessageBox.Show(string.Format(Properties.Resources.ImportFilesFound, inputFiles.Count), "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(Properties.Resources.NoFilesFound, "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        if (!string.IsNullOrWhiteSpace(line) && File.Exists(line))
                        {
                            Dispatcher.Invoke(() => inputFiles.Add(new InputFile(line)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnRemove.IsEnabled = btnAdd.IsEnabled && lstDocuments.SelectedIndex > -1;
        }
    }
}
