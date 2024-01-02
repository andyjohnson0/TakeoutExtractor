using System;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using uk.andyjohnson.TakeoutExtractor.Lib;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;

namespace uk.andyjohnson.TakeoutExtractor.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Parse command-line
            var commands = new string[] { "photo" };
            var cl = CommandLine.Create(args, commands);
            if (cl.Count == 0)
            {
                ShowHelp();
                return 0;
            }

            //
            var globalOptions = new GlobalOptions();
            var mediaOptions = new List<IExtractorOptions>();
            foreach(var kvp in cl)
            {
                switch (kvp.Key)
                {
                    case "":
                        if (kvp.Value.GetArgFlag("?", argAltNames: new string[] { "h", "help" }))
                        {
                            ShowHelp();
                            return 0;
                        }
                        globalOptions = new GlobalOptions()
                        {
                            InputDir = kvp.Value.GetArgDir("i", required: true),
                            OutputDir = kvp.Value.GetArgDir("o", required: true),
                            LogFile = kvp.Value.GetArgEnum<GlobalOptions.LogFileType>("lf",
                                                                                      new string?[] { "none", "json", "xml" },
                                                                                      defaultValue: GlobalOptions.Defaults.LogFile),

                            StopOnError = kvp.Value.GetArgBool("se", defaultValue: GlobalOptions.Defaults.StopOnError)
                        };
                        break;
                    case "photo":
                        var opt = new PhotoOptions()
                        {
                            OutputFileNameFormat = kvp.Value.GetArgString("fm", defaultValue: PhotoOptions.Defaults.OutputFileNameFormat)!,
                            OutputFileNameTimeKind = kvp.Value.GetArgEnum<DateTimeKind>("ft",
                                                                                        new string?[] { null, "utc", "local" },
                                                                                        defaultValue: PhotoOptions.Defaults.OutputFileNameTimeKind),
                            OutputFileVersionOrganisation = kvp.Value.GetArgEnum<PhotoFileVersionOrganisation>("fv",
                                                                                                               new string?[] { null, "lv", "lvo", "avs", "avx", "ev", "ov" },
                                                                                                               defaultValue: PhotoOptions.Defaults.OutputFileVersionOrganisation),
                            UpdateExif = kvp.Value.GetArgBool("ux", defaultValue: PhotoOptions.Defaults.UpdateExif),
                            OutputDirOrganisation = kvp.Value.GetArgEnum<PhotoDirOrganisation>("fd",
                                                                                               new string?[] { null, "y", "ym", "ymd" },
                                                                                               defaultValue: PhotoOptions.Defaults.OutputDirOrganisation),
                            ExtractDeletedFiles = kvp.Value.GetArgBool("xd", defaultValue: false)
                        };
                        mediaOptions.Add(opt);
                        break;
                }
            }

            if ((globalOptions?.InputDir == null) || (globalOptions?.OutputDir == null))
                throw new CommandLineException("Input and/or output directories not specified");  // This should never happen but it keeps the compiler happy.

            var extractor = new ExtractorManager(globalOptions, mediaOptions);
            extractor.Progress += Extractor_Progress;
            try
            {
                // Validate general options
                if (!globalOptions.InputDir.Exists)
                    throw new InvalidOperationException("Input directory does not exist");
                if (globalOptions.OutputDir.IsSubirOf(globalOptions.InputDir) || globalOptions.InputDir.IsSubirOf(globalOptions.OutputDir))
                    throw new InvalidOperationException("Input and output directory paths must not overlap");

                // Validate options. Vaiidators throw expections to be caught below.
                mediaOptions.ForEach(o => o.Vaildate());

                // Perform the extraction.
                var results = await extractor.ExtractAsync(CancellationToken.None);

                // Display the results.
                var alerts = results.SelectMany(a => a.Alerts);
                var errorCount = alerts.Count(a => a.Type == ExtractorAlertType.Error);
                var warningCount = alerts.Count(a => a.Type == ExtractorAlertType.Warning);
                var infoCount = alerts.Count(a => a.Type == ExtractorAlertType.Information);
                Console.WriteLine($"{errorCount} error, {warningCount} warning, {infoCount} information");
                foreach(var alert in alerts)
                {
                    alert.Write(Console.Out);
                }

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
            Console.WriteLine("    -lf none | json | sml");
            Console.WriteLine("        Create logfile of specified type. Default: none.");
            Console.WriteLine("    -se true/false");
            Console.WriteLine("        Stop on error. Default: false.");
            Console.WriteLine("    -h");
            Console.WriteLine("        Display help/usage information");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("    photo");
            Console.WriteLine("        Extract photos and videos");
            Console.WriteLine("        Options:");
            Console.WriteLine("            -fm format_str");
            Console.WriteLine("                Time-based format for output file names. Default \"yyyyMMdd_HHmmss.\"");
            Console.WriteLine("            -ft time_kind");
            Console.WriteLine("                Kind of time for output file name. Values can be utc or local. Default: local");
            Console.WriteLine("            -fv lv | lvo | avs | avx | ev | ov");
            Console.WriteLine("                Determines whether and how original and edited versions are placed in the output. Values can be:");
            Console.WriteLine("                    lv:  latest version only");
            Console.WriteLine("                    lvo: latest version, with originals in a subdirectory");
            Console.WriteLine("                    avs: all versions in same directory, with filename suffixes to disambiguate");
            Console.WriteLine("                    avx: all versions in separate original and edited directories");
            Console.WriteLine("                    ev:  edited versions only");
            Console.WriteLine("                    ov:  original versions only");
            Console.WriteLine("            -fd y | ym | ymb");
            Console.WriteLine("                Create subdirectories for year, year and month, or year and month and day. Default: none.");
            Console.WriteLine("            -ux true | false");
            Console.WriteLine("                Update edited EXIF information in output files. Default: true.");
            Console.WriteLine("            -xd true | false");
            Console.WriteLine("                Extract the deleted files in the Bin directory. Default: false.");
            Console.WriteLine();
            Console.WriteLine("(end)");
            Console.WriteLine();
        }
    }
}
