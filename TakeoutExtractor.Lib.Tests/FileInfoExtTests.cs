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
        private const string shortPath = @"C:\foo\bar\filename1234567890.txt";
        private const string longPath = @"C:\foo12345678\bar123456\baz1234\fn.txt";
        private const string longName = @"C:\foo1234\thisisaveryverylongfilename.txt";



        #region CompactName

        [TestMethod]
        public void CompactName_NoChange()
        {
            Assert.AreEqual(longPath, new FileInfo(longPath).CompactName(longPath.Length + 1));
            Assert.AreEqual(longPath, new FileInfo(longPath).CompactName(longPath.Length));
        }


        [TestMethod]
        public void CompactName_Shorten()
        {
            var parts = longPath.Split(Path.DirectorySeparatorChar);

            // Remove longest section
            Assert.AreEqual(longPath.Replace(parts[1], "..."), 
                            new FileInfo(longPath).CompactName(longPath.Length - parts[1].Length + 3));

            // Remove two longest section
            Assert.AreEqual(longPath.Replace(parts[1], "...").Replace(parts[2], "..."), 
                            new FileInfo(longPath).CompactName(longPath.Length - parts[1].Length + 3 - parts[2].Length + 3));
        }


        [TestMethod]
        public void CompactName_DontShortenFileName()
        {
            var fi = new FileInfo(longName);
            var path = fi.CompactName(fi.FullName.Length - 10);
            Assert.IsTrue(path.Length < fi.FullName.Length);  // Check that it compacted something
            Assert.IsTrue(path.Contains(fi.Name));
        }


        [TestMethod]
        public void CompactName_UnableToShorten1()
        {
            var fi = new FileInfo(@"D:\filename.txt");  // Can't shorten: "D:" is shorter than elipsis
            var path = fi.CompactName(1);
            Assert.AreEqual(fi.FullName, path);
        }



        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CompactName_Validation1()
        {
            
            new FileInfo(longPath).CompactName(0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CompactName_Validation2()
        {

            new FileInfo(longPath).CompactName(-1);
        }

        #endregion CompactName




        #region TrimName

        [TestMethod]
        public void TrimName_NoChange()
        {
            var fi1 = new FileInfo(shortPath);
            var fi2 = fi1.TrimName(999);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(fi1.FullName, fi2.FullName);
        }


        [TestMethod]
        public void TrimName_Changed()
        {
            var fi1 = new FileInfo(shortPath);

            var fi2 = fi1.TrimName(10);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(@"C:\foo\bar\filename12.txt", fi2.FullName);

            fi2 = fi1.TrimName(1);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(@"C:\foo\bar\f.txt", fi2.FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TrimName_ValidationZeroLength()
        {
            var fi1 = new FileInfo(shortPath);
            var fi2 = fi1.TrimName(0);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TrimName_ValidationNegativeLength()
        {
            var fi1 = new FileInfo(shortPath);
            var fi2 = fi1.TrimName(-1);
        }

        #endregion TrimName


        #region AppendToName

        [TestMethod]
        public void AppendToName_Append()
        {
            var fi1 = new FileInfo(shortPath);
            var fi2 = fi1.AppendToName("_ABC");
            Assert.IsNotNull(fi2);
            Assert.AreEqual(@"C:\foo\bar\filename1234567890_ABC.txt", fi2.FullName);
        }

        [TestMethod]
        public void AppendToName_AppendEmpty()
        {
            var fi1 = new FileInfo(shortPath);
            var fi2 = fi1.AppendToName("");
            Assert.IsNotNull(fi2);
            Assert.AreEqual(shortPath, fi2.FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppendToName_AppendNull()
        {
            var fi1 = new FileInfo(shortPath);
            var fi2 = fi1.AppendToName(null!);
        }

        #endregion AppendToName


        #region IsXxxxxFile

        [TestMethod]
        public void IsImageFile()
        {
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.jpg").IsImageFile());
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.jpeg").IsImageFile());
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.png").IsImageFile());
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.gif").IsImageFile());

            Assert.IsFalse(new FileInfo(@"C:\foo\mydoc.txt").IsImageFile());
            Assert.IsFalse(new FileInfo(@"C:\foo\myfile").IsImageFile());
        }

        [TestMethod]
        public void IsImageFileWithExif()
        {
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.jpg").IsImageFileWithExif());
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.jpeg").IsImageFileWithExif());
            Assert.IsTrue(new FileInfo(@"C:\foo\myimage.png").IsImageFileWithExif());
            Assert.IsFalse(new FileInfo(@"C:\foo\myimage.gif").IsImageFileWithExif());

            Assert.IsFalse(new FileInfo(@"C:\foo\mydoc.txt").IsImageFileWithExif());
            Assert.IsFalse(new FileInfo(@"C:\foo\myfile").IsImageFileWithExif());
        }

        [TestMethod]
        public void IsVideoFile()
        {
            Assert.IsTrue(new FileInfo(@"C:\foo\myvid.mp4").IsVideoFile());
            Assert.IsTrue(new FileInfo(@"C:\foo\myvid.mpeg4").IsVideoFile());
            Assert.IsFalse(new FileInfo(@"C:\foo\myvid.mov").IsVideoFile());

            Assert.IsFalse(new FileInfo(@"C:\foo\mydoc.txt").IsVideoFile());
            Assert.IsFalse(new FileInfo(@"C:\foo\myfile").IsVideoFile());
        }


        #endregion IsImageFile
    }
}
