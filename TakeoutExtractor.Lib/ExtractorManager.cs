using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// <param name="options">Media-specific options.</param>
        /// <param name="verbosity">Lverbosity level. 0 = quiet.</param>
        public ExtractorManager(
            DirectoryInfo inputDir,
            DirectoryInfo outputDir,
            IEnumerable<IExtractorOptions> options,
            int verbosity = 0)
        {
            this.inputDir = inputDir;
            this.outputDir = outputDir;
            this.options = options;
            this.verbosity = verbosity;
        }

        private DirectoryInfo inputDir;
        private DirectoryInfo outputDir;
        private IEnumerable<IExtractorOptions> options;
        private int verbosity;


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
                outputDir.Create();

            var results = new List<IExtractorResults>();

            var inSubDir = inputDir.GetDirectories().First(d => d.Name == "Google Photos");
            var opt = options.FirstOrDefault(o => o is PhotoOptions);
            if ((inSubDir != null) && (opt != null))
            {
                var pe = new PhotoExtractor((opt as PhotoOptions)!, inSubDir, outputDir, verbosity);
                pe.Progress += Extractor_Progress;
                results.Add(await pe.ExtractAsync(cancellationToken));
                pe.Progress -= Extractor_Progress;
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
