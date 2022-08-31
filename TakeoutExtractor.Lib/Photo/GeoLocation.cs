using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Photo
{
    /// <summary>
    /// Represents a location specified by signed decimal degrees, and a signed altitude
    /// </summary>
    public record struct LatLonAltLocation(decimal latDeg, decimal lonDeg, decimal alt);

    /// <summary>
    /// Represents a location specified by signed degrees and unsigned minutes and seconds, and a signed altitude
    /// </summary>
    public record struct LatLonDmsAltLocation(decimal latDeg, decimal latMin, decimal latSec, decimal lonDeg, decimal lonMin, decimal lonSec, decimal alt);


    /// <summary>
    /// Utilities for geo-location
    /// </summary>
    public static class GeoLocation
    {
        /// <summary>
        /// Null location at 0° 0' 0'', 0° 0' 0'', 0M
        /// </summary>
        public static readonly LatLonAltLocation NullLatLonAltLocation = new LatLonAltLocation(0M, 0M, 0M);

        /// <summary>
        /// Null location at 0° 0' 0'', 0° 0' 0'', 0M
        /// </summary>
        public static readonly LatLonDmsAltLocation NullLatLonDmsAltLocation = new LatLonDmsAltLocation(0M, 0M, 0M, 0M, 0M, 0M, 0M);


        /// <summary>
        /// Convert a location in decimal degrees to degrees/minutes/deconds.
        /// </summary>
        /// <param name="location">Location to convert</param>
        /// <param name="numDecimals">Number of decimal places for the seconds part only.</param>
        /// <returns>Location in degrees/minutes/deconds</returns>
        /// <exception cref="ArgumentException">Invalid argument</exception>
        /// <exception cref="InvalidOperationException">Invalid calculation</exception>
        public static LatLonDmsAltLocation ToLatLonDmsAlt(
            LatLonAltLocation location,
            uint numDecimals = 3)
        {
            if (location.latDeg < -90M || location.latDeg > +90M)
                throw new ArgumentException($"Latitude out of range: {location.latDeg}", nameof(location.latDeg));
            if (location.lonDeg < -180M || location.lonDeg > +180M)
                throw new ArgumentException($"Longitude out of range: {location.lonDeg}", nameof(location.lonDeg));

            var latDms = ToLatLonDms(location.latDeg, numDecimals);
            var lonDms = ToLatLonDms(location.lonDeg, numDecimals);
            var latLonLoc = new LatLonDmsAltLocation(latDms.deg, latDms.min, latDms.sec, lonDms.deg, lonDms.min, lonDms.sec, location.alt);

            if (latLonLoc.latDeg != 0M && location.latDeg != 0M &&
                Math.Sign(latLonLoc.latDeg) != Math.Sign(location.latDeg))
            {
                throw new InvalidOperationException($"Sign mismatch for longitude decimal degrees {location.latDeg} and latitude degrees {latLonLoc.latDeg}");
            }
            if (latLonLoc.lonDeg != 0M && location.lonDeg != 0M &&
                Math.Sign(latLonLoc.lonDeg) != Math.Sign(location.lonDeg))
            {
                throw new InvalidOperationException($"Sign mismatch for longitude decimal degrees {location.lonDeg} and longitude degrees {latLonLoc.lonDeg}");
            }

            return latLonLoc;
        }


        private static (decimal deg, decimal min, decimal sec) ToLatLonDms(
            decimal decimalDegrees,
            uint numDecimals = 1)
        {
            (decimal deg, decimal min, decimal sec) dms;

            var t = Math.Abs(decimalDegrees);
            dms.deg = Math.Floor(t);
            t = (t - dms.deg) * 60M;
            dms.min = Math.Floor(t);
            t = (t - dms.min) * 60M;
            dms.sec = Math.Round(t, (int)numDecimals);

            dms.deg *= Math.Sign(decimalDegrees);

            return dms;
        }
    }
}
