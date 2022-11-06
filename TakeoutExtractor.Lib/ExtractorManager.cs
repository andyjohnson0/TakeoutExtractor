using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;

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
        /// <param name="globalOptions">Global options.</param>
        /// <param name="mediaOptions">Media-specific options.</param>
        public ExtractorManager(
            GlobalOptions globalOptions,
            IEnumerable<IExtractorOptions> mediaOptions)
        {
            this.globalOptions = globalOptions;
            this.mediaOptions = mediaOptions;
        }

        private readonly GlobalOptions globalOptions;
        private readonly IEnumerable<IExtractorOptions> mediaOptions;


        /// <summary>
        /// Progress event.
        /// </summary>
        public event EventHandler<ProgressEventArgs>? Progress;


        /// <summary>
        /// ExtractorAlert event.
        /// </summary>
        public event EventHandler<ExtractorAlertEventArgs>? Alert;


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>IExtractorResults collection</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public async Task<IEnumerable<IExtractorResults>> ExtractAsync(CancellationToken cancellationToken)
        {
            if (globalOptions?.InputDir == null)
                throw new InvalidOperationException("Input directory not specified");
            if (globalOptions?.OutputDir == null)
                throw new InvalidOperationException("Output directory not specified");

            if (!globalOptions.OutputDir!.Exists)
            {
                globalOptions.OutputDir.Create();
            }

            Utf8JsonWriter? logFileWtr = null;
            if (globalOptions.CreateLogFile)
            {
                var stm = new FileStream(Path.Combine(globalOptions.OutputDir.FullName, "logfile.json"), FileMode.Create, FileAccess.Write);
                logFileWtr = new Utf8JsonWriter(stm, new JsonWriterOptions() { Indented = true });
                logFileWtr.WriteStartObject();
                logFileWtr.WriteStartObject("ExtractionLog");
                logFileWtr.WriteString("Started", DateTime.UtcNow.ToString("u"));
            }

            var results = new List<IExtractorResults>();
            try
            {
                results.AddRange(await ExtractAsync(globalOptions.InputDir, globalOptions.OutputDir, cancellationToken, logFileWtr));
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


        private async Task<IEnumerable<IExtractorResults>> ExtractAsync(
            DirectoryInfo di,
            DirectoryInfo outputDir,
            CancellationToken cancellationToken,
            Utf8JsonWriter? logFileWtr)
        {
            var results = new List<IExtractorResults>();

            foreach (var subDir in di.EnumerateDirectories())
            {
                if (subDir == null)
                    continue;

                // Photos
                if (subDir.Name == "Google Photos")
                {
                    var opt = mediaOptions.FirstOrDefault(o => o is PhotoOptions);
                    if (opt != null)
                    {
                        var pe = new PhotoExtractor((opt as PhotoOptions)!, subDir, outputDir, logFileWtr);
                        pe.Progress += Extractor_Progress;
                        pe.Alert += Extractor_Alert;
                        results.Add(await pe.ExtractAsync(cancellationToken));
                        pe.Progress -= Extractor_Progress;
                        pe.Alert -= Extractor_Alert;
                    }
                }
                else
                {
                    // Recurse into unknown subdir.
                    results.AddRange(await ExtractAsync(subDir, outputDir, cancellationToken, logFileWtr));
                }
            }

            return results;
        }



        private void Extractor_Progress(object? sender, ProgressEventArgs args)
        {
            if (this.Progress != null)
            {
                this.Progress(sender, args);  // intentionally pass original sender, not self
            }
        }

        private void Extractor_Alert(object? sender, ExtractorAlertEventArgs args)
        {
            if (this.Alert != null)
            {
                this.Alert(sender, args);  // intentionally pass original sender, not self
            }

            if ((args?.Alert?.Type == ExtractorAlertType.Error) && this.globalOptions.StopOnError)
            {
                throw new InvalidOperationException("An error has occurred");
            }
        }
    }
}
