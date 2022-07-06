using System;
using System.Linq;
using System.IO;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Extension methods for FileInfo
    /// </summary>
    public static class FileInfoExt
    {
        /// <summary>
        /// Trim the name part of a FileInfo object, reatining the directory and extension parts.
        /// </summary>
        /// <param name="fi">FileInfo object</param>
        /// <param name="maxNameLen">Maximum name length</param>
        /// <returns>Modified FileInfo object.</returns>
        /// <exception cref="ArgumentException">Invalid argument</exception>
        /// <exception cref="ArgumentNullException">Invalid argument</exception>
        public static FileInfo TrimName(
            this FileInfo fi,
            int maxNameLen)
        {
            if (fi == null)
                throw new ArgumentNullException(nameof(fi));
            if (maxNameLen <= 0)
                throw new ArgumentException(nameof(maxNameLen));

            var fn = Path.GetFileNameWithoutExtension(fi.Name);
            if (fn.Length > maxNameLen)
            {
                fn = fn.Substring(0, maxNameLen) + Path.GetExtension(fi.Name);
                return new FileInfo(Path.Combine(fi.DirectoryName!, fn));
            }
            else
            {
                return fi;
            }
        }


        /// <summary>
        /// Append a string to the name part of a FileInfo object, reatining the directory and extension parts.
        /// </summary>
        /// <param name="fi">FileInfo object</param>
        /// <param name="str">String to append</param>
        /// <returns>Modified FileInfo object.</returns>
        /// <exception cref="ArgumentNullException">Invalid argument</exception>
        public static FileInfo AppendToName(
            this FileInfo fi,
            string str)
        {
            if (fi == null)
                throw new ArgumentNullException(nameof(fi));
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            var fn = Path.GetFileNameWithoutExtension(fi.Name) + str + Path.GetExtension(fi.Name);
            return new FileInfo(Path.Combine(fi.DirectoryName!, fn));
        }


        private static (string ext, bool hasExif)[] imageExts = new (string, bool)[] { (".jpg", true), (".jpeg", true), (".png", true), (".gif", false) };

        /// <summary>
        /// Determine if the FileIInfo object refers to an image/picture file format.
        /// </summary>
        /// <param name="fi">FileInfo object.</param>
        /// <returns>True if an image file, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Invalid argument</exception>
        public static bool IsImageFile(
            this FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException(nameof(fi));

            var t = imageExts.FirstOrDefault(x => x.ext == fi.Extension.ToLower());
            return t.ext != null;
        }


        /// <summary>
        /// Determine if FileIInfo object refers to an image/picture file format that has embedded exif metadata.
        /// </summary>
        /// <param name="fi">FileInfo object.</param>
        /// <returns>True if an image file, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Invalid argument</exception>
        public static bool IsImageFileWithExif(
            this FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException(nameof(fi));

            var t = imageExts.FirstOrDefault(x => x.ext == fi.Extension.ToLower());
            return (t.ext != null) && t.hasExif;
        }



        private static string[] videoExts = new string[] { ".mp4", ".mpeg4" };

        /// <summary>
        /// Determine if the FileInfo object refers to a video file.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns>True if a video fies, otherwise false</returns>
        /// <exception cref="ArgumentNullException">Invalid argument</exception>
        public static bool IsVideoFile(
            this FileInfo fi)
        {
            if (fi == null)
                throw new ArgumentNullException(nameof(fi));

            return videoExts.Any(x => x == fi.Extension.ToLower());
        }
    }
}
