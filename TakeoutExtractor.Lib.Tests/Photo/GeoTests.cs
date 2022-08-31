using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Tests.Photo
{
    [TestClass]
    public class GeoTests
    {
        // Decimal degrees extracted from OpenStreetMap at https://www.openstreetmap.org
        // Converted to deg/min/sec using converter at https://www.pgc.umn.edu/apps/convert/

        [TestMethod]
        public void DecimalDegreesToLatLon1()
        {
            // Manchester UK. North of equator, West of meridian
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(new LatLonAltLocation(53.47830M, -2.24958M, 50M), 3),
                            new LatLonDmsAltLocation(53M, 28M, 41.880M, -2M, 14M, 58.488M, 50M));

            // Modena, Italy. North of equator, East of meridian
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(new LatLonAltLocation(44.64632M, 10.92550M, 60M), 3),
                            new LatLonDmsAltLocation(44M, 38M, 46.752M, 10M, 55M, 31.8M, 60M));

            // Napier, New Zealand. South of equator, East of meridian
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(new LatLonAltLocation(-39.4790M, 176.9173M, 70M), 3),
                            new LatLonDmsAltLocation(-39M, 28M, 44.4M, 176M, 55M, 2.28M, 70M));

            // Santiago, Peru. South of equator, West of meridian
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(new LatLonAltLocation(-33.43697M, -70.63445M, 80M), 3),
                            new LatLonDmsAltLocation(-33M, 26M, 13.092M, -70M, 38M, 4.02M, 80M));

            // Null island
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(GeoLocation.NullLatLonAltLocation, 3),
                            GeoLocation.NullLatLonDmsAltLocation);
        }

        [TestMethod]
        public void DecimalDegreesToLatLon2()
        {
            // Small negative decimal latitude gives zero degrees: test aign check in conversion routine.
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(new LatLonAltLocation(-0.880395M, 91.684533M, 50M), 3),
                            new LatLonDmsAltLocation(0M, 52M, 49.422M, 91M, 41M, 4.319M, 50));

            // Small negative decimal longitudeitude gives zero degrees: test aign check in conversion routine.
            Assert.AreEqual(GeoLocation.ToLatLonDmsAlt(new LatLonAltLocation(87.469077M, -0.849447M, 50M), 3),
                            new LatLonDmsAltLocation(87M, 28M, 8.677M, 0M, 50M, 58.009M, 50));

        }
    }
}
