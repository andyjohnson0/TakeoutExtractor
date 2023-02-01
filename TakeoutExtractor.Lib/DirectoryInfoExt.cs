using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Extension methods for DirectoryInfo
    /// </summary>
    public static class DirectoryInfoExt
    {
        /// <summary>
        /// Determines if a subdirectory of the supplied obbject.
        /// Also returns true if di and self refer to the same directory.
        /// Does not check whether directories exist.
        /// </summary>
        /// <param name="self">DirectoeyInfo object.</param>
        /// <param name="di">Potential parentdirectory.</param>
        /// <returns>True if self is subdirectory (or same as) di</returns>
        /// <exception cref="ArgumentNullException">Argument is null</exception>
        public static bool IsSubirOf(
            this DirectoryInfo self,
            DirectoryInfo di)
        {
            if (di == null)
                throw new ArgumentNullException(nameof(di));
            return self.FullName.StartsWith(di.FullName);
        }


        /// <summary>
        /// Append a subdirectory to the path of a DirectoryInfo object.
        /// Does not check whether directories exist.
        /// </summary>
        /// <param name="self">DirectoeyInfo object.</param>
        /// <param name="subdirName">Subdirectory name or names</param>
        /// <returns>A DirectoryInfo object referring to the subdirectory</returns>
        /// <exception cref="ArgumentNullException">Argument is null</exception>
        public static DirectoryInfo AppendSubdirectory(
            this DirectoryInfo self,
            string subdirName)
        {
            if (subdirName == null)
                throw new ArgumentNullException(nameof(subdirName));
            return new DirectoryInfo(Path.Combine(self.FullName, subdirName));
        }
    }
}
