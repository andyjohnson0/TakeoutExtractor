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
    public class DirectoryInfoExtTests
    {
        [TestMethod]
        public void ImmediateSubdir()
        {
            var d1 = new DirectoryInfo(@"C:\foo\bar");
            var d2 = new DirectoryInfo(@"C:\foo\bar\baz");

            Assert.IsTrue(d2.IsSubirOf(d1));
            Assert.IsFalse(d1.IsSubirOf(d2));
        }


        [TestMethod]
        public void NonImmediateSubdir()
        {
            var d1 = new DirectoryInfo(@"C:\foo\");
            var d2 = new DirectoryInfo(@"C:\foo\bar\baz");

            Assert.IsTrue(d2.IsSubirOf(d1));
            Assert.IsFalse(d1.IsSubirOf(d2));
        }


        [TestMethod]
        public void SameDirs()
        {
            var d1 = new DirectoryInfo(@"C:\foo\bar\baz");

            Assert.IsTrue(d1.IsSubirOf(d1));
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Validation()
        {
            var d1 = new DirectoryInfo(@"C:\foo\");
            DirectoryInfo? d2 = null;

            d1.IsSubirOf(d2!);
        }
    }
}
