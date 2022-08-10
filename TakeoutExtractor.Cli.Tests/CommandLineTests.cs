using Microsoft.VisualStudio.TestTools.UnitTesting;

using uk.andyjohnson.TakeoutExtractor.Cli;


namespace uk.andyjohnson.TakeoutExtractor.Cli.Tests
{
    [TestClass]
    public class CommandLineTests
    {
        [TestMethod]
        public void EmptyCommand()
        {
            var args = new string[0];
            var cl = new CommandLine(args);
            Assert.AreEqual(0, cl.Args.Length);
        }


        [TestMethod]
        public void GlobalOptions1()
        {
            var args = new string[] { "-i", "input.dat", "-o", "output.dat" };
            var cl = new CommandLine(args);
            Assert.AreEqual(4, cl.Args.Length);
            Assert.AreEqual("input.dat", cl.GetArgString("i"));
            Assert.AreEqual("output.dat", cl.GetArgString("o"));
            Assert.AreEqual(null, cl.GetArgString("x"));
        }


        [TestMethod]
        public void GlobalOptions2()
        {
            var args = new string[] { "-i", "input.dat", "-o", "output.dat" };
            var commands = new string[0];
            var parse = CommandLine.Create(args,commands);
            Assert.IsNotNull(parse);
            Assert.AreEqual(1, parse.Count);
            var cl = parse[""];  // Get root commands
            Assert.AreEqual(4, cl.Args.Length);
            Assert.AreEqual("input.dat", cl.GetArgString("i"));
            Assert.AreEqual("output.dat", cl.GetArgString("o"));
            Assert.AreEqual(null, cl.GetArgString("x"));
        }


        [TestMethod]
        public void GlobalOptionsAndCommandWithOptions()
        {
            var args = new string[] { "-i", "input.dat", "-o", "output.dat", "photos", "-os", "original", "-od", "originals" };
            var commands = new string[] { "photos" };
            var parse = CommandLine.Create(args, commands);
            Assert.IsNotNull(parse);
            Assert.AreEqual(2, parse.Count);
            var cl = parse[""];   // get root commands
            Assert.AreEqual(4, cl.Args.Length);
            cl = parse["photos"];
            Assert.AreEqual(4, cl.Args.Length);
        }


        #region enum mapping

        private enum E
        {
            None = 0,
            Foo = 1,
            Bar = 2,
            Baz = 3,
            Quux = 4
        }

        private readonly string?[] V = new string?[] { null, "foo", "bar", "bax", "quux" };

        [TestMethod]
        public void EnumParseMatch()
        {
            var cl = new CommandLine(new string[] { "-p", "bar" });
            Assert.IsNotNull(cl);
            var e = cl.GetArgEnum<E>("p", V);
            Assert.AreEqual(E.Bar, e);
        }


        [TestMethod]
        [ExpectedException(typeof(CommandLineException))]
        public void EnumParseNoMatch()
        {
            var cl = new CommandLine(new string[] { "-p", "baz" });  // "baz" does not match any of the values in V
            Assert.IsNotNull(cl);
            var e = cl.GetArgEnum<E>("p", V);
        }


        [TestMethod]
        public void EnumParseDefaultOptional()
        {
            var cl = new CommandLine(new string[] { "-t" });
            Assert.IsNotNull(cl);
            var e = cl.GetArgEnum<E>("t", V, required: false);  // not required, so return the value (nope) that maps to the default enum value (None / 0)
            Assert.AreEqual(E.None, e);
        }


        [TestMethod]
        [ExpectedException(typeof(CommandLineException))]
        public void EnumParseDefaultRequired()
        {
            var cl = new CommandLine(new string[] { "-t" });
            Assert.IsNotNull(cl);
            var e = cl.GetArgEnum<E>("t", V, required: true);  // required overrrides default, so throws an excpetion
        }

        #endregion enum mapping
    }
}