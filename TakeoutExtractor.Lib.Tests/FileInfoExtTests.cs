using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Tests
{
    [TestClass]
    public class FileInfoExtTests
    {
        private readonly FileInfo shortPathFi = new FileInfo("/foo/bar/filename1234567890.txt");
        private readonly FileInfo longPathFi = new FileInfo("/foo12345678/bar123456/baz1234/fn.txt");
        private readonly FileInfo longNameFi = new FileInfo("/foo1234/thisisaveryverylongfilename.txt");



        #region CompactName

        [TestMethod]
        public void CompactName_NoChange()
        {
            Assert.AreEqual(longPathFi.FullName, longPathFi.CompactName(longPathFi.FullName.Length + 1));
            Assert.AreEqual(longPathFi.FullName, longPathFi.CompactName(longPathFi.FullName.Length));
        }


        [TestMethod]
        public void CompactName_Shorten()
        {
            var parts = longPathFi.FullName.Split(Path.DirectorySeparatorChar);

            // Remove longest section
            Assert.AreEqual(longPathFi.FullName.Replace(parts[1], "..."), 
                            longPathFi.CompactName(longPathFi.FullName.Length - parts[1].Length + 3));

            // Remove two longest section
            Assert.AreEqual(longPathFi.FullName.Replace(parts[1], "...").Replace(parts[2], "..."), 
                            longPathFi.CompactName(longPathFi.FullName.Length - parts[1].Length + 3 - parts[2].Length + 3));
        }


        [TestMethod]
        public void CompactName_DontShortenFileName()
        {
            var path = longNameFi.CompactName(longNameFi.FullName.Length - 10);
            Assert.IsTrue(path.Length < longNameFi.FullName.Length);  // Check that it compacted something
            Assert.IsTrue(path.Contains(longNameFi.Name));
        }


        [TestMethod]
        public void CompactName_UnableToShorten1()
        {
            var fi = new FileInfo("/filename.txt");  // Can't shorten: "" is shorter than elipsis
            var path = fi.CompactName(1);
            Assert.AreEqual(fi.FullName, path);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CompactName_Validation1()
        {
            
            longPathFi.CompactName(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CompactName_Validation2()
        {

            longPathFi.CompactName(-1);
        }

        #endregion CompactName




        #region TrimName

        [TestMethod]
        public void TrimName_NoChange()
        {
            var fi2 = shortPathFi.TrimName(999);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(shortPathFi.FullName, fi2.FullName);
        }


        [TestMethod]
        public void TrimName_Changed()
        {
            var fi2 = shortPathFi.TrimName(10);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(new FileInfo("/foo/bar/filename12.txt").FullName, fi2.FullName);

            fi2 = shortPathFi.TrimName(1);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(new FileInfo("/foo/bar/f.txt").FullName, fi2.FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TrimName_ValidationZeroLength()
        {
            var fi2 = shortPathFi.TrimName(0);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TrimName_ValidationNegativeLength()
        {
            var fi2 = shortPathFi.TrimName(-1);
        }

        #endregion TrimName


        #region AppendToName

        [TestMethod]
        public void AppendToName_Append()
        {
            var fi2 = shortPathFi.AppendToName("_ABC");
            Assert.IsNotNull(fi2);
            Assert.AreEqual(new FileInfo("/foo/bar/filename1234567890_ABC.txt").FullName, fi2.FullName);
        }

        [TestMethod]
        public void AppendToName_AppendEmpty()
        {
            var fi2 = shortPathFi.AppendToName("");
            Assert.IsNotNull(fi2);
            Assert.AreEqual(shortPathFi.FullName, fi2.FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppendToName_AppendNull()
        {
            var fi2 = shortPathFi.AppendToName(null!);
        }

        #endregion AppendToName


        #region IsXxxxxFile

        [TestMethod]
        public void IsImageFile()
        {
            Assert.IsTrue(new FileInfo("/foo/myimage.jpg").IsImageFile());
            Assert.IsTrue(new FileInfo("/foo/myimage.jpeg").IsImageFile());
            Assert.IsTrue(new FileInfo("/foo/myimage.png").IsImageFile());
            Assert.IsTrue(new FileInfo("/foo/myimage.gif").IsImageFile());

            Assert.IsFalse(new FileInfo("/foo/mydoc.txt").IsImageFile());
            Assert.IsFalse(new FileInfo("/foo/myfile").IsImageFile());
        }

        [TestMethod]
        public void IsImageFileWithExif()
        {
            Assert.IsTrue(new FileInfo("/foo/myimage.jpg").IsImageFileWithExif());
            Assert.IsTrue(new FileInfo("/foo/myimage.jpeg").IsImageFileWithExif());
            Assert.IsTrue(new FileInfo("/foo/myimage.png").IsImageFileWithExif());
            Assert.IsFalse(new FileInfo("/foo/myimage.gif").IsImageFileWithExif());

            Assert.IsFalse(new FileInfo("/foo/mydoc.txt").IsImageFileWithExif());
            Assert.IsFalse(new FileInfo("/foo/myfile").IsImageFileWithExif());
        }

        [TestMethod]
        public void IsVideoFile()
        {
            Assert.IsTrue(new FileInfo("/foo/myvid.mp4").IsVideoFile());
            Assert.IsTrue(new FileInfo("/foo/myvid.mpeg4").IsVideoFile());
            Assert.IsFalse(new FileInfo("/foo/myvid.mov").IsVideoFile());

            Assert.IsFalse(new FileInfo("/foo/mydoc.txt").IsVideoFile());
            Assert.IsFalse(new FileInfo("/foo/myfile").IsVideoFile());
        }


        #endregion IsImageFile
    }
}
