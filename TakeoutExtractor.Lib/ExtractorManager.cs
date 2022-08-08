using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Top-level extractor class for Takeout archives
    /// </summary>
    public class ExtractorManager
    {
        /// <summary>
        /// Coonstructor. Initialise a TakeoutExtractor object.
        /// </summary>
        /// <param name="inputDir">Root input directory</param>
        /// <param name="outputDir">Root output directory</param>
        /// <param name="createLogFile">Log file.</param>
        /// <param name="options">Media-specific options.</param>
        public ExtractorManager(
            DirectoryInfo inputDir,
            DirectoryInfo outputDir,
            bool createLogFile,
            IEnumerable<IExtractorOptions> options)
        {
            this.inputDir = inputDir;
            this.outputDir = outputDir;
            this.createLogFile = createLogFile;
            this.options = options;
        }

        private readonly DirectoryInfo inputDir;
        private readonly DirectoryInfo outputDir;
        private readonly bool createLogFile;
        private readonly IEnumerable<IExtractorOptions> options;


        /// <summary>
        /// Progress event.
        /// </summary>
        public event EventHandler<ProgressEventArgs>? Progress;


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>IExtractorResults collection</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public async Task<IEnumerable<IExtractorResults>> ExtractAsync(CancellationToken cancellationToken)
        {
            if (!outputDir.Exists)
            {
                outputDir.Create();
            }

            Utf8JsonWriter logFileWtr = null;
            if (this.createLogFile)
            {
                var stm = new FileStream(Path.Combine(outputDir.FullName, "logfile.json"), FileMode.Create, FileAccess.Write);
                logFileWtr = new Utf8JsonWriter(stm, new JsonWriterOptions() { Indented = true });
                logFileWtr.WriteStartObject();
                logFileWtr.WriteStartObject("ExtractionLog");
                logFileWtr.WriteString("Started", DateTime.UtcNow.ToString("u"));
            }

            var results = new List<IExtractorResults>();
            try
            {
                var inSubDir = inputDir.GetDirectories().First(d => d.Name == "Google Photos");
                var opt = options.FirstOrDefault(o => o is PhotoOptions);
                if ((inSubDir != null) && (opt != null))
                {
                    var pe = new PhotoExtractor((opt as PhotoOptions)!, inSubDir, outputDir, logFileWtr);
                    pe.Progress += Extractor_Progress;
                    results.Add(await pe.ExtractAsync(cancellationToken));
                    pe.Progress -= Extractor_Progress;
                }
            }
            finally
            {
                if (logFileWtr != null)
                {
                    logFileWtr.WriteString("Finished", DateTime.UtcNow.ToString("u"));
                    logFileWtr.WriteEndObject();
                    logFileWtr.WriteEndObject();
                    await logFileWtr.FlushAsync();
                }
            }

            return results;
        }


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>IExtractorResults collection</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public IEnumerable<IExtractorResults> Extract(CancellationToken cancellationToken)
        {
            try
            {
                // Defer to the async version.
                return Task.Run(async () => await ExtractAsync(cancellationToken)).Result;
            }
            catch (AggregateException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                return null;  // Not reached
            }
        }



        private void Extractor_Progress(object? sender, ProgressEventArgs args)
        {
            if (this.Progress != null)
            {
                this.Progress(sender, args);  // intentionally pass original sender, not self
            }
        }
    }
}
