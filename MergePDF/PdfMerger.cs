using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
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

            using (FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
            using (Document output = new Document())
            using (PdfCopy copy = new PdfCopy(output, stream))
            {
                output.Open();

                foreach (InputFile input in inputFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using (PdfReader reader = new PdfReader(input.Path))
                        {
                            copy.AddDocument(reader);
                            copy.FreeReader(reader);
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
