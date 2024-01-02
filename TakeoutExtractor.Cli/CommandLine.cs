using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace uk.andyjohnson.TakeoutExtractor.Cli
{
    /// <summary>
    /// a simple command-line parser/handler.
    /// For simple command lines, instantiate an instance using the constructor and call the GetArgXxxx() functions.
    /// For composite command lines containing commands, call Create(string[], string[], string) to get a dictionary
    /// that maps commands to arguments.
    /// </summary>
    public class CommandLine
    {
        /// <summary>
        /// Constructore. Initailise a CommandLine object.
        /// </summary>
        /// <param name="args">Command-line argument tokens.</param>
        public CommandLine(string[] args)
        {
            this.Args = args;
        }

        /// <summary>
        /// Command-line argument tokens.
        /// </summary>
        public string[] Args { get; private set; }


        /// <summary>
        /// Split a command-line that contains commands into separate command-lines.
        /// </summary>
        /// <param name="args">Command-line argument tokens</param>
        /// <param name="commands">Command tokens</param>
        /// <param name="rootCommand">Pseudo-command name for oprions that are not bound to a command.</param>
        /// <returns>
        /// Dictionary of command name to CommandLine.
        /// Options that are not bound to a command (because they occur before the first command) are returned
        /// in the key/value pair who's key is equal to rootCommand.
        /// If the command-line is empty then the dictionary will be empty.
        /// If there are options but no commands then the dictionary will contain one element.
        /// Never returns null.
        /// </returns>
        public static Dictionary<string, CommandLine> Create(
            string[] args,
            string[] commands,
            string rootCommand = "")
        {
            var commandToArgs = new Dictionary<string, List<string>>();
            string currentCommand = rootCommand;

            foreach (var arg in args)
            {
                if (commands.Contains(arg))
                {
                    currentCommand = arg;
                    commandToArgs.Add(currentCommand, new List<string>());
                }
                else
                {
                    if (commandToArgs.TryGetValue(currentCommand, out var commandArgs))
                        commandArgs.Add(arg);
                    else
                        commandToArgs.Add(currentCommand, new List<string>() { arg });
                }
            }

            var d = new Dictionary<string, CommandLine>();
            foreach (var c in commandToArgs)
            {
                d.Add(c.Key, new CommandLine(c.Value.ToArray()));
            }
            return d;
        }


        /// <summary>
        /// Get the string value of the option identified by argName. This will be the token that follows -argName or /argName.
        /// </summary>
        /// <param name="argName">Argument name</param>
        /// <param name="argAltNames">Alternative/synonym names.</param>
        /// <param name="defaultValue">Default value to return if the argument is not found.</param>
        /// <param name="required">If true then a CommandLineException is thrown if the argument is not found.</param>
        /// <returns>Argument string value. This can be null if the argument is not found and the default is null.</returns>
        /// <exception cref="CommandLineException">Argument value missing or invalid</exception>
        public string? GetArgString(
            string argName,
            string[]? argAltNames = null,
            string? defaultValue = null,
            bool required = false)
        {
            for (int i = 0; i < (Args.Length - 1); i++)
            {
                if (IsMatch(Args[i], argName, argAltNames))
                {
                    if (!Args[i + 1].StartsWith("/") && !Args[i + 1].StartsWith("-"))
                        return Args[i + 1];
                    else
                        throw new CommandLineException($"No value found for argument '{argName}'");
                }
            }
            if (required)
                throw new CommandLineException($"Missing argument '{argName}'");
            else
                return defaultValue;
        }


        /// <summary>
        /// Get the boolean value of the option identified by argName. This will be the token that follows -argName or /argName.
        /// </summary>
        /// <param name="argName">Argument name</param>
        /// <param name="argAltNames">Alternative/synonym names.</param>
        /// <param name="defaultValue">Default value to return if the argument is not found.</param>
        /// <param name="required">If true then a CommandLineException is thrown if the argument is not found.</param>
        /// <returns>Argument boolean value.</returns>
        /// <exception cref="CommandLineException">Argument value missing or invalid</exception>
        public bool GetArgBool(
            string argName,
            string[]? argAltNames = null,
            bool defaultValue = false,
            bool required = false)
        {
            try
            {
                var argStr = GetArgString(argName, argAltNames: argAltNames, defaultValue: defaultValue.ToString(), required: required);
                return (argStr != null) ? bool.Parse(argStr) : defaultValue;
            }
            catch(FormatException ex)
            {
                throw new CommandLineException($"Invalid boolean value for argument '{argName}'", ex);
            }
        }


        /// <summary>
        /// Get the integer value of the option identified by argName. This will be the token that follows -argName or /argName.
        /// </summary>
        /// <param name="argName">Argument name</param>
        /// <param name="argAltNames">Alternative/synonym names.</param>
        /// <param name="defaultValue">Default value to return if the argument is not found.</param>
        /// <param name="required">If true then a CommandLineException is thrown if the argument is not found.</param>
        /// <returns>Argument integer value.</returns>
        /// <exception cref="CommandLineException">Argument value missing or invalid</exception>
        public int GetArgInt(
            string argName,
            string[]? argAltNames = null,
            int defaultValue = -1,
            bool required = false)
        {
            try
            {
                var argStr = GetArgString(argName, argAltNames: argAltNames, defaultValue: defaultValue.ToString(), required: required);
                return (argStr != null) ? int.Parse(argStr) : defaultValue;
            }
            catch (FormatException ex)
            {
                throw new CommandLineException($"Invalid integer value for argument '{argName}'", ex);
            }
        }


        /// <summary>
        /// Get whether the option identified by argName is present. This is used for valueless flags
        /// </summary>
        /// <param name="argName">Argument name</param>
        /// <param name="argAltNames">Alternative/synonym names.</param>
        /// <param name="defaultValue">Default value to return if the argument is not found.</param>
        /// <param name="required">If true then a CommandLineException is thrown if the argument is not found.</param>
        /// <returns>True if the argument is present, otherwise false.</returns>
        /// <exception cref="CommandLineException">Argument missing</exception>
        public bool GetArgFlag(
            string argName,
            string[]? argAltNames = null,
            bool defaultValue = false,
            bool required = false)
        {
            if (GetArgExists(argName))
                return true;
            else if (!required)
                return defaultValue;
            else
                throw new CommandLineException($"Missing argument '{argName}'");
        }


        /// <summary>
        /// Get the DirectoryInfo value of the option identified by argName. This will be the token that follows -argName or /argName.
        /// </summary>
        /// <param name="argName">Argument name</param>
        /// <param name="argAltNames">Alternative/synonym names.</param>
        /// <param name="defaultValue">Default value to return if the argument is not found.</param>
        /// <param name="required">If true then a CommandLineException is thrown if the argument is not found.</param>
        /// <returns>Argument DirectoryInfo value. The existence of the directory is not checked.</returns>
        /// <exception cref="CommandLineException">Argument value missing or invalid</exception>
        public DirectoryInfo? GetArgDir(
            string argName,
            string[]? argAltNames = null,
            DirectoryInfo? defaultValue = null,
            bool required = false)
        {
            var argStr = GetArgString(argName, argAltNames: argAltNames, defaultValue: defaultValue?.FullName, required: required);
            if (argStr != null)
            {
                if (!Path.IsPathRooted(argStr))
                    argStr = Path.GetFullPath(argStr);
                return new DirectoryInfo(argStr);
            }
            else
            {
                return defaultValue;
            }
        }


        /// <summary>
        /// Given a set of possible argument values, and an enumeration with corresponding values, return
        /// the enum value that corresponds to the argument value.
        /// </summary>
        /// <typeparam name="T">An enum type</typeparam>
        /// <param name="argName">Argument name</param>
        /// <param name="argValues">
        /// Possible argument values.
        /// Must match one to one with enum values.
        /// Specify null for enum elements that shuld not be returned - for example if the default enum value does not map to an argument value.
        /// </param>
        /// <param name="argAltNames">Alternative/synonym names.</param>
        /// <param name="defaultValue">Default value. By default this is the argument value that corresponds to the default value of the denum.</param>
        /// <param name="required">If true then a CommandLineException is thrown if the argument is not found.</param>
        /// <returns>Enumeration value corresponding to the argument value.</returns>
        /// <exception cref="CommandLineException">Argument value missing or invalid</exception>
        public T GetArgEnum<T>(
            string argName,
            string?[] argValues,
            string[]? argAltNames = null,
            T defaultValue = default(T)!,
            bool required = false) where T : System.Enum
        {
            // Check that the arg values array has the same number of elements as the enum has values.
            // This is required so that we can map from vaues to enums.
            var enumVals = (T[])Enum.GetValues(typeof(T));
            var enumNames = Enum.GetNames(typeof(T));
            if (argValues.Length != enumNames.Length)
            {
                throw new ArgumentException($"Argument values mismatch with {typeof(T).Name} - expected {enumNames.Length} but got {argValues.Length}");
            }

            // Get the element of argValues that corresponds to the default value. The element may be null.
            string? defaultArgValue = argValues[Array.IndexOf(enumVals, defaultValue)];

            // Get the actual argument value from the command line.
            var argValue = GetArgString(argName, argAltNames: argAltNames, defaultValue: defaultArgValue, required: required);

            // Find argValue in the array of argument values we were given . We will use the idex to return the corresponding enum value.
            var i = Array.IndexOf(argValues, argValue);
            if (i != -1)
            {
                // Found. Return the correspnding enum value.
                return enumVals[i];
            }
            else
            {
                // Not found. argValue is not in argValues so we can't map to the enum.
                var argValuesStr = "'" + string.Join("', '", argValues) + "'";
                throw new CommandLineException($"Invalid value '{argValue}' for  '{argName}'. Must be one of '{argValuesStr}.");
            }
        }



        #region Implementation

        private const string optChars = "/-";


        private bool GetArgExists(string argName)
        {
            foreach (string arg in Args)
            {
                if ((arg == ("/" + argName)) || (arg == ("-" + argName)))
                {
                    return true;
                }
            }
            return false;
        }


        private static bool IsMatch(
            string argVal,
            string argName,
            string[]? argAltNames)
        {
            foreach (var c in optChars.ToCharArray())
            {
                if ( (argVal == (c + argName)) ||
                     ((argAltNames != null) && argAltNames.Contains(c + argName)) )
                {
                    return true;
                }
            }
            return false;
        }

        #endregion Implementation
    }
}
