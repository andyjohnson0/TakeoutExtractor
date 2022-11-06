using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Extension methods for the Exception class and derived classes
    /// </summary>
    public static class ExceptionExt
    {
        /// <summary>
        /// Add data to an exception.
        /// Returns the target Exception, so this can be used to add data when throwing an exception without
        /// requiring an interstitial variable. E.g. throw new InvalidOperationException("error").AddData("foo", bar)
        /// </summary>
        /// <param name="ex">The exception. Must not be null.</param>
        /// <param name="key">Key. Must not be null.</param>
        /// <param name="obj">Data. Must not be null.</param>
        /// <returns>The exception</returns>
        /// <exception cref="ArgumentNullException">Null parameter</exception>
        public static Exception AddData(
            this Exception ex,
            string key,
            object obj)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            if (!string.IsNullOrEmpty(key) && obj != null)
            {
                ex.Data.Add(key, obj);
            }
            return ex;
        }


        /// <summary>
        /// Convert an exceptions Data collection to an IDictionary<string, object?> collection for easier handling.</string>
        /// </summary>
        /// <param name="ex">The exception. Must not be null.</param>
        /// <returns>An IDictionary<string, object?> collection</returns>
        /// <exception cref="ArgumentNullException">Null parameter</exception>
        public static IDictionary<string, object?> DataDict(
            this Exception ex)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex));

            var keys = new string[ex.Data.Keys.Count];
            ex.Data.Keys.CopyTo(keys, 0);
            var values = new object?[ex.Data.Values.Count];
            ex.Data.Values.CopyTo(values, 0);

            var dataDict = new Dictionary<string, object?>();
            for (var i = 0; i < ex.Data.Count; i++)
            {
                dataDict.Add(keys[i], values[i]);
            }
            return dataDict;
        }
    }
}
