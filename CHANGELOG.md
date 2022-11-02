#Change Log

##2022-11-02 v0.6.1

Core photo extraction:
- Improved file matching, including for long file names and files distinguished only by "(1)" suffixes. Added tests for these cases.
- Made core test driver more generic and now tests for output edited files
- Extract GPS location and altitude from sidecar and conditionally populate EXIF fields
- Added tests for location/altitude handling.
- Added additional tests for geolocation data.

GUI:
- Added check for existing files in putput directory.
- Allow alerts to be attached to exceptions.
- Handle exceptions as unrecoverable errors.
- Added details column to Alerts page
- Added View->Alerts menu option to allow access to last extraction results.
- Alerts page displays alerts breakdown count.
- Stop on error global setting defaults to false

Various refactorings, principally in the TakeoutExtractor.Lib project.

Migrated to Visual Studio 17.3.6 release version from preview version.

Reinstated Mac Catalyst platform target.


##2022-08-25 v0.5

Extractors return results and error/warning/info objects that are presented and logged. Option to stop on first error.

Various fixed and code cleanup.


##2022-08-12 v0.4

Added option for extracting photos into directory hierarchy based on creation date (year, year/month, year/month/day)

Added options to create a JSON log file describing what was extracted.

General code clean-up


##2022-08-06 v0.3

Initial, minimal installable GUI


##2022-07-27 v0.2

Initial release of cli tool only

