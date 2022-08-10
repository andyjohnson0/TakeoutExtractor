using System;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using uk.andyjohnson.TakeoutExtractor.Lib;


namespace uk.andyjohnson.TakeoutExtractor.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var msg = string.Format("Takeout Extractor v{0} by Andy Johnson. See https://github.com/andyjohnson0/TakeoutExtractor for info.",
                                    Assembly.GetExecutingAssembly().GetName().Version!.ToString());
            Console.WriteLine(msg);
            Console.WriteLine("Use /? or /h for help");

            //
            var options = new List<IExtractorOptions>();
            DirectoryInfo? inDir = null;
            DirectoryInfo? outDir = null;
            bool createLogFile = false;
            var commands = new string[] { "photo" };
            var cl = CommandLine.Create(args, commands);
            foreach(var kvp in cl)
            {
                switch(kvp.Key)
                {
                    case "":
                        if (kvp.Value.GetArgFlag("?", argAltNames: new string[] { "h", "help" }))
                        {
                            ShowHelp();
                            return 0;
                        }
                        inDir = kvp.Value.GetArgDir("i", required: true);
                        outDir = kvp.Value.GetArgDir("o", required: true);
                        createLogFile = kvp.Value.GetArgBool("lf", defaultValue: false);
                        break;
                    case "photo":
                        var opt = new PhotoOptions();
                        opt.OriginalsSuffix = kvp.Value.GetArgString("fm", defaultValue: opt.OutputFileNameFormat);
                        opt.KeepOriginalsForEdited = kvp.Value.GetArgBool("ox", defaultValue: false);
                        opt.OriginalsSubdirName = kvp.Value.GetArgString("od", defaultValue: opt.OriginalsSubdirName);
                        opt.OriginalsSuffix = kvp.Value.GetArgString("os", defaultValue: opt.OriginalsSuffix);
                        opt.UpdateExif = kvp.Value.GetArgBool("ux", defaultValue: opt.UpdateExif);
                        opt.OrganiseBy = kvp.Value.GetArgEnum<PhotoOptions.OutputFileOrganisation>("fd",
                                                                                                   new string?[] { null, "y", "ym", "ymd" },
                                                                                                   defaultValue: PhotoOptions.OutputFileOrganisation.None);
                        options.Add(opt);
                        break;
                }
            }

            if ((inDir == null) || (outDir == null))
                throw new CommandLineException("Input and/or output directories not specified");  // This should never happen but it keeps the compiler happy.

            var extractor = new ExtractorManager(inDir, outDir, createLogFile, options);
            extractor.Progress += Extractor_Progress;
            try
            {
                // Validate general options
                if (!inDir.Exists)
                    throw new InvalidOperationException("Input directory does not exist");
                if (outDir.IsSubirOf(inDir) || inDir.IsSubirOf(outDir))
                    throw new InvalidOperationException("Input and output directory paths must not overlap");

                // Validate options. Vaiidators throw expections to be caught below.
                options.ForEach(o => o.Vaildate());

                // Perform the extraction.
                await extractor.ExtractAsync(CancellationToken.None);

                // All done
                return 0;
            }
            catch(CommandLineException ex)
            {
                Console.WriteLine("Command error: " + ex.Message);
            }
            catch(InvalidOperationException ex)
            {
                Console.WriteLine("Validation error: " + ex.Message);
            }
            catch(OperationCanceledException)
            {
                Console.WriteLine("Extraction cancelled");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                extractor.Progress -= Extractor_Progress;
            }

            // All done.
            return 1;
        }



        private static void Extractor_Progress(object? sender, ProgressEventArgs e)
        {
            if ((e?.SourceFile != null) && (e?.DesinationFile != null))
            {
                Console.WriteLine($"{e.SourceFile.FullName} => {e.DesinationFile.FullName}");
            }
        }


        private static void ShowHelp()
        {
            var msg = string.Format("Takeout Extractor v{0} by Andy Johnson. See https://github.com/andyjohnson0/TakeoutExtractor for info.",
                                    Assembly.GetExecutingAssembly().GetName().Version!.ToString());
            Console.WriteLine(msg);
            Console.WriteLine();
            Console.WriteLine("Purpose:");
            Console.WriteLine("    Extract and neatly structure the contents of a Google™ Takeout archive");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("    tex global_options command1 command1_options command2 command2_options ...");
            Console.WriteLine();
            Console.WriteLine("Global options:");
            Console.WriteLine("    -i  input_dir");
            Console.WriteLine("    -o  output_dir");
            Console.WriteLine("    -lf true/false");
            Console.WriteLine("        Create json logfile in root output dir");
            Console.WriteLine("    -h");
            Console.WriteLine("        Help/usage information");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("    photo");
            Console.WriteLine("        Extract phots and videos");
            Console.WriteLine("        Options:");
            Console.WriteLine("            -fm format_str");
            Console.WriteLine("                Time-based format for output file names. E.g. \"yyyyMMdd_HHmmss.\"");
            Console.WriteLine("            -fd y | ym | ymb");
            Console.WriteLine("                Create subdirectories for year, year and month, or year and month and day. Default none.");
            Console.WriteLine("            -ox true/false");
            Console.WriteLine("                Extract original photos/videos");
            Console.WriteLine("            -od sub_dir");
            Console.WriteLine("                Put original photos/videos in subdir");
            Console.WriteLine("            -os suffix");
            Console.WriteLine("                Filename suffix for original photos/videos");
            Console.WriteLine("            -ux true | false");
            Console.WriteLine("                Update EXIF timestamps in output files. Default: true.");
            Console.WriteLine();
            Console.WriteLine("(end)");
            Console.WriteLine();
        }
    }
}
