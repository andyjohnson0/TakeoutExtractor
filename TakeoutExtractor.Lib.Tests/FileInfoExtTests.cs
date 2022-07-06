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
        private const string fn = @"C:\foo\bar\filename1234567890.txt";

        #region TrimName

        [TestMethod]
        public void TrimName_NoChange()
        {
            var fi1 = new FileInfo(fn);
            var fi2 = fi1.TrimName(999);
            Assert.IsNotNull(fi2);
            Assert.AreEqual(fi1.FullName, fi2.FullName);
        }


        [TestMethod]
        public void TrimName_Changed()
        {
            var fi1 = new FileInfo(fn);

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
            var fi1 = new FileInfo(fn);
            var fi2 = fi1.TrimName(0);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TrimName_ValidationNegativeLength()
        {
            var fi1 = new FileInfo(fn);
            var fi2 = fi1.TrimName(-1);
        }

        #endregion TrimName


        #region AppendToName

        [TestMethod]
        public void AppendToName_Append()
        {
            var fi1 = new FileInfo(fn);
            var fi2 = fi1.AppendToName("_ABC");
            Assert.IsNotNull(fi2);
            Assert.AreEqual(@"C:\foo\bar\filename1234567890_ABC.txt", fi2.FullName);
        }

        [TestMethod]
        public void AppendToName_AppendEmpty()
        {
            var fi1 = new FileInfo(fn);
            var fi2 = fi1.AppendToName("");
            Assert.IsNotNull(fi2);
            Assert.AreEqual(fn, fi2.FullName);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AppendToName_AppendNull()
        {
            var fi1 = new FileInfo(fn);
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
