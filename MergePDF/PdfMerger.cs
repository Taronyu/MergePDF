using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MergePDF
{
    class PdfMerger
    {
        public Task MergeAsync(IEnumerable<InputFile> inputFiles, string outputFile, IProgress<int> progress)
        {
            return Task.Run(() => MergeAsyncInternal(inputFiles, outputFile, CancellationToken.None, progress));
        }

        public Task MergeAsync(IEnumerable<InputFile> inputFiles, string outputFile, CancellationToken cancellationToken, IProgress<int> progress)
        {
            return Task.Run(() => MergeAsyncInternal(inputFiles, outputFile, cancellationToken, progress), cancellationToken);
        }

        private void MergeAsyncInternal(IEnumerable<InputFile> inputFiles, string outputFile, CancellationToken cancellationToken, IProgress<int> progress)
        {
            int processed = 0;
            int total = 0;

            using (PdfWriter writer = new PdfWriter(outputFile))
            using (PdfDocument outputDoc = new PdfDocument(writer))
            {
                outputDoc.InitializeOutlines();

                foreach (InputFile input in inputFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using (PdfReader reader = new PdfReader(input.Path))
                        using (PdfDocument inputDoc = new PdfDocument(reader))
                        {
                            int numPages = inputDoc.GetNumberOfPages();
                            input.PageCount = numPages;

                            inputDoc.CopyPagesTo(1, numPages, outputDoc);
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

                        if (progress != null)
                        {
                            progress.Report(total);
                        }
                    }
                }
            }
        }
    }
}
