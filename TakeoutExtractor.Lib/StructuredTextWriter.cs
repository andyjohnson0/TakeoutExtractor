using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Xml;



namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Fascade over an XmlWriter or Utf8JsonWriter.
    /// Implements a subset of the Utf8JsonWriter interface to allow writing to either type of file.
    /// </summary>
    public class StructuredTextWriter
    {
        /// <summary>
        /// Constructor. Initailise a StructuredTextWriter.
        /// </summary>
        /// <param name="wtr">Utf8JsonWriter for underlying JSON document</param>
        /// <exception cref="ArgumentNullException">wtr argument is null</exception>
        public StructuredTextWriter(Utf8JsonWriter wtr)
        {
            if (wtr == null)
                throw new ArgumentNullException(nameof(wtr));

            this.jsonWtr = wtr;
            this.xmlWtr = null;
        }


        /// <summary>
        /// Constructor. Initailise a StructuredTextWriter.
        /// </summary>
        /// <param name="wtr">XmlWriter for underlying XML document</param>
        /// <exception cref="ArgumentNullException">wtr argument is null</exception>
        public StructuredTextWriter(XmlWriter wtr)
        {
            if (wtr == null)
                throw new ArgumentNullException(nameof(wtr));

            this.jsonWtr = null;
            this.xmlWtr = wtr;
        }


        private readonly Utf8JsonWriter? jsonWtr;
        private readonly XmlWriter? xmlWtr;

        private Stack<bool> inArray = new Stack<bool>();


        /// <summary>
        /// Write the start of document to underlying document.
        /// For a JSON document this does writes the start of a global scope.
        /// For an XML document this writes the XML declaration.
        /// </summary>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteStartDocumentAsync()
        {
            if (jsonWtr != null)
            {
                jsonWtr.WriteStartObject();
            }
            else  if (xmlWtr != null)
            {
                await xmlWtr.WriteStartDocumentAsync();
            }

            inArray.Push(false);
        }


        /// <summary>
        /// Write the end of document to the underlying document.
        /// For a JSON document this closes the global scope.
        /// For an XML document this closes any open elements.
        /// </summary>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteEndDocumentAsync()
        {
            if (jsonWtr != null)
            {
                jsonWtr.WriteEndObject();
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.WriteEndDocumentAsync();
            }

            if (inArray.Pop() != false)
            {
                throw new InvalidOperationException("WriteEndDocumentAsync() called in un-terminated array");
            }
            if (inArray.Count != 0)
            {
                throw new InvalidOperationException($"WriteEndDocumentAsync() called with {inArray.Count} un-terminated elements");
            }
        }



        /// <summary>
        /// Write the start of an object to the underlying document.
        /// For a JSON document this writes the start of an object with an optional property name.
        /// For an XML document this writes the start of an element and the name parameter must not be null.
        /// </summary>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Argument value is invalid</exception>
        /// <exception cref="ArgumentNullException">Argument must not be null</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteStartObjectAsync(string? name)
        {
            if (jsonWtr != null)
            {
                if (name != null && !inArray.Peek())
                    jsonWtr.WriteStartObject(propertyName: name);
                else
                    jsonWtr.WriteStartObject();
            }
            else if (xmlWtr != null)
            {
                if (name == null)
                    throw new ArgumentNullException("Parameter must not be null for xml", nameof(name));
                await xmlWtr.WriteStartElementAsync(prefix: null, localName: name, ns: null);
            }

            inArray.Push(false);
        }


        /// <summary>
        /// Write the end of an object to the underlying document.
        /// For a JSON document this writes the end of the current object,
        /// or a property without a name if the name parameter is null
        /// For an XML document this closes the current element.
        /// </summary>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteEndObjectAsync()
        {
            if (jsonWtr != null)
            {
                jsonWtr.WriteEndObject();
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.WriteEndElementAsync();
            }

            if (inArray.Pop() != false)
            {
                throw new InvalidOperationException("WriteEndObjectAsync() called in un-terminated array");
            }
        }


        /// <summary>
        /// Write the start of an array to the underlying document.
        /// For a JSON document this writes the start of an array with an optional property name.
        /// For an XML document this writes the start of an element and the name parameter must not be null.
        /// </summary>
        /// <param name="name">Name of the array</param>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Argument value is invalid</exception>
        /// <exception cref="ArgumentNullException">Argument must not be null</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteStartArrayAsync(string? name)
        {
            if (jsonWtr != null)
            {
                if (name != null)
                    jsonWtr.WriteStartArray(propertyName: name);
                else
                    jsonWtr.WriteStartArray();
            }
            else if (xmlWtr != null)
            {
                if (name == null)
                    throw new ArgumentNullException("Parameter must not be null for xml", nameof(name));
                await xmlWtr.WriteStartElementAsync(prefix: null, localName: name, ns: null);
            }

            inArray.Push(true);
        }


        /// <summary>
        /// Write the end of an array to the underlying document.
        /// For a JSON document this writes the end of an array.
        /// For an XML document this writes the end of an element.
        /// </summary>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteEndArrayAsync()
        {
            if (jsonWtr != null)
            {
                jsonWtr.WriteEndArray();
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.WriteEndElementAsync();
            }

            if (inArray.Pop() != true)
            {
                throw new InvalidOperationException("WriteEndObjectAsync() called outside of array");
            }
        }


        /// <summary>
        /// Write a string to the underlying document.
        /// For a JSON document this writes a string property as a name/value pair.
        /// For an XML document this writes an element with a string value.
        /// </summary>
        /// <param name="name">Property/element name</param>
        /// <param name="value">Property/element value</param>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Argument value is invalid</exception>
        /// <exception cref="ArgumentNullException">Argument must not be null</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteStringAsync(
            string name,
            string value)
        {
            if (jsonWtr != null)
            {
                if (inArray.Peek() == true)
                    jsonWtr.WriteStringValue(value: value);
                else
                    jsonWtr.WriteString(propertyName: name, value: value);
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.WriteElementStringAsync(prefix: null, localName: name, ns: null, value: value);
            }
         }


        /// <summary>
        /// Write a number to the underlying document.
        /// For a JSON document this writes a numeric property as a name/value pair.
        /// For an XML document this writes an element with a numeric value.
        /// </summary>
        /// <param name="name">Property/element name</param>
        /// <param name="value">Property/element value</param>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Argument value is invalid</exception>
        /// <exception cref="ArgumentNullException">Argument must not be null</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteNumberAsync(
            string name,
            int value)
        {
            if (jsonWtr != null)
            {
                if (inArray.Peek() == true)
                    jsonWtr.WriteNumberValue(value: value);
                else
                    jsonWtr.WriteNumber(propertyName: name, value: value);
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.WriteElementStringAsync(prefix: null, localName: name, ns: null, value: XmlConvert.ToString(value));
            }
        }


       /// <summary>
        /// Write a number to the underlying document.
        /// For a JSON document this writes a numeric property as a name/value pair.
        /// For an XML document this writes an element with a numeric value.
        /// </summary>
        /// <param name="name">Property/element name</param>
        /// <param name="value">Property/element value</param>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="ArgumentException">Argument value is invalid</exception>
        /// <exception cref="ArgumentNullException">Argument must not be null</exception>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        public async Task WriteNumberAsync(
            string name,
            decimal value)
        {
            if (jsonWtr != null)
            {
                if (inArray.Peek() == true)
                    jsonWtr.WriteNumberValue(value: value);
                else
                    jsonWtr.WriteNumber(propertyName: name, value: value);
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.WriteElementStringAsync(prefix: null, localName: name, ns: null, value: XmlConvert.ToString(value));
            }
        }


        /// <summary>
        /// Asynchronously commits all pending writes to the underlying document.
        /// </summary>
        /// <returns>The Task that represents the asynchronous operation</returns>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        /// <exception cref="ObjectDisposedException">The writer for the underlying document has been disposed</exception>
        public async Task FlushAsync()
        {
            if (jsonWtr != null)
            {
                await jsonWtr.FlushAsync();
            }
            else if (xmlWtr != null)
            {
                await xmlWtr.FlushAsync();
            }
        }


        /// <summary>
        /// Synchrounously commits all pending writes to the underlying document.
        /// </summary>
        /// <exception cref="InvalidOperationException">The operation is invalid for the current documet state</exception>
        /// <exception cref="ObjectDisposedException">The writer for the underlying document has been disposed</exception>
        public void Flush()
        {
            if (jsonWtr != null)
            {
                jsonWtr.Flush();
            }
            else if (xmlWtr != null)
            {
                xmlWtr.Flush();
            }
        }
    }
}
