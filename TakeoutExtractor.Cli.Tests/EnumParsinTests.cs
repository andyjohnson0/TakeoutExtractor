using Microsoft.VisualStudio.TestTools.UnitTesting;

using uk.andyjohnson.TakeoutExtractor.Cli;


namespace uk.andyjohnson.TakeoutExtractor.Cli.Tests
{
    public enum TestEnum
    {
        None = 0,
        Foo,
        Bar,
        Baz
    }


    [TestClass]
    public class EnumParsinTests
    {
        [TestMethod]
        public void ValueFound()
        {
            var cl = new CommandLine(new string[] { "-f", "foo" });
            var val = cl.GetArgEnum<TestEnum>("f", new string?[] { "none", "foo", "bar", "baz" });
            Assert.AreEqual(TestEnum.Foo, val);
        }


        [TestMethod]
        [ExpectedException(typeof(CommandLineException))]
        public void ValueNotFound()
        {
            var cl = new CommandLine(new string[] { "-f", "pig" });
            var val = cl.GetArgEnum<TestEnum>("f", new string?[] { "none", "foo", "bar", "baz" });
        }


        [TestMethod]
        [ExpectedException(typeof(CommandLineException))]
        public void ValueNotAllowed()
        {
            var cl = new CommandLine(new string[] { "-f", "none" });
            var val = cl.GetArgEnum<TestEnum>("f", new string?[] { null, "foo", "bar", "baz" });  // TestEnum.None is not allowed
        }
    }
}
