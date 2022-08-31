using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using ExifLibrary;


namespace uk.andyjohnson.TakeoutExtractor.Lib.Photo
{
    public static class ExifLibraryExt
    {
        /// <summary>
        /// Convert to string to specified number of decimal places for the seconds vallue.
        /// </summary>
        /// <param name="self">GPSLatitudeLongitude object</param>
        /// <param name="secNumDecimals">Number of decimal places</param>
        /// <returns>String form</returns>
        public static string ToString(
            this GPSLatitudeLongitude self,
            uint secNumDecimals)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            var fmt = "{0:F2}°{1:F2}'{2:F" + secNumDecimals + "}\"";
            return string.Format(fmt, (float)self.Degrees, (float)self.Minutes, (float)self.Seconds);
        }


        /// <summary>
        /// Convert a UFraction32 to decimal
        /// </summary>
        /// <param name="self">MathEx.UFraction32 object</param>
        /// <returns>Decimal value</returns>
        public static decimal ToDecimal(this MathEx.UFraction32 self)
        {
            return (decimal)self.Numerator / (decimal)self.Denominator;
        }
    }
}
