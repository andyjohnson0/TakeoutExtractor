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
    }
}