using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Tests
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static class PhotoTestsHelper
    {
        /// <summary>
        /// Create an image set comprising a json manifest, an original image, and an optional edited image.
        /// </summary>
        /// <param name="imageDir">Image directory</param>
        /// <param name="imageBaseName">Base file name</param>
        /// <param name="imageFormat">Image format</param>
        /// <param name="createdTime">TIme that the original image was created</param>
        /// <param name="modifiedTime">Time that the edited image was created, or null if there is no edited image.</param>
        public static void CreateImageSet(
            DirectoryInfo imageDir,
            string imageBaseName,
            ImageFormat imageFormat,
            DateTime createdTime,
            DateTime? modifiedTime)
        {
            const int maxTakeoutFileNameLen = 47;

            createdTime = createdTime.ToUniversalTime();
            modifiedTime = modifiedTime.HasValue ? modifiedTime.Value.ToUniversalTime() : null;

            // Create a plausible json manifest
            string templateStr;
            using (var templateStm = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("uk.andyjohnson.TakeoutExtractor.Lib.Tests.manifest_template.json"))
            {
                if (templateStm == null)
                    throw new InvalidOperationException("Failed to load json template");
                using (var rdr = new StreamReader(templateStm))
                {
                    templateStr = rdr.ReadToEnd()
                        .Replace("\"{", "\"[[")
                        .Replace("}\"", "]]\"")
                        .Replace("{", "{{")
                        .Replace("}", "}}")
                        .Replace("\"[[", "\"{")
                        .Replace("]]\"", "}\"");
                    templateStr = String.Format(
                        templateStr,
                        imageBaseName,                                                                                        // title incl. extension
                        new DateTimeOffset(createdTime).ToUnixTimeSeconds(),
                        createdTime.ToString("d MMM yyyy, HH:mm:ss UTC"),                                                     // e.g. "1 Mar 2022, 15:28:43 UTC"
                        new DateTimeOffset(modifiedTime.HasValue ? modifiedTime.Value : createdTime).ToUnixTimeSeconds(),
                        (modifiedTime.HasValue ? modifiedTime.Value : createdTime).ToString("d MMM yyyy, HH:mm:ss UTC"));     // e.g. "1 Mar 2022, 15:33:11 UTC"
                    templateStr = templateStr
                        .Replace("{{", "{")
                        .Replace("}}", "}");
                }
            }
            var fn = new FileInfo(Path.Join(imageDir.FullName, imageBaseName)).TrimName(maxTakeoutFileNameLen);
            using (var fileWtr = new StreamWriter(fn + ".json"))
            {
                fileWtr.Write(templateStr);
            }

            var imageSize = new Size(1000, 1000);
            var rnd = new System.Random();
            var imageColour = Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255));
            CreateImage(fn, imageSize, imageColour, "Original", imageFormat);
            if (modifiedTime.HasValue)
            {
                CreateImage(fn.AppendToName("-edited"), imageSize, imageColour, "Edited", imageFormat);
            }
        }



        private static void CreateImage(
            FileInfo fi,
            Size size,
            Color colour,
            string? annotation,
            ImageFormat imageFormat)
        {
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
        }


        /// <summary>
        /// Compare two image files.
        /// Compares only the visible pixels, not exif data etc.
        /// </summary>
        /// <param name="imageFile1">First image file.</param>
        /// <param name="imageFile2">Second image file.</param>
        /// <returns>True if the image pixels are identical, or false if they differ.</returns>
        public static bool CompareImageFiles(
            FileInfo imageFile1,
            FileInfo imageFile2)
        {
            using(var bmp1 = new Bitmap(imageFile1.FullName))
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
        /// EXIF original date time.
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.drawing.imaging.propertyitem.id?view=dotnet-plat-ext-6.0
        /// </summary>
        public const int PropertyTagOriginalDateTime = 0x9003;

        /// <summary>
        /// EXIF DateTime property. This is the last modified time.
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.drawing.imaging.propertyitem.id?view=dotnet-plat-ext-6.0
        /// </summary>
        public const int PropertyTagDateTime = 0x0132;


        /// <summary>
        /// Get the value of a bitmap/image's timestamp property.
        /// </summary>
        /// <param name="bmp">Bitmap</param>
        /// <param name="propertyId">
        /// Property ID. 
        /// See https://docs.microsoft.com/en-us/dotnet/api/system.drawing.imaging.propertyitem.id?view=dotnet-plat-ext-6.0.
        /// </param>
        /// <param name="returnKind">DateTimeKind to return</param>
        /// <returns>
        /// Property value as a time of the specified kind.
        /// Returns null if the property does nt exist or is not a timestamp or cannot be comverted.
        /// </returns>
        public static DateTime? GetTimestampProperty(
            Bitmap bmp,
            int propertyId,
            DateTimeKind returnKind = DateTimeKind.Utc)
        {
            var propItem = bmp.GetPropertyItem(propertyId);
            if (propItem?.Value != null)
            {
                string timestampStr = Encoding.ASCII.GetString(propItem.Value, 0, propItem.Len - 1);
                if (DateTime.TryParseExact(timestampStr, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,
                                           System.Globalization.DateTimeStyles.AssumeLocal, out var dt))
                {
                    return DateTime.SpecifyKind(dt, returnKind);
                }
            }

            return null;
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
        public static DateTime GetRandomTimestamp(
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
    }
}
