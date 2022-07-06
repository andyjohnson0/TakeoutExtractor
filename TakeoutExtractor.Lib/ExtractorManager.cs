using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// <param name="verbosity">Lverbosity level. 0 = quiet.</param>
        public ExtractorManager(
            DirectoryInfo inDir,
            DirectoryInfo outDir,
            int verbosity = 0)
        {
            this.inputDir = inDir;
            this.outputDir = outDir;
            this.verbosity = verbosity;
        }

        private DirectoryInfo inputDir;
        private DirectoryInfo outputDir;
        private int verbosity;


        /// <summary>
        /// Progress event.
        /// </summary>
        public event EventHandler<string>? Progress;


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="options">Media-specific options.</param>
        public void Extract(
            IEnumerable<Options> options)
        {
            if (!outputDir.Exists)
                outputDir.Create();

            var inSubDir = inputDir.GetDirectories().First(d => d.Name == "Google Photos");
            var opt = options.FirstOrDefault(o => o is PhotoOptions);
            if ((inSubDir != null) && (opt != null))
            {
                var pe = new PhotoExtractor((opt as PhotoOptions)!, inSubDir, outputDir, verbosity);
                pe.Progress += Extractor_Progress;
                pe.Extract();
                pe.Progress -= Extractor_Progress;
            }
        }

        private void Extractor_Progress(object? sender, string e)
        {
            if (this.Progress != null)
            {
                this.Progress(this, e);
            }
        }
    }
}
