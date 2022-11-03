﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

using ExifLibrary;
using System.Reflection.Metadata.Ecma335;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Photo
{
    /// <summary>
    /// Photo/video extractor.
    /// </summary>
    public partial class PhotoExtractor
    {
        /// <summary>
        /// Coonstructor. Initialise a PhotoExtractor object.
        /// </summary>
        /// <param name="options">Photo options</param>
        /// <param name="inputDir">Root input directory</param>
        /// <param name="outputDir">Root output directory</param>
        /// <param name="logFileWtr">Log file writer. Can be null.</param>
        public PhotoExtractor(
            PhotoOptions options,
            DirectoryInfo inputDir,
            DirectoryInfo outputDir,
            Utf8JsonWriter? logFileWtr)
        {
            this.options = options;
            this.inputDir = inputDir;
            this.outputDir = outputDir;
            this.logFileWtr = logFileWtr;
        }

        private PhotoOptions options;
        private DirectoryInfo inputDir;
        private DirectoryInfo outputDir;
        private Utf8JsonWriter? logFileWtr;


        /// <summary>
        /// Progress event.
        /// </summary>
        public event EventHandler<ProgressEventArgs>? Progress;


        /// <summary>
        /// An ExtractorAlert has been generated.
        /// </summary>
        public event EventHandler<ExtractorAlertEventArgs>? Alert;



        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PhotoResults object describing results</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public async Task<PhotoResults> ExtractAsync(CancellationToken cancellationToken)
        {
            // Validate options.
            // If keeping originals then, to prevent file name clash, subdir and/or suffix option must be set.
            if (options.KeepOriginalsForEdited && string.IsNullOrEmpty(options.OriginalsSubdirName) && string.IsNullOrEmpty(options.OriginalsSuffix))
                throw new InvalidOperationException("Original file subdir and/or suffix option must be set to prevent file name clash");

            if (logFileWtr != null)
            {
                logFileWtr.WriteStartObject("PhotosAndVideos");
                logFileWtr.WriteStartArray("ExtractedFiles");
            }

            // Do it.
            var results = new PhotoResults();
            results.StartTime = DateTime.UtcNow;
            await ProcessInputDirAsync(inputDir, outputDir, options, results, cancellationToken);
            results.Duration = DateTime.UtcNow - results.StartTime;

            if (logFileWtr != null)
            {
                logFileWtr.WriteEndArray();

                logFileWtr.WriteStartObject("Results");
                logFileWtr.WriteString("TimeTaken", results.Duration.ToString("hh\\:mm\\:ss"));
                logFileWtr.WriteNumber("InputGroupTotalCount", results.InputGroupCount);
                logFileWtr.WriteNumber("InputGroupEditedCount", results.InputEditedCount);
                logFileWtr.WriteNumber("InputGroupUneditedCount", results.InputUneditedCount);
                logFileWtr.WriteNumber("OutputTotalFileCount", results.OutputFileCount);
                logFileWtr.WriteNumber("OutputEditedFileCount", results.OutputEditedCount);
                logFileWtr.WriteNumber("OutputUneditedFileCount", results.OutputUneditedCount);
                logFileWtr.WriteNumber("CoveragePercent", results.Coverage * 100M);
                logFileWtr.WriteEndObject();

                logFileWtr.WriteEndObject();
            }

            return results;
        }


        /// <summary>
        /// Perform extraction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PhotoResults object describing results</returns>
        /// <exception cref="InvalidOperationException">Invalid options.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
        public PhotoResults Extract(CancellationToken cancellationToken)
        {
            try
            {
                // Defer to the async version.
                return Task.Run(async () => await ExtractAsync(cancellationToken)).Result;
            }
            catch (AggregateException ex)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                return null; // Not reached
            }
        }


        #region Implementation

        private const string starDotJason = "*.json";
        private const int maxImageFileNamePartLen = 47;  // Max length of the name part of a source file


        /// <summary>
        /// Process an input directory by iteating over the jason sidecar files and attempting to extract the image file that they reference.
        /// </summary>
        /// <param name="inDir">Input directory</param>
        /// <param name="outDir">Output directory</param>
        /// <param name="options">Options</param>
        /// <param name="results">Results data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Nothing</returns>
        private async Task ProcessInputDirAsync(
            DirectoryInfo inDir,
            DirectoryInfo outDir,
            PhotoOptions options,
            PhotoResults results,
            CancellationToken cancellationToken)
        {
            // Process all json manifests in 'inDir'
            foreach (var sidecarFile in inDir.EnumerateFiles(starDotJason))
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                await ProcessInputFileAsync(sidecarFile, outDir, options, results);
            }

            // Recurse all subdirectories of 'inDir'.
            foreach (var subDir in inDir.EnumerateDirectories())
            {
                await ProcessInputDirAsync(subDir, outDir, options, results, cancellationToken);
            }
        }


        /// <summary>
        /// Process a (possible) json sidecar file, attempting to extract the timages that it references.
        /// </summary>
        /// <param name="sidecarFile">Json sidecar file</param>
        /// <param name="outDir">Output directory.</param>
        /// <param name="options">Options</param>
        /// <param name="results">Results</param>
        /// <returns>
        /// True if the sidecar was processed, otherwise false.
        /// A false return indicates a continuable condition, although alerts may have been raised.
        /// </returns>
        private async Task<bool> ProcessInputFileAsync(
            FileInfo sidecarFile,
            DirectoryInfo outDir,
            PhotoOptions options,
            PhotoResults results)
        {
            var mi = await PhotoMetadata.CreateFromSidecar(sidecarFile);
            if (mi == null)
            {
                // Not a sidecar file
                return false;
            }

            // Identify the original file from the title in the sidecar
            // First, substitute speciasl chars in the title and check that we have a filename extension.
            var originalFilename = mi.title.Replace('&', '_').Replace('?', '_');
            if (Path.GetExtension(originalFilename) == string.Empty)
            {
                // No file extension. This rarely happens.
                var alert = new ExtractorAlert(ExtractorAlertType.Error, $"Metadata title without file extension in {sidecarFile}")
                {
                    AssociatedFile = sidecarFile
                };
                results.Add(alert);
                RaiseAlert(alert);
                return false;
            }
            // Next, get the uniqueness suffix that may be embedded in the sidecar filename. It won't be in the title element.
            string uniquenessSuffix = ExtractUniquenessSuffix(sidecarFile);

            // Finally, try to build the original filename and check that it referes to a file that exists.
            var originalFile = new FileInfo(Path.Combine(sidecarFile.Directory!.ToString(), originalFilename)).TrimName(maxImageFileNamePartLen).AppendToName(uniquenessSuffix);
            if (originalFile == null || !originalFile.Exists)
            {
                var alert = new ExtractorAlert(ExtractorAlertType.Error, $"Failed to identify original content file for {sidecarFile}")
                {
                    AssociatedFile = sidecarFile
                };
                results.Add(alert);
                RaiseAlert(alert);
                return false;
            }

            // Identify the optional edited file.
            FileInfo? editedFile = MatchEditedFileToOriginal(originalFile, sidecarFile, uniquenessSuffix);

            // Update result counters.
            results.InputGroupCount += 1;
            if (editedFile != null)
                results.InputEditedCount += 1;
            else
                results.InputUneditedCount += 1;

            // Create output directory if necessary.
            DirectoryInfo destDir = CreateOutputDir(outDir, originalFile, mi.creationTime, options);

            // Create the output files by copying and renaming the original and optional edited file.
            if (editedFile != null)
            {
                // We have an edited file, and an original file too.

                // Create edited.
                var outFile = await CreateOutputFileAsync(editedFile, destDir, null,
                                                            mi.title, mi.description, mi.takenTime, mi.creationTime, mi.lastModifiedTime,
                                                            mi.editedLocation != GeoLocation.NullLatLonAltLocation ? mi.editedLocation : mi.exifLocation,
                                                            results);
                RaiseProgress(editedFile, outFile);
                results.OutputFileCount += 1;

                // Optionally create original. 
                if (options.KeepOriginalsForEdited)
                {
                    if (!string.IsNullOrEmpty(options.OriginalsSubdirName))
                        destDir = destDir.CreateSubdirectory(options.OriginalsSubdirName);
                    outFile = await CreateOutputFileAsync(originalFile, destDir, options.OriginalsSuffix,
                                                            mi.title, mi.description, mi.takenTime, mi.creationTime, mi.creationTime,
                                                            mi.exifLocation,
                                                            results);
                    RaiseProgress(originalFile, outFile);
                    results.OutputEditedCount += 1;
                }
            }
            else
            {
                // We have an original file only.
                var outFile = await CreateOutputFileAsync(originalFile, destDir, null,
                                                            mi.title, mi.description, mi.takenTime, mi.creationTime, mi.creationTime,
                                                            mi.editedLocation != GeoLocation.NullLatLonAltLocation ? mi.editedLocation : mi.exifLocation,
                                                            results);
                RaiseProgress(originalFile, outFile);
                results.OutputFileCount += 1;
                results.OutputUneditedCount += 1;
            }

            return true;
        }



        /// <summary>
        /// Create and return the output directory given the file media type, creation time, and options.
        /// </summary>
        /// <param name="outputDir">Base output directory</param>
        /// <param name="originalFile">Original file. Determines name of created directory.</param>
        /// <param name="creationTimeCreationTime">Original file's creation time.</param>
        /// <param name="options">Options.</param>
        /// <returns>The created output directory.</returns>
        private static DirectoryInfo CreateOutputDir(
            DirectoryInfo outputDir,
            FileInfo originalFile,
            DateTime originalFileCreationTime,
            PhotoOptions options)
        {
            if (originalFile.IsImageFile())
            {
                outputDir = outputDir.CreateSubdirectory("Photos");
            }
            else if (originalFile.IsVideoFile())
            {
                outputDir = outputDir.CreateSubdirectory("Videos");
            }
            else
            {
                outputDir = outputDir.CreateSubdirectory("Unknown");
            }

            if (options.OrganiseBy == PhotoOptions.OutputFileOrganisation.Year ||
                options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonth ||
                options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonthDay)
            {
                outputDir = outputDir.CreateSubdirectory(originalFileCreationTime.Year.ToString("D4"));
            }
            if (options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonth ||
                options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonthDay)
            {
                outputDir = outputDir.CreateSubdirectory(originalFileCreationTime.Month.ToString("D2"));
            }
            if (options.OrganiseBy == PhotoOptions.OutputFileOrganisation.YearMonthDay)
            {
                outputDir = outputDir.CreateSubdirectory(originalFileCreationTime.Day.ToString("D2"));
            }
            return outputDir;
        }


        /// <summary>
        /// Get optional uniqueness suffix.
        /// If a filename is not unique within the file-set then Takeout appends a (nnn) suffix to disambiguate it.
        /// Unfortunately is appends it in a different place in the json file name compared to the referenced
        /// file names. Also, the suffix doesn't occur in the embedded title element. Examples are:
        /// - IMG_20180830_123540573.jpg(1).json
        /// - IMG_20180830_123540573(1).jpg
        /// - IMG_20180830_123540573-edited(1).jpg
        /// </summary>
        /// <param name="fi">Information about the file</param>
        /// <returns>Uniqueness suffiex (e.g. "(1)") or an empty string if there is no suffix.</returns>
        private static string ExtractUniquenessSuffix(FileInfo fi)
        {
            var name = Path.GetFileNameWithoutExtension(fi.Name);
            if (name.EndsWith(")"))
            {
                var i = name.LastIndexOf('(');
                if (i != -1)
                {
                    return name.Substring(i);
                }
            }
            return string.Empty;
        }


        /// <summary>
        /// Use file name matching to identify the optional edited file given the original file.
        /// </summary>
        /// <param name="originalFile">Original, extant file</param>
        /// <param name="jsonManifestFile">Json manifest file</param>
        /// <param name="uniquenessSuffix">Uniqueness suffix, which may be an empty string</param>
        /// <returns>Edited file, or null if there is no edited file.</returns>
        private static FileInfo? MatchEditedFileToOriginal(
            FileInfo originalFile,
            FileInfo jsonManifestFile,
            string uniquenessSuffix)
        {
            if (!originalFile.Exists)
                throw new InvalidOperationException($"Original file does not exist: {originalFile.FullName}");
            if (!jsonManifestFile.Exists)
                throw new InvalidOperationException($"Manifest file does not exist: {jsonManifestFile.FullName}");

            // This is all a bit gnarly.
            // It isn't possible to determine the file name of the edited file, if it exists, because it may contains various suffixes
            // that the photo software uses to indicate that it is edited. All we know is that its name will begin wth the same characters
            // as the original file name, then there will be a suffix indicating that it is the edited version, then the uniqueness suffix
            // (if present) and the same file extension.
            // So we must search for it - any match that isn't the original must be the edited.
            // Examples:
            // 1. We want IMG_20180830_123540573(1).jpg to match with IMG_20180830_123540573-edited(1).jpg. But we don't want to match
            //    IMG_20180830_123540573-edited.jpg because thats the edited version of IMG_20180830_123540573.jpg - a different original file, 
            // 2. We want original_a5025662-cb40-45dd-be98-684ee48aa226_I.jpg to match original_a5025662-cb40-45dd-be98-684ee48aa226_I(1).jpg.
            //    Here the (1) is being used to distinguish the edited from its origial, which has 47 characters in its file name and is as long
            //    as it can be..
            var editedPattern = Path.GetFileNameWithoutExtension(originalFile.Name);
            if (uniquenessSuffix != "")
                editedPattern = editedPattern.Replace(uniquenessSuffix, "");
            editedPattern += "*" + uniquenessSuffix + Path.GetExtension(originalFile.Name);
            var matchingFiles = new List<FileInfo>(jsonManifestFile.Directory!.GetFiles(editedPattern));
            var i = matchingFiles.FindIndex(f => f.Name == originalFile.Name);
            if (i != -1)
                matchingFiles.RemoveAt(i);
            foreach (var matchingFile in matchingFiles)
            {
                var possManifest = new FileInfo(Path.Combine(originalFile.DirectoryName!, Path.GetFileNameWithoutExtension(originalFile.Name) + ".json"));
                if (possManifest.Exists && possManifest.FullName != jsonManifestFile.FullName)
                {
                    // There is a manifest for this matching image file and its not the current manifest. So its not a match.
                    continue;
                }

                // Match long file names.
                if (Path.GetFileNameWithoutExtension(matchingFile.FullName).Length >= maxImageFileNamePartLen &&
                     Path.GetFileNameWithoutExtension(matchingFile.FullName).StartsWith(Path.GetFileNameWithoutExtension(originalFile.FullName)))
                {
                    return matchingFile;
                }

                //
                if (matchingFile.FullName != originalFile.FullName &&
                     ExtractUniquenessSuffix(matchingFile) == ExtractUniquenessSuffix(originalFile))
                {
                    return matchingFile;
                }
            }

            // Not found
            return null;
        }



        /// <summary>
        /// Create an output file.
        /// </summary>
        /// <param name="sourceFile">Source file</param>
        /// <param name="outDir">Directory to create file in.</param>
        /// <param name="filenameSuffix">File name suffix. Can be null.</param>
        /// <param name="title">Image title. Can be null.</param>
        /// <param name="description">Image description. Can be null.</param>
        /// <param name="takenTime">Time that the photo was taken. Must not null.</param>
        /// <param name="creationTime">Time that the photo was created on the camera. Must not null.</param>
        /// <param name="lastModifiedTime">Last modified time (on phone or Google Photos). Can be null.</param>
        /// <param name="location">Location that the photo was taken at.</param>
        /// <param name="results">Results object. Must not be null.</param>
        /// <returns>FileInfo object referring to the created file.</returns>
        /// <exception cref="InvalidOperationException">File creation failed</exception>
        private async Task<FileInfo> CreateOutputFileAsync(
            FileInfo sourceFile,
            DirectoryInfo outDir,
            string? filenameSuffix,
            string? title,
            string? description,
            DateTime takenTime,
            DateTime creationTime,
            DateTime? lastModifiedTime,
            LatLonAltLocation location,
            PhotoResults results)
        {
            if (sourceFile == null)
                throw new ArgumentNullException(nameof(sourceFile));
            if (!sourceFile.Exists)
                throw new ArgumentException("Source file does not exist", nameof(sourceFile));
            if (outDir == null)
                throw new ArgumentNullException(nameof(outDir));
            if (!outDir.Exists)
                throw new ArgumentException("Output directory does not exist", nameof(sourceFile));

            for (int i = 0; i < 9999; i++)
            {
                // Build the output file name. Because images can have timestamps that differ by less than a second, we prepend
                // a uniqieness suffix if necessary to make the filename uniqie.
                var uniqSuffix = i == 0 ? "" : string.Format("-{0:000}", i);
                var filename = creationTime.ToString(options.OutputFileNameFormat) + uniqSuffix + filenameSuffix + sourceFile.Extension;
                var destFile = new FileInfo(Path.Combine(outDir.FullName, filename));
                if (destFile.Exists)
                {
                    continue;
                }
                File.Copy(sourceFile.FullName, destFile.FullName, false);

                if (options.UpdateExif && destFile.IsImageFileWithExif())
                {
                    try
                    {
                        var imageFile = await ImageFile.FromFileAsync(destFile.FullName);

                        if (!string.IsNullOrEmpty(title))
                            imageFile.Properties.Set(ExifTag.DocumentName, title);
                        if (!string.IsNullOrEmpty(description))
                            imageFile.Properties.Set(ExifTag.ImageDescription, description);
                        imageFile.Properties.Set(ExifTag.DateTime, new ExifDateTime(ExifTag.DateTime, takenTime));
                        imageFile.Properties.Set(ExifTag.DateTimeOriginal, new ExifDateTime(ExifTag.DateTime, creationTime));
                        if (location != GeoLocation.NullLatLonAltLocation)
                        {
                            var locDmsAlt = GeoLocation.ToLatLonDmsAlt(location);

                            // Latitude
                            var latitude = new GPSLatitudeLongitude(ExifTag.GPSLatitude, (float)Math.Abs(locDmsAlt.latDeg), (float)locDmsAlt.latMin, (float)locDmsAlt.latSec);
                            imageFile.Properties.Add(latitude);
                            var latitudeRef = new ExifEnumProperty<GPSLatitudeRef>(ExifTag.GPSLatitudeRef, locDmsAlt.latDeg >= 0M ? GPSLatitudeRef.North : GPSLatitudeRef.South);
                            imageFile.Properties.Add(latitudeRef);

                            // Longitude
                            var longitude = new GPSLatitudeLongitude(ExifTag.GPSLongitude, (float)Math.Abs(locDmsAlt.lonDeg), (float)locDmsAlt.lonMin, (float)locDmsAlt.lonSec);
                            imageFile.Properties.Add(longitude);
                            var longitudeRef = new ExifEnumProperty<GPSLongitudeRef>(ExifTag.GPSLongitudeRef, locDmsAlt.lonDeg >= 0M ? GPSLongitudeRef.East : GPSLongitudeRef.West);
                            imageFile.Properties.Add(longitudeRef);

                            // Altitiude
                            imageFile.Properties.Set(ExifTag.GPSAltitude, (float)Math.Abs(locDmsAlt.alt));
                            imageFile.Properties.Set(ExifTag.GPSAltitudeRef, locDmsAlt.alt >= 0 ? GPSAltitudeRef.AboveSeaLevel : GPSAltitudeRef.BelowSeaLevel);
                        }

                        await imageFile.SaveAsync(destFile.FullName);
                        if (imageFile.Errors?.Count > 0)
                        {
                            var imageAlert = new ExtractorAlert(ImageErrorsToAlertType(imageFile.Errors), "One or more errors occurred while updatimg image EXIF data")
                            {
                                AssociatedFile = destFile,
                                AssociatedObject = imageFile.Errors
                            };
                            results.Add(imageAlert);
                            RaiseAlert(imageAlert);
                        }
                    }
                    catch (Exception ex)
                    {
                        var exifAlert = new ExtractorAlert(ExtractorAlertType.Error, "An error occurred while updating image EXIF data")
                        {
                            AssociatedFile = destFile,
                            AssociatedException = ex
                        };
                        results.Add(exifAlert);
                        RaiseAlert(exifAlert);
                    }
                }

                File.SetCreationTime(destFile.FullName, creationTime);
                if (lastModifiedTime.HasValue)
                    File.SetLastWriteTime(destFile.FullName, lastModifiedTime.Value);

                if (logFileWtr != null)
                {
                    logFileWtr.WriteStartObject();
                    if (sourceFile.IsImageFile())
                        logFileWtr.WriteString("Type", "Photo");
                    else if (sourceFile.IsVideoFile())
                        logFileWtr.WriteString("Type", "Video");
                    else
                        logFileWtr.WriteString("Type", "Unknown");
                    logFileWtr.WriteString("Source", sourceFile.FullName);
                    logFileWtr.WriteString("Output", destFile.FullName);
                    logFileWtr.WriteString("CreationTime", creationTime.ToString("u"));
                    if (lastModifiedTime.HasValue)
                        logFileWtr.WriteString("ModifiedTime", lastModifiedTime.Value.ToString("u"));
                    else
                        logFileWtr.WriteNull("ModifiedTime");
                    logFileWtr.WriteEndObject();
                }

                return destFile;
            }

            // If we get to here then we were unable to generate a unique file name.
            var alert = new ExtractorAlert(ExtractorAlertType.Error, "Couldn't create a unique output filename")
            {
                AssociatedDirectory = outDir,
                AssociatedFile = sourceFile
            };
            RaiseAlert(alert);
            results.Add(alert);
            throw new InvalidOperationException("Couldn't create a unique output filename").AddData("alerts", results.Alerts);
        }


        private static ExtractorAlertType ImageErrorsToAlertType(IEnumerable<ImageError> errors)
        {
            if (errors.Any(e => e.Severity == Severity.Error))
                return ExtractorAlertType.Error;
            else if (errors.Any(e => e.Severity == Severity.Warning))
                return ExtractorAlertType.Warning;
            else
                return ExtractorAlertType.Information;
        }


        private void RaiseProgress(
            FileInfo sourceFile,
            FileInfo destinationFile)
        {
            if (Progress != null)
            {
                var args = new ProgressEventArgs(sourceFile, destinationFile);
                Progress(this, args);
            }
        }


        private void RaiseAlert(
            ExtractorAlert alert)
        {
            if (logFileWtr != null)
            {
                alert.Write(logFileWtr);
            }

            if (Alert != null)
            {
                var args = new ExtractorAlertEventArgs(alert);
                Alert(this, args);
            }
        }


        #endregion Implementation
    }
}