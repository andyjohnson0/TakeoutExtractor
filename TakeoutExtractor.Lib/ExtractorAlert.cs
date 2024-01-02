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
        /// Full (muli-line formatted) description of the event.
        /// </summary>
        public string FullDescription
        {
            get
            {
                var sb = new StringBuilder();
                using (var wtr = new StringWriter(sb))
                {
                    this.Write(wtr);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Does the alert have a full description?
        /// Used for control visibility binding.
        /// </summary>
        public bool HasFullDescription
        {
            get { return !string.IsNullOrEmpty(this.FullDescription); }
        }

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
            if (AssociatedException != null)
                wtr.WriteLine($"Error: {AssociatedException.Message}");
        }


        /// <summary>
        /// Write the alert to a structured text stream
        /// </summary>
        /// <param name="wtr">JSON stream writer</param>
        public async Task WriteAsync(StructuredTextWriter wtr)
        {
            await wtr.WriteStartObjectAsync("Alert");
            await wtr.WriteStringAsync("Time", Timestamp.ToString("u"));
            await wtr.WriteStringAsync("Type", Type.ToString());
            await wtr.WriteStringAsync("Description", Description);
            if (AssociatedFile != null)
                await wtr.WriteStringAsync("File", AssociatedFile.FullName);
            if (AssociatedDirectory != null)
                await wtr.WriteStringAsync("Directory", AssociatedDirectory.FullName);
            if (AssociatedObject != null)
            {
                await wtr.WriteStartArrayAsync("Info");
                var e = AssociatedObject as System.Collections.IEnumerable;
                if (e == null)
                    e = new object[] { AssociatedObject };
                foreach (var o in e)
                {
                    await wtr.WriteStringAsync("Value", o.ToString()!);
                }
                await wtr.WriteEndArrayAsync();
            }
            await wtr.WriteEndObjectAsync();
        }
    }
}
