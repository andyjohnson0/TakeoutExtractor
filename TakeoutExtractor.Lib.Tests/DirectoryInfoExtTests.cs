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
        #region IsSubirOf

        [TestMethod, TestCategory("IsSubirOf")]
        public void ImmediateSubdir()
        {
            var d1 = new DirectoryInfo("/foo/bar");
            var d2 = new DirectoryInfo("/foo/bar/baz");

            Assert.IsTrue(d2.IsSubirOf(d1));
            Assert.IsFalse(d1.IsSubirOf(d2));
        }


        [TestMethod, TestCategory("IsSubirOf")]
        public void NonImmediateSubdir()
        {
            var d1 = new DirectoryInfo("/foo/");
            var d2 = new DirectoryInfo("/foo/bar/baz");

            Assert.IsTrue(d2.IsSubirOf(d1));
            Assert.IsFalse(d1.IsSubirOf(d2));
        }


        [TestMethod, TestCategory("IsSubirOf")]
        public void SameDirs()
        {
            var d1 = new DirectoryInfo("/foo/bar/baz");

            Assert.IsTrue(d1.IsSubirOf(d1));
        }


        [TestMethod, TestCategory("IsSubirOf")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Validation()
        {
            var d1 = new DirectoryInfo("/foo/");
            DirectoryInfo? d2 = null;

            d1.IsSubirOf(d2!);
        }

        #endregion IsSubirOf


        #region AppendSubdirectory

        [TestMethod, TestCategory("AppendSubdirectory")]
        public void SingleDir()
        {
            var d1 = new DirectoryInfo("/foo/");

            var d2 = d1.AppendSubdirectory("bar");
        }

        #endregion AppendSubdirectory
    }
}
