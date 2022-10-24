using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using ExifLibrary;
using System.Diagnostics;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;
using System.Reflection;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Tests.Photo
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static class PhotoTestsHelper
    {
        /// <summary>
        /// Create an image pair comprising an original image, an optional edited image, and a json sidecar manifest.
        /// The created images have no exif data.
        /// </summary>
        /// <param name="imageDir">Image directory</param>
        /// <param name="baseName">Base file name, excluding path and extension.</param>
        /// <param name="imageFormat">Image format</param>
        /// <param name="createdTime">Time that the original image was created</param>
        /// <param name="modifiedTime">Time that the edited image was created, or null if there is no edited image.</param>
        /// <param name="description">Description</param>
        /// <param name="exifLocation">
        /// Original location information.
        /// This is always specified but can be the "null" location to simulate the case whetre the camera did not capture a location.
        /// </param>
        /// <param name="editedLocation">
        /// Optional edited location information.
        /// Can be null to simulate the case where the location was not edited.
        /// </param>
        /// <returns>Original file and optional edited file</returns>
        public static (FileInfo originalFile, FileInfo? editedFile) CreateImagePair(
            DirectoryInfo imageDir,
            string baseName,
            ImageFormat imageFormat,
            DateTime createdTime,
            DateTime? modifiedTime,
            string description,
            LatLonAltLocation exifLocation,
            LatLonAltLocation? editedLocation)
        {
            if (imageDir == null)
                throw new ArgumentNullException(nameof(imageDir));
            if (!imageDir.Exists)
                throw new ArgumentException($"Image directory does not exist: {imageDir}", nameof(imageDir));
            if (baseName == null)
                throw new ArgumentNullException(nameof(baseName));
            if (Path.GetFileNameWithoutExtension(baseName) != baseName)
                throw new ArgumentException($"baseName must not contain path or extension: {baseName}", nameof(baseName));
            if (imageFormat == null)
                throw new ArgumentNullException(nameof(imageFormat));

            const int maxTakeoutFileNameLen = 47;

            // Get image extension
            string imageFileExt = GetImageExt(imageFormat);

            // If the base name ends in a uniqueness suffix such as "(1)" then remove it and retain for use below.
            var uniquenessSuffix = "";
            if (baseName.EndsWith(')'))
            {
                var i = baseName.LastIndexOf('(');
                if (i != -1)
                {
                    uniquenessSuffix = baseName.Substring(i);
                    baseName = baseName.Replace(uniquenessSuffix, "");
                }
            }

            createdTime = createdTime.ToUniversalTime();
            modifiedTime = modifiedTime.HasValue ? modifiedTime.Value.ToUniversalTime() : null;
            if (editedLocation == null)
                editedLocation = exifLocation;  // If there is no edited location then use the exif location, which is what google does.

            // Create a plausible json sidecar
            string templateStr;
            using (var templateStm = Assembly.GetExecutingAssembly().GetManifestResourceStream("uk.andyjohnson.TakeoutExtractor.Lib.Tests.Photo.manifest_template.json"))
            {
                if (templateStm == null)
                    throw new InvalidOperationException("Failed to load json template");
                using (var rdr = new StreamReader(templateStm))
                {
                    templateStr = rdr.ReadToEnd().
                        Replace("$Title$",
                                baseName + imageFileExt).
                        Replace("$Description$",
                                description).
                        Replace("$CreationTime_Timestamp$",
                                new DateTimeOffset(createdTime).ToUnixTimeSeconds().ToString()).
                        Replace("$CreationTime_Formatted$",
                                createdTime.ToString("d MMM yyyy, HH:mm:ss UTC")).
                        Replace("$ModifiedTime_Timestamp$",
                                new DateTimeOffset(modifiedTime.HasValue ? modifiedTime.Value : createdTime).ToUnixTimeSeconds().ToString()).
                        Replace("$ModifiedTime_Formatted$",
                                (modifiedTime.HasValue ? modifiedTime.Value : createdTime).ToString("d MMM yyyy, HH:mm:ss UTC")).
                        Replace("$ExifLocation_Latitude$",
                                $"{exifLocation.latDeg}").
                        Replace("$ExifLocation_Longitude$",
                                $"{exifLocation.lonDeg}").
                        Replace("$ExifLocation_Altitude$",
                                $"{exifLocation.alt}").
                        Replace("$EditedLocation_Latitude$",
                                $"{editedLocation.Value.latDeg}").
                        Replace("$EditedLocation_Longitude$",
                                $"{editedLocation.Value.lonDeg}").
                        Replace("$EditedLocation_Altitude$",
                                $"{editedLocation.Value.alt}");
                }
            }
            var manifestFile = new FileInfo(Path.Join(imageDir.FullName, baseName + imageFileExt + uniquenessSuffix + ".json")).TrimName(maxTakeoutFileNameLen - 1);
            if (manifestFile.Exists)
                throw new InvalidOperationException($"Non-unique manifest filename generated: {manifestFile.FullName}");
            using (var fileWtr = new StreamWriter(manifestFile.FullName))
            {
                fileWtr.Write(templateStr);
            }

            var imageSize = new Size(800, 800);
            var rnd = new Random();
            var imageColour = Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255));

            // Create mandatory original file
            var originalFile = new FileInfo(Path.Join(imageDir.FullName, baseName + uniquenessSuffix + imageFileExt)).TrimName(maxTakeoutFileNameLen);
            if (originalFile.Exists)
                throw new InvalidOperationException($"Non-unique original filename generated: {originalFile.FullName}");
            CreateImage(originalFile, imageSize, imageColour, "Original", imageFormat);

            // Create optional edited file
            FileInfo? editedFile = null;
            if (modifiedTime.HasValue)
            {
                editedFile = new FileInfo(Path.Join(imageDir.FullName, baseName + "-edited" + uniquenessSuffix + imageFileExt)).TrimName(maxTakeoutFileNameLen);
                if (editedFile.Exists)
                    editedFile = AppendUniquenessSuffix(editedFile);
                CreateImage(editedFile, imageSize, imageColour, "Edited", imageFormat);
            }

            return (originalFile, editedFile);
        }



        /// <summary>
        /// Confirm that a pair of extracted, output files are the same as the corresponding input files.
        /// This is done by comparing provenance strings embedded in the EXIF Image Unique ID field,
        /// and also by comparing the image pixel data.
        /// </summary>
        /// <param name="sourceOriginalFile">Source original file that was an input to the extraction</param>
        /// <param name="extractedOriginalFile">Extracted original file that was an output from the extraction</param>
        /// <param name="sourceEditedFile">Source edited file that was an input to the extraction. Can be null if there is no edited file.</param>
        /// <param name="extractedEditedFile">Extracted edited file that was an output from the extraction. Can be null if there is no edited file.</param>
        /// <param name="expectedDescription">Expected description.</param>
        /// <param name="expectedExifLocation">Expected exif location.</param>
        /// <param name="expectedEditedLocation">Optional expected edited location.</param>
        /// <returns>True if the images in the image set match.</returns>
        public static bool ValidateExtractedImagePair(
            FileInfo sourceOriginalFile,
            FileInfo extractedOriginalFile,
            FileInfo? sourceEditedFile,
            FileInfo? extractedEditedFile,
            string expectedDescription,
            LatLonAltLocation expectedExifLocation,
            LatLonAltLocation? expectedEditedLocation)
        {
            if (sourceOriginalFile == null)
                throw new ArgumentNullException(nameof(sourceOriginalFile));
            if (extractedOriginalFile == null)
                throw new ArgumentNullException(nameof(extractedOriginalFile));
            if (sourceEditedFile == null && extractedEditedFile != null)
                throw new ArgumentNullException(nameof(sourceOriginalFile));

            var valid = ValidateExtractedImagePair(sourceOriginalFile, extractedOriginalFile, "Original",
                                                   expectedDescription,
                                                   expectedExifLocation);
            if (sourceEditedFile != null && extractedEditedFile != null)
            {
                valid = valid && ValidateExtractedImagePair(sourceEditedFile, extractedEditedFile, "Edited",
                                                            expectedDescription,
                                                            expectedEditedLocation != null ? expectedEditedLocation.Value : expectedExifLocation);

            }

            return valid;
        }


        private static bool ValidateExtractedImagePair(
            FileInfo sourceFile,
            FileInfo extractedFile,
            string annotation,
            string expectedDescription,
            LatLonAltLocation expectedLocation)
        {
            if (!CompareImageFiles(sourceFile, extractedFile))
                return false;

            var expectedLocationDms = GeoLocation.ToLatLonDmsAlt(expectedLocation);

            var outputImageFile = ImageFile.FromFile(extractedFile.FullName);

            // Check the provenance string in the ImageUniqieID exif element.
            var prop = outputImageFile.Properties.Get(ExifTag.ImageUniqueID);
            if (prop == null || (string)prop.Value != annotation + "|" + sourceFile.Name)
                return false;

            // Check description
            {
                var descProp = outputImageFile.Properties.Get(ExifTag.ImageDescription);
                if ((string)descProp.Value != expectedDescription)
                    return false;
            }

            // Check latitude
            {
                var locProp = outputImageFile.Properties.Get(ExifTag.GPSLatitude) as GPSLatitudeLongitude;
                var locRefProp = outputImageFile.Properties.Get(ExifTag.GPSLatitudeRef) as ExifEnumProperty<GPSLatitudeRef>;
                if (locProp == null || locRefProp == null)
                    return false;

                var loc = new GPSLatitudeLongitude(ExifTag.GPSLatitude, (float)Math.Abs(expectedLocationDms.latDeg), (float)expectedLocationDms.latMin, (float)expectedLocationDms.latSec);
                if (locProp.ToString(0) != loc.ToString(0))  // omit fractions of second from test due to round-trip rounding
                    return false;
            }

            // Check longitude
            {
                var locProp = outputImageFile.Properties.Get(ExifTag.GPSLongitude) as GPSLatitudeLongitude;
                var locRefProp = outputImageFile.Properties.Get(ExifTag.GPSLongitudeRef) as ExifEnumProperty<GPSLongitudeRef>;
                if (locProp == null || locRefProp == null)
                    return false;

                var loc = new GPSLatitudeLongitude(ExifTag.GPSLongitude, (float)Math.Abs(expectedLocationDms.lonDeg), (float)expectedLocationDms.lonMin, (float)expectedLocationDms.lonSec);
                if (locProp.ToString(0) != loc.ToString(0))  // omit fractions of second from test due to round-trip rounding
                    return false;
            }

            // Check altitude
            {
                var altProp = outputImageFile.Properties.Get(ExifTag.GPSAltitude) as ExifURational;
                var altRefProp = outputImageFile.Properties.Get(ExifTag.GPSAltitudeRef) as ExifEnumProperty<GPSAltitudeRef>;
                if (altProp == null || altRefProp == null)
                    return false;

                // omit fractions of altitude from test due to round-trip rounding
                var alt = (altRefProp == GPSAltitudeRef.AboveSeaLevel) ? altProp.Value.ToDecimal() : -altProp.Value.ToDecimal();
                if (Math.Round(alt, 0, MidpointRounding.ToEven) != Math.Round(expectedLocation.alt, 0, MidpointRounding.ToEven))
                    return false;
            }

            return true;
        }





        #region Implementation

        private static FileInfo AppendUniquenessSuffix(FileInfo fi)
        {
            var i = 0;
            while (i < 9999)
            {
                var fi2 = fi.AppendToName(i > 0 ? string.Format($"({i})") : "");
                if (!fi2.Exists)
                    return fi2;
                i += 1;
            }
            throw new InvalidOperationException($"File uniqueness number overflow for {fi.FullName}");
        }



        private static void CreateImage(
            FileInfo fi,
            Size size,
            Color colour,
            string? annotation,
            ImageFormat imageFormat)
        {
            if (fi.Exists)
                throw new InvalidOperationException($"Image file would be overwritten: {fi.FullName}");

            var f = new Font("Arial", 24, FontStyle.Bold);
            using (var bmp = new Bitmap(size.Width, size.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    // Fill with solid colour
                    g.Clear(colour);

                    // Draw filename at top/left
                    var ts = g.MeasureString(fi.Name, f);
                    g.FillRectangle(Brushes.White, 0, 0, ts.Width, ts.Height);
                    g.DrawString(fi.Name, f, Brushes.Black, 0, 0);

                    // Draw annotation at centre
                    if (annotation != null)
                    {
                        ts = g.MeasureString(annotation, f);
                        var tr = new RectangleF() { X = (size.Width - ts.Width) / 2, Y = (size.Height - ts.Height) / 2, Width = ts.Width, Height = ts.Height };
                        g.FillRectangle(Brushes.White, tr);
                        g.DrawString(annotation, f, Brushes.Black, tr);
                    }
                }
                bmp.Save(fi.FullName, imageFormat);
            }

            var imageFile = ImageFile.FromFile(fi.FullName);
            imageFile.Properties.Set(ExifTag.ImageUniqueID, annotation + "|" + fi.Name);
            imageFile.Save(fi.FullName);
        }



        /// <summary>
        /// Compare two image files.
        /// Compares only the visible pixels, not exif data etc.
        /// </summary>
        /// <param name="imageFile1">First image file.</param>
        /// <param name="imageFile2">Second image file.</param>
        /// <returns>True if the image pixels are identical, or false if they differ.</returns>
        private static bool CompareImageFiles(
            FileInfo imageFile1,
            FileInfo imageFile2)
        {
            using (var bmp1 = new Bitmap(imageFile1.FullName))
            {
                using (var bmp2 = new Bitmap(imageFile2.FullName))
                {
                    if (bmp1.Size != bmp2.Size)
                        return false;
                    for (int x = 0; x < bmp1.Size.Width; x++)
                    {
                        for (int y = 0; y < bmp1.Size.Height; y++)
                        {
                            if (bmp1.GetPixel(x, y) != bmp2.GetPixel(x, y))
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        #endregion Implementation


        #region Utility methods

        /// <summary>
        /// Get the file extension corresponding to an image format.
        /// </summary>
        /// <param name="imageFormat">Image format</param>
        /// <returns>File extension - e.g. ".jpg"</returns>
        /// <exception cref="ArgumentException">Unsupported image format</exception>
        public static string GetImageExt(ImageFormat imageFormat)
        {
            if (imageFormat == ImageFormat.Jpeg)
                return ".jpg";
            else if (imageFormat == ImageFormat.Png)
                return ".png";
            else if (imageFormat == ImageFormat.Gif)
                return ".gif";
            else
                throw new ArgumentException($"Unsupported image format: {imageFormat}", nameof(imageFormat));
        }


        /// <summary>
        /// Create a temporary directory with an optional name suffix
        /// </summary>
        /// <param name="suffix">P[tional name suffix.</param>
        /// <returns>DirectoryInfo object referenving the temporary directory.</returns>
        public static DirectoryInfo CreateTempDir(string suffix = "")
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), suffix + Path.GetRandomFileName());
            return Directory.CreateDirectory(tempDirectory);
        }


        /// <summary>
        /// Generate a random location
        /// </summary>
        /// <returns>A LatLonAltLocation record containing random but realistic location values</returns>
        public static LatLonAltLocation BuildRandomLocation()
        {
            /* const */
            decimal[] epsilons = new decimal[] { 0.00000000000001M, 0.0000009999999997M };
            var sf = (int)Math.Pow(10D, 6D);

            var rnd = new Random();
            var lat = rnd.Next(-90 * sf, +90 * sf) / (decimal)sf + epsilons[rnd.Next(epsilons.Length - 1)];
            var lon = rnd.Next(-180 * sf, +180 * sf) / (decimal)sf + epsilons[rnd.Next(epsilons.Length - 1)];
            var alt = rnd.Next(-9999, +99999) / 1000M;
            return new LatLonAltLocation(lat, lon, alt);
        }


        /// <summary>
        /// Generate a random dateTime value between two inclusive limits.
        /// If the kinds of minValue and maxValue differ from the kind parameter then conversion to the required
        /// kind can result in the returned timestamp being slightly outside the required range.
        /// </summary>
        /// <param name="minValue">Minimum limit. ust be less than maxValue.</param>
        /// <param name="maxValue">Maximum limit. Must be greated than minValue.</param>
        /// <param name="kind">Time kind of return value.</param>
        /// <returns>Random timestamp between specified limits.</returns>
        public static DateTime BuildRandomTimestamp(
            DateTime minValue,
            DateTime maxValue,
            DateTimeKind kind)
        {
            if (minValue >= maxValue)
                throw new ArgumentException();

            var window = maxValue.Ticks - minValue.Ticks;
            var dt = minValue.AddTicks(new Random().NextInt64(window));
            return DateTime.SpecifyKind(dt, kind);
        }


        /// <summary>
        /// Generate a random line of text description
        /// </summary>
        /// <returns>Random text</returns>
        public static string BuildRandomDescription()
        {
            var rnd = new Random();
            var chars = "abcdefghijklmnopqrstuvwxyz";

            var sb = new StringBuilder();
            var wordCount = 6 + rnd.Next(6);
            for (var iWord = 0; iWord < wordCount; iWord++)
            {
                var letterCount = 3 + rnd.Next(6);
                for (var iLetter = 0; iLetter < letterCount; iLetter++)
                {
                    sb.Append(chars[rnd.Next(chars.Length)]);
                }
                sb.Append(' ');
            }
            return sb.ToString();
        }

        #endregion Utility methods
    }
}
