#Change Log

##2024-01-08 v1.1

Fixes:

- Issue #16: Trailing command is lost.

- Issue #17: Array error accessing manifest title element.

- Issue #18: Windows GUI is not longer easy installable due to expired publishing certificate. Certificate now expires in 2034.

- Issue #19: Use sidecar photoTakenTime element for file and directory naming.

- Issue #20: Ignore json files that are not image sidecars. metadata.json, print-subscriptions.json, shared_album_comments.json,
  user-generated-memory-titles.json, etc. are ignored.

- Issue #20: Deleted photos/videos are extracted.

Enhancements:

- Migrated to .net 7 and refreshed gui project structure

- Added option to ignore (default) or extract deleted photos files in the Bin folder.

- Improved error detail reporting. Display alert details link only if alert has details to display.

- Replaced GUI app icon and splash overlay.

- Added additional readme content and a screenshot.

- Include PubXml files for this solution since they contain no secrets

Note: This is a source-only release as publishing the gui app on Windows is broken for unknown reasons. See issue #22.



##2023-02-01 v1.0

v1.0 release!

Enhancements:
- Implemented six different photo version handling strategies instead of the previous single fixed approach.
As part of this, removed configurable handling of directory name and file suffix for original photos.

Fit and finish:
- Issue #15: Prevent splash screen from being displayed when navigating back to main page from alerts page
- Made alerts page scrollable. Uses theme-dependant link colour for readability.
- Prompt to create output directory if it doesn't exist


##2023-01-18 v0.9

Multi-platform release: now includes a Mac Catalyst version.

Principal changes to support multiple platforms:
- Directory and file enumeration ordered specifically by name to ensure consistent results across platforms.
- Implemented DisplayAlert() for Mac platform.
- Removed partial themes support - AppThemeBinding is sufficient.
- Documented edited file matching code.
- Updated tests to use Unix-style paths for cross-platform.


##2022-12-03 v0.8

Gui:
- Added a simple splash screen for Windows and Mac.
- Improved look of project's app icon and fixed project file so that it is used.
- Conditional gui app title to distinguish development and installed versions.
- Main window (except progress dialog) is disabled during extraction.
- Added some support for styling, but this appers to be broken at present - see https://github.com/dotnet/maui/issues/6596
- Restored Android and iOS targets, as this seems to be the only way to render the project pubishable - see https://github.com/dotnet/maui/issues/11816


##2022-11-18 v0.7

Added config option for time kind used for photo output file naming. Options are local time or UTC, default is local time to
match EXIF value.
- Added combo to Gui project.
- Added -ft option to Cli project.

Allow selectable log file format - json or xml.
- Added combo to Gui project.
- Modified -lf option to Cli project to take new values.

Gui
- Gui starts maximised and flashes its takbar icon on completion (unless manually cancelled).
- Re-organised main page so that start button is immediately visible.
- Restored origonal grey theme and added a little colour.
- Various minor fit and finish changes.

Added enum parsing tests.


##2022-11-04 v0.6.2

- Restored previous output package names: tex and tex-gui.
- Misc cleanup including removing unused platform targets.


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

