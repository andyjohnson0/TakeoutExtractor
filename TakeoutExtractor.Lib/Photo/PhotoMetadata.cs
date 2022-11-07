using System.Text.Json;

namespace uk.andyjohnson.TakeoutExtractor.Lib.Photo
{
    /// <summary>
    /// Sidecar metadata.
    /// </summary>
    public class PhotoMetadata
    {
        /// <summary>
        /// Original filename. Required.
        /// </summary>
        public string title = default!;

        /// <summary>
        /// Used-provided description. Optional and can be null.
        /// </summary>
        public string? description;

        /// <summary>
        /// Time that the photo was taken. UTC, required. 
        /// </summary>
        public DateTime takenTime;

        /// <summary>
        /// Time that the file was created (on the phone? on google photos?). UTC, required.
        /// </summary>
        public DateTime creationTime;

        /// <summary>
        /// Time that the file/photo was last changed (including on google photos). UTC, can be null. 
        /// </summary>
        public DateTime? lastModifiedTime;

        /// <summary>
        /// Decimal lat/lon/altitude position as recorded by the device
        /// </summary>
        public LatLonAltLocation exifLocation;

        /// <summary>
        /// Decimal lat/lon/altitude position as entered on the website
        /// </summary>
        public LatLonAltLocation editedLocation;


        /// <summary>
        /// Given a photo sidecar file, return information about the corresponding media file and edited version.
        /// </summary>
        /// <param name="jsonSidecarFile">Path to the photo sidecar file.</param>
        /// <returns>
        /// If the json file is a photo sidecar then returns a PhotoMetadata object containing path to the correspondng
        /// media original and edited media files, the media creation time, and last modified time. These times are UTC.
        /// Media file paths can be null if the media files do not exist.
        /// Timestamps can be null if metadata was not available.
        /// If the json file is not a media sidecar then returns null.
        /// </returns>
        public async static Task<PhotoMetadata?> CreateFromSidecar(
            FileInfo jsonSidecarFile)
        {
            var md = new PhotoMetadata();

            using (var manifestStm = new FileStream(jsonSidecarFile.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var manifestDoc = await JsonDocument.ParseAsync(manifestStm, new JsonDocumentOptions() { AllowTrailingCommas = true }))
                {
                    if (!manifestDoc.RootElement.TryGetProperty("title", out JsonElement elem))
                    {
                        // Not a photo/video manifest file.
                        return null;
                    }

                    // Get title from json manifest. Original file will be the same as the title
                    // but the name part (excluding path and ext) will have a max length of 'maxFileNameLen' chars.
                    md.title = elem.GetString()!;

                    // Get the description. This is the optional human-readable description of the file's contents.
                    if (manifestDoc.RootElement.TryGetProperty("description", out elem))
                    {
                        md.description = elem.GetString();
                    }

                    // Get timestamps.
                    if (manifestDoc.RootElement.TryGetProperty("photoTakenTime", out elem) &&
                        elem.TryGetProperty("timestamp", out elem))
                    {
                        var ticksStr = long.Parse(elem.GetString()!);
                        md.takenTime = DateTime.UnixEpoch.AddSeconds(ticksStr);  // UTC
                    }
                    if (manifestDoc.RootElement.TryGetProperty("creationTime", out elem) &&
                        elem.TryGetProperty("timestamp", out elem))
                    {
                        var ticksStr = long.Parse(elem.GetString()!);
                        md.creationTime = DateTime.UnixEpoch.AddSeconds(ticksStr);  // UTC
                    }
                    if (manifestDoc.RootElement.TryGetProperty("photoLastModifiedTime", out elem) &&
                        elem.TryGetProperty("timestamp", out elem))
                    {
                        var ticksStr = long.Parse(elem.GetString()!);
                        md.lastModifiedTime = DateTime.UnixEpoch.AddSeconds(ticksStr);  // UTC
                    }

                    md.exifLocation = ExtractMetadataLocation(manifestDoc.RootElement, "geoDataExif");
                    md.editedLocation = ExtractMetadataLocation(manifestDoc.RootElement, "geoData");
                }
            }

            return md;
        }



        /// <summary>
        /// Extract location information from a photo sidecar.
        /// </summary>
        /// <param name="rootElement">JSON root element. This is the immediate parent of the location elements.</param>
        /// <param name="propertyName">JSON property name</param>
        /// <returns>
        /// A record representing the location data in the JSON property,
        /// If the property does not exist or is incorrectly formatted then (0, 0, 0) is returned.
        /// </returns>
        private static LatLonAltLocation ExtractMetadataLocation(
            JsonElement rootElement,
            string propertyName)
        {
            if (rootElement.TryGetProperty(propertyName, out var locationElem))
            {
                if (locationElem.TryGetProperty("latitude", out var latitudeElem) &&
                    locationElem.TryGetProperty("longitude", out var longitudeElem) &&
                    locationElem.TryGetProperty("altitude", out var altitudeElem))
                {
                    return new LatLonAltLocation(Math.Round(latitudeElem.GetDecimal(), 6, MidpointRounding.ToZero),
                                                 Math.Round(longitudeElem.GetDecimal(), 6, MidpointRounding.ToZero),
                                                 Math.Round(altitudeElem.GetDecimal(), 3, MidpointRounding.ToZero));
                }
            }
            return GeoLocation.NullLatLonAltLocation;
        }
    }
}
