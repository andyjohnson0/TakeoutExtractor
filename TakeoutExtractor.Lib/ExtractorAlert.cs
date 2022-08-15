using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace uk.andyjohnson.TakeoutExtractor.Lib
{
    /// <summary>
    /// Indicates the type of an ExtractorAlert
    /// </summary>
    public enum ExtractorAlertType
    {
        /// <summary>An error event</summary>
        Error = 0,
        /// <summary>A warning event</summary>
        Warning,
        /// <summary>An informative event</summary>
        Information
    }


    /// <summary>
    /// An event that occurred during an extraction process
    /// </summary>
    public class ExtractorAlert
    {
        /// <summary>
        /// Constructor. Iitialisean ExtractorAlert object.
        /// </summary>
        /// <param name="type">Alert type</param>
        /// <param name="description">Alert description</param>
        public ExtractorAlert(
            ExtractorAlertType type,
            string description)
        {
            this.Timestamp = DateTime.UtcNow;
            this.Type = type;
            this.Description = description;
        }

        /// <summary>
        /// The time that the event occurred (UTC)
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Event type: error, warning, etc
        /// </summary>
        public ExtractorAlertType Type { get; private set; }

        /// <summary>
        /// Description of the event
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Optional file associated with the event.
        /// </summary>
        public FileInfo? AssociatedFile { get; set; }

        /// <summary>
        /// Optional directory associated with the event.
        /// </summary>
        public DirectoryInfo? AssociatedDirectory { get; set; }

        /// <summary>
        /// Optional exception associated with the event.
        /// </summary>
        public Exception? AssociatedException { get; set; }

        /// <summary>
        /// Optional object associated with the event.
        /// </summary>
        public object? AssociatedObject { get; set; }


        /// <summary>
        /// Write the alert to a text stream
        /// </summary>
        /// <param name="wtr">Text writer</param>
        public void Write(TextWriter wtr)
        {
            wtr.WriteLine($"{Timestamp.ToString("u")} {Type} {Description}");
            if (AssociatedFile != null)
                wtr.WriteLine($"File: {AssociatedFile.FullName}");
            if (AssociatedDirectory != null)
                wtr.WriteLine($"Directory: {AssociatedDirectory.FullName}");
            if (AssociatedObject != null)
            {
                var e = AssociatedObject as System.Collections.IEnumerable;
                if (e == null)
                    e = new object[] { AssociatedObject };
                foreach (var o in e)
                {
                    wtr.WriteLine($"Info: {o}");
                }
            }
        }


        /// <summary>
        /// Write the alert to a ISON stream
        /// </summary>
        /// <param name="wtr">JSON stream writer</param>
        public void Write(Utf8JsonWriter wtr)
        {
            wtr.WriteStartObject();
            wtr.WriteString("Time", Timestamp.ToString("u")); 
            wtr.WriteString("Type", Type.ToString());
            wtr.WriteString("Description", Description);
            if (AssociatedFile != null)
                wtr.WriteString("File", AssociatedFile.FullName);
            if (AssociatedDirectory != null)
                wtr.WriteString("Directory", AssociatedDirectory.FullName);
            if (AssociatedObject != null)
            {
                wtr.WritePropertyName("Object");
                wtr.WriteStartObject();
                var e = AssociatedObject as System.Collections.IEnumerable;
                if (e == null)
                    e = new object[] { AssociatedObject };
                foreach (var o in e)
                {
                    wtr.WriteStartArray("Values");
                    wtr.WriteStringValue(o.ToString());
                    wtr.WriteEndArray();
                }
                wtr.WriteEndObject();
            }
            wtr.WriteEndObject();
        }
    }
}
