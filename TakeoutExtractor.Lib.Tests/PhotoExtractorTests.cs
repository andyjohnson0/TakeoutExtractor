using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


using Microsoft.VisualStudio.TestTools.UnitTesting;

using uk.andyjohnson.TakeoutExtractor.Lib;


namespace uk.andyjohnson.TakeoutExtractor.Lib.Tests
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [TestClass]
    public class PhotoExtractorTests
    {
        [TestMethod]
        public async Task SingleFileWithEdit()
        {
            await DoExtractionTestAsync("file_1646604530458_6906359968518998018.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, hasEdited: true);
        }


        [TestMethod]
        public async Task SingleFileNoEdit()
        {
            await DoExtractionTestAsync("file_993673465464_690438953657018.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, hasEdited: false);
        }


        [TestMethod]
        public async Task MultipleFilesWithEdits()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_f67g4544445_kjsdf76sdfhg.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_87e669feuf_efrhu333674rf.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, true)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFilesNoEdits()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_f67g4544445_kjsdf76sdfhg.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_87e669feuf_efrhu333674rf.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, false)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFilesMixedNoEdits()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_f67g4544445_kjsdf76sdfhg.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_87e669feuf_efrhu333674rf.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_87efff7j33sd_df6gddff7h.jpg", System.Drawing.Imaging.ImageFormat.Jpeg, true)
            };
            await DoExtractionTestAsync(data);
        }



        #region Shared implementation

        public static async Task DoExtractionTestAsync(
            string imageBaseName,
            System.Drawing.Imaging.ImageFormat imageFormat,
            bool hasEdited)
        {
            var data = new (string, System.Drawing.Imaging.ImageFormat, bool)[] { (imageBaseName, imageFormat, hasEdited) };
            await DoExtractionTestAsync(data);
        }





        public static async Task DoExtractionTestAsync(
            (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[] testData)
        {
            const int yearsPast = 2;
            const int daysPast = 5;

            var testDir = PhotoTestsHelper.CreateTempDir("TakeoutExtractor_");
            var inDir = testDir.CreateSubdirectory("input");
            var outDir = testDir.CreateSubdirectory("output");
            var timestamps = new List<(DateTime creationTime, DateTime? modifiedTime)>();

            try
            {
                for(var i = 0; i < testData.Length; i++)
                {
                    var creationTime = PhotoTestsHelper.GetRandomTimestamp(
                        DateTime.UtcNow.AddDays(-daysPast).AddYears(-yearsPast),
                        DateTime.UtcNow.AddDays(-daysPast),
                        DateTimeKind.Utc);
                    DateTime? modifiedTime = null;
                    if (testData[i].hasEdited)
                        modifiedTime = PhotoTestsHelper.GetRandomTimestamp(
                            creationTime,
                            creationTime.AddDays(daysPast - 1),
                            DateTimeKind.Utc);

                    PhotoTestsHelper.CreateImageSet(inDir, testData[i].imageBaseName, testData[i].imageFormat, creationTime, modifiedTime);
                    timestamps.Add((creationTime, modifiedTime));
                }

                var options = new PhotoOptions() { KeepOriginalsForEdited = true, UpdateExif = true };
                var photoExtractor = new PhotoExtractor(options, inDir, outDir, null);
                var results = await photoExtractor.ExtractAsync(CancellationToken.None);
                Assert.IsNotNull(results);

                for (var i = 0; i < testData.Length; i++)
                {
                    var ext = Path.GetExtension(testData[i].imageBaseName);
                    Assert.IsTrue(ext.StartsWith("."));

                    // Check for existence of the required original file.
                    // If there is an edited file then the original will be in the original subdirectory (if specified) and will have a suffix.
                    // If there is no edited file then the original will be in the main output directory and will have no suffix..
                    var dn = options.OriginalsSubdirName != null ? options.OriginalsSubdirName : ""; 
                    dn = Path.Combine(outDir.ToString(), "Photos", testData[i].hasEdited ? dn : "");
                    var fn = timestamps[i].creationTime.ToString(options.OutputFileNameFormat) +
                             (testData[i].hasEdited ? options.OriginalsSuffix : "") +
                             ext;
                    var fi = new FileInfo(Path.Combine(dn, fn));
                    Assert.IsTrue(fi.Exists);
                    Assert.AreEqual(timestamps[i].creationTime.ToString("u"), fi.CreationTimeUtc.ToString("u"));
                    Assert.AreEqual(timestamps[i].creationTime.ToString("u"), fi.LastWriteTimeUtc.ToString("u"));
                    using (var bmp = new System.Drawing.Bitmap(fi.FullName))
                    {
                        var t = PhotoTestsHelper.GetTimestampProperty(bmp, PhotoTestsHelper.PropertyTagOriginalDateTime);
                        Assert.IsNotNull(t);
                        Assert.AreEqual(timestamps[i].creationTime.ToString("u"), t!.Value.ToString("u"));
                        t = PhotoTestsHelper.GetTimestampProperty(bmp, PhotoTestsHelper.PropertyTagDateTime);
                        Assert.IsNotNull(t);
                        Assert.AreEqual(timestamps[i].creationTime.ToString("u"), t!.Value.ToString("u"));
                    }
                    Assert.IsTrue(PhotoTestsHelper.CompareImageFiles(new FileInfo(Path.Combine(inDir.FullName, testData[i].imageBaseName)), fi));

                    // Check for existence of the optional edited file.
                    // This will be in the main output directory.
                    dn = Path.Combine(outDir.ToString(), "Photos");
                    fn = timestamps[i].creationTime.ToString(options.OutputFileNameFormat) + ext;
                    fi = new FileInfo(Path.Combine(dn, fn));
                    if (testData[i].hasEdited)
                    {
                        Assert.IsTrue(fi.Exists);
                        Assert.AreEqual(timestamps[i].creationTime.ToString("u"), fi.CreationTimeUtc.ToString("u"));
                        Assert.IsNotNull(timestamps[i].modifiedTime);
                        Assert.AreEqual(timestamps[i].modifiedTime!.Value.ToString("u"), fi.LastWriteTimeUtc.ToString("u"));
                        using (var bmp = new System.Drawing.Bitmap(fi.FullName))
                        {
                            var t = PhotoTestsHelper.GetTimestampProperty(bmp, PhotoTestsHelper.PropertyTagOriginalDateTime);
                            Assert.IsNotNull(t);
                            Assert.AreEqual(timestamps[i].creationTime.ToString("u"), t!.Value.ToString("u"));
                            t = PhotoTestsHelper.GetTimestampProperty(bmp, PhotoTestsHelper.PropertyTagDateTime);
                            Assert.IsNotNull(t);
                            Assert.AreEqual(timestamps[i].modifiedTime!.Value.ToString("u"), t!.Value.ToString("u"));
                        }
                        // TODO: Comparen edited file.
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                testDir.Delete(recursive: true);
            }
        }

        #endregion Shared implementation
    }
}
