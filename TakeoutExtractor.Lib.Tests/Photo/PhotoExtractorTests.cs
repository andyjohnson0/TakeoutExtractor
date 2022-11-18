using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;


using Microsoft.VisualStudio.TestTools.UnitTesting;
using uk.andyjohnson.TakeoutExtractor.Lib.Photo;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Tests.Photo
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [TestClass]
    public class PhotoExtractorTests
    {
        [TestMethod]
        public async Task SingleFile_WithEdit()
        {
            await DoExtractionTestAsync("file_1646604530458_6906359968518998018", System.Drawing.Imaging.ImageFormat.Jpeg, hasEdited: true);
        }


        [TestMethod]
        public async Task SingleFile_NoEdit()
        {
            await DoExtractionTestAsync("file_993673465464_690438953657018", System.Drawing.Imaging.ImageFormat.Jpeg, hasEdited: false);
        }


        [TestMethod]
        public async Task MultipleFiles_Edits()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64", System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_f67g4544445_kjsdf76sdfhg", System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_87e669feuf_efrhu333674rf", System.Drawing.Imaging.ImageFormat.Jpeg, true)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_NoEdits()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_f67g4544445_kjsdf76sdfhg", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_87e669feuf_efrhu333674rf", System.Drawing.Imaging.ImageFormat.Jpeg, false)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_MixedEdits()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_f67g4544445_kjsdf76sdfhg", System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_87e669feuf_efrhu333674rf", System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_87efff7j33sd_df6gddff7h",  System.Drawing.Imaging.ImageFormat.Jpeg, true)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_NoEdits_NameConflict()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64",    System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("file_467g456hg345_84rf555tt64(1)", System.Drawing.Imaging.ImageFormat.Jpeg, false)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_Edits_NameConflict()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("file_467g456hg345_84rf555tt64",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("file_467g456hg345_84rf555tt64(1)", System.Drawing.Imaging.ImageFormat.Jpeg, true)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_NoEdits_MaxLength()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("ThisIs47Characters01234567890123456789012345678",    System.Drawing.Imaging.ImageFormat.Jpeg, false),
                ("AndThisIsAlso47Characters0123456789012345678901",    System.Drawing.Imaging.ImageFormat.Jpeg, false)
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_Edits_MaxLength()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("ThisIs47Characters01234567890123456789012345678",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("AndThisIsAlso47Characters0123456789012345678901",    System.Drawing.Imaging.ImageFormat.Jpeg, true)
            };
            await DoExtractionTestAsync(data);
        }



        [TestMethod]
        public async Task MultipleFiles_NoEdits_MaxLengthPlus()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("ThisIsAVeryLongFileNameThatExceedsThe47CharacterLimit0123456789",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("AndThisIsAnEvenLongerFileNameThatExceedsThe47CharacterLimit01234567890123456789",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
            };
            await DoExtractionTestAsync(data);
        }


        [TestMethod]
        public async Task MultipleFiles_Edits_MaxLengthPlus()
        {
            var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
            {
                ("ThisIsAVeryLongFileNameThatExceedsThe47CharacterLimit0123456789",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
                ("AndThisIsAnEvenLongerFileNameThatExceedsThe47CharacterLimit01234567890123456789",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
            };
            await DoExtractionTestAsync(data);
        }


        //[TestMethod]
        //public async Task MultipleFiles_Edits_LongLength_NameConflict()
        //{
        //    var data = new (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[]
        //    {
        //        ("ThisIsAVeryLongFileNameThatExceedsThe47CharacterLimit0123456789",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
        //        //("ThisIsAVeryLongFileNameThatExceedsThe47CharacterLimit0123456789(1)", System.Drawing.Imaging.ImageFormat.Jpeg, true),
        //        ("AndThisIsAnEvenLongerFileNameThatExceedsThe47CharacterLimit01234567890123456789",    System.Drawing.Imaging.ImageFormat.Jpeg, true),
        //        //("AndThisIsAnEvenLongerFileNameThatExceedsThe47CharacterLimit01234567890123456789(1)", System.Drawing.Imaging.ImageFormat.Jpeg, true)
        //    };
        //    await DoExtractionTestAsync(data);
        //}




        #region Shared implementation

        private async static Task DoExtractionTestAsync(
            string imageBaseName,
            System.Drawing.Imaging.ImageFormat imageFormat,
            bool hasEdited)
        {
            var data = new (string, System.Drawing.Imaging.ImageFormat, bool)[] { (imageBaseName, imageFormat, hasEdited) };
            await DoExtractionTestAsync(data);
        }


        private async static Task DoExtractionTestAsync(
            (string imageBaseName, System.Drawing.Imaging.ImageFormat imageFormat, bool hasEdited)[] testData)
        {
            const int yearsPast = 2;
            const int daysPast = 5;

            var testDir = PhotoTestsHelper.CreateTempDir("TakeoutExtractor_");
            var inDir = testDir.CreateSubdirectory("input");
            var outDir = testDir.CreateSubdirectory("output");

            try
            {
                // Build the source files
                var sourceFiles = new List<(FileInfo originalFile, FileInfo? editedFile)>();
                var sourceFileTimestamps = new List<(DateTime creationTime, DateTime? modifiedTime)>();
                var sourceFileDescriptions = new List<string>();
                var sourceFileExifLocations = new List<LatLonAltLocation>();
                var sourceFileEditedLocations = new List<LatLonAltLocation?>();
                for (var i = 0; i < testData.Length; i++)
                {
                    // Build plausible metadata values for the sidecar.
                    var creationTime = PhotoTestsHelper.BuildRandomTimestamp(DateTime.UtcNow.AddDays(-daysPast).AddYears(-yearsPast),
                                                                             DateTime.UtcNow.AddDays(-daysPast), DateTimeKind.Utc);
                    DateTime? modifiedTime = testData[i].hasEdited
                                                ? PhotoTestsHelper.BuildRandomTimestamp(creationTime, creationTime.AddDays(daysPast - 1), DateTimeKind.Utc)
                                                : null;
                    var description = PhotoTestsHelper.BuildRandomDescription();
                    var exifLocation = PhotoTestsHelper.BuildRandomLocation();
                    LatLonAltLocation? editedLocation = testData[i].hasEdited ? PhotoTestsHelper.BuildRandomLocation() : null;

                    // Stash value for comparison later.
                    sourceFileTimestamps.Add((creationTime, modifiedTime));
                    sourceFileDescriptions.Add(description);
                    sourceFileExifLocations.Add(exifLocation);
                    sourceFileEditedLocations.Add(editedLocation);

                    // Create the image(s) plus the json sidecar.
                    sourceFiles.Add(PhotoTestsHelper.CreateImagePair(inDir, testData[i].imageBaseName, testData[i].imageFormat,
                                                                     creationTime, modifiedTime, description, exifLocation, editedLocation));
                }
                Assert.AreEqual(testData.Length, sourceFiles.Count);
                Assert.AreEqual(testData.Length, sourceFileTimestamps.Count);

                // Do the extraction.
                // We hook into the extractor's Progress event to record the input->output file mappings.
                var options = new PhotoOptions() { KeepOriginalsForEdited = true, UpdateExif = true, OutputFileNameTimeKind = DateTimeKind.Utc };
                var extractor = new PhotoExtractor(options, inDir, outDir, null);
                var fileMappings = new Dictionary<string, string>();
                extractor.Progress += (o, e) =>
                {
                    fileMappings.Add(e.SourceFile.FullName, e.DesinationFile.FullName);
                };
                var results = await extractor.ExtractAsync(CancellationToken.None);
                Assert.IsNotNull(results);

                // Check the outcome.

                // 1. Check that we have the correct number of input->output file mappings.
                var expectedFileCount = sourceFiles.Count(f => f.originalFile != null) + sourceFiles.Count(f => f.editedFile != null);
                Assert.AreEqual(expectedFileCount, fileMappings.Count);


                // 2. Iterate over the input files.
                //    - Check that the input file was extracted according to the evidence in the fileMappings dictionary.
                //    - Check that corresponding files contain the same image
                //    - Check that the timestamps are correct.
                //    - Check that the filename is correct.
                //    - Check that the location is correct.
                //    - If there is an edited file then the original will be in the original subdirectory (if specified) and will have a suffix.
                //    - If there is no edited file then the original will be in the main output directory and will have no suffix..
                for (var i = 0; i < testData.Length; i++)
                {
                    var inputOriginalFile = sourceFiles[i].originalFile;
                    Assert.IsNotNull(inputOriginalFile);
                    Assert.IsTrue(fileMappings.ContainsKey(inputOriginalFile.FullName));
                    var outputOriginalFile = new FileInfo(fileMappings[inputOriginalFile.FullName]);
                    Assert.IsNotNull(outputOriginalFile);
                    Assert.IsTrue(outputOriginalFile.Exists);

                    Assert.AreEqual(sourceFileTimestamps[i].creationTime.ToString("u"), outputOriginalFile.CreationTimeUtc.ToString("u"));
                    Assert.AreEqual(sourceFileTimestamps[i].modifiedTime.HasValue, sourceFiles[i].editedFile != null);
                    Assert.AreEqual(sourceFileTimestamps[i].creationTime.ToString("u"), outputOriginalFile.LastWriteTimeUtc.ToString("u"));

                    Assert.IsTrue(outputOriginalFile.Name.StartsWith(sourceFileTimestamps[i].creationTime.ToString(options.OutputFileNameFormat)));

                    FileInfo? outputEditedlFile = null;
                    var inputEditedFile = sourceFiles[i].editedFile;
                    if (inputEditedFile != null)
                    {
                        Assert.IsTrue(fileMappings.ContainsKey(inputEditedFile.FullName));
                        outputEditedlFile = new FileInfo(fileMappings[inputEditedFile.FullName]);
                        Assert.IsNotNull(outputEditedlFile);
                        Assert.IsTrue(outputEditedlFile.Exists);

                        Assert.AreEqual(sourceFileTimestamps[i].creationTime.ToString("u"), outputEditedlFile.CreationTimeUtc.ToString("u"));
                        Assert.IsTrue(sourceFileTimestamps[i].modifiedTime.HasValue);
                        Assert.AreEqual(sourceFileTimestamps[i].modifiedTime!.Value.ToString("u"), outputEditedlFile.LastWriteTimeUtc.ToString("u"));

                        Assert.IsTrue(outputEditedlFile.Name.StartsWith(sourceFileTimestamps[i].creationTime.ToString(options.OutputFileNameFormat)));
                    }

                    Assert.IsTrue(PhotoTestsHelper.ValidateExtractedImagePair(sourceFiles[i].originalFile, outputOriginalFile,
                                                                              sourceFiles[i].editedFile, outputEditedlFile,
                                                                              sourceFileDescriptions[i],
                                                                              sourceFileExifLocations[i],
                                                                              sourceFileEditedLocations[i]));
                }
            }
            finally
            {
                testDir.Delete(recursive: true);
            }
        }

        #endregion Shared implementation
    }
}
