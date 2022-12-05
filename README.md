# Takout Extractor

Extracts the contents of a [Google Takeout](https://takeout.google.com/) archive - re-organising it, adding missing metadata, and
applying a uniform file naming convention.

This software currently processes only photo and video files. It is planned to add support for other media types in the future.

- *Photos and Videos* Image files in a Google takeout dataset have inconsistent naming. They also do not contain exif timestamps -
although, confusingly, they do contain other metadata such as location information and camera settings. This software builds a uniformaly
named copy of the image and video files in a takeout dataset and restores their exif timestamps.


## Getting Started

1. Build the solution in Visual Studio 2022

2. The Gui-extractor, tex-gui.exe, will be found in `TakeoutExtractorGui\bin\Release\net6.0\`.

3. The command-line extractor, `tex.exe`, will be found in `TakeoutExtractorCli\bin\Release\net6.0\`.
Run `tex /h` for help.



## Built With

- Visual Studio 2022, v17.3 or later for .net Maui support. .net 6.0, with nullable reference type checking enabled
- Fork of [ExifLibNet](https://www.nuget.org/packages/ExifLibNet) with bug-fixes, included in the `ThirdParty` directory with source available at https://github.com/andyjohnson0/exiflibrary.


## Author

Andeew Johnson | [github.com/andyjohnson0](https://github.com/andyjohnson0) | https://andyjohnson.uk


## Licence

Except for third-party elements that are licened separately, this project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

The folder picker implementations used in the gui project are based on code from [MauiFolderPickerSample](https://github.com/jfversluis/MauiFolderPickerSample)
and https://blog.verslu.is/maui/folder-picker-with-dotnet-maui/ by Gerald Versluis. 
Licenced as [Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)] (https://creativecommons.org/licenses/by-sa/4.0/) by the original author.

The [shovel icon](https://github.com/frexy/glyph-iconset/blob/master/svg/si-glyph-shovel.svg) used by the gui project is from (SmartIcon's)[https://smarticons.co/]
excellent *Gylph* icon set at https://glyph.smarticons.co/ and https://github.com/frexy/glyph-iconset. 
Licenced as [Attribution-ShareAlike 4.0 International (CC BY-SA 4.0)](http://creativecommons.org/licenses/by-sa/4.0/) by the original author.


## Future Enhancements, TODOs, and known bugs

These are logged as issues at https://github.com/andyjohnson0/TakeoutExtractor/issues/

Things to do are flagged with `TODO:` in the code. 



## Implementation Notes

### Project Structure

- **TakeoutExtractor.Gui** GUI front-end using MAUI.

- **TakeoutExtractor.Cli** Command-line `tex` app that drives the extraction process.

- **TakeoutExtractor.Lib** Core library for the Takeout Extractor project. Exposes the TakeoutExtractor class which
coordinates the extraction and reassembly of files from an unzipped Google Takeout archive.

- **TakeoutExtractor.Cli.Tests** Tests for the TakeoutExtractor.Cli project.

- **TakeoutExtractor.Lib.Tests** Tests for the TakeoutExtractor.Lib project.


### Method

#### Images and Videos

1. Iterate over all .json files containing image or video metadata.

2. Extract the title element. This will be the original image file name, including extension.

3. Truncate the *name part* of the title to a maximum of 47 characters. This gives the image file name in the archive.

4. Search for images with the same name but with a (possibly truncated) "-edited" suffix. If this exists then the
image was edited and the image file in the previous step is the original, un-edited, version. If there is no edited
file then only the original image exists.

5. Extract timestamps from the json file and, for images only, update the image's embedded exif metadata. Google seems
to preserve all(?) other exif fields that were populated at the time of image capture.

6. Rename the file or files according to the timestamp and place into appropriate directories.



### Some Resources

EXIF tag reference: <https://exiftool.org/TagNames/EXIF.html>


### Some Notes on Takeout's Image File Naming

Google takeout appears to provide access to the original captured form of an image, together with the last edited version of
the image, if any. The images are linked together by a photo sidecar file. The easiest way to iterate over the images is to 
iterate over the metadata files

Image file extentions can be ".jpeg", ".jpg", ".png", ".gif" and ".mp4". Sometimes an image may have a different extension in
archives requested at different times. For example, it may be .jpeg in one archive and .jpg in an archive created time time
later. I suspect that this may be caused by the introduction of an attempt to normalise file extensions.

The maximum length of file names, including the extension but excluding the dot/period, appears to be 50 characters.
So a jpg file will have a name part with a maximum length of 47, and for a json file this will be 48. File names are truncated
to fit these limits, preserving the extension.

The /title element in the metadata file gives the full file name of the _original_ image. However, as google truncates the name-part
of the image file name, so a title of "a5025662-cb40-45dd-be98-684ee48aa226_IMG_20210818_122959697_HDR.jpg" would refer to an
original image file named "a5025662-cb40-45dd-be98-684ee48aa226_IMG_202108.jpg"

If the title contains & or ? characters then these are substituted with _ characters in the file names of the corresponding images.

If an edited version of the image exists then it will have the same name-part as the original, but with a suffix. This suffix
(which I suspect is generated by Google Photos, not Takeout) is usually "-edited"". It can be truncated (e.g. to "-edit"
or "-edi") if necessary by the 47 character name-part limit.

It is possible to have an "-edited" file with no original file - for example, if the original has been deleted.
In this case the name part of json file name will end with the edited suffix. E.g. IMG_20190329_083618347-edited.jpeg.json
and IMG_20190329_083618347-edited.jpeg

#### File Name Uniqueness

To ensure that names are unique, Takeout appends a "uniqueness suffix" in the form of a bracketed integer (e.g. "(1)") to the end
the name part of the filename. Commonly this will be present in the name of the orginal file, because there is another original
file that would otherwise have the same name. The uniqueness suff will also be present in the json manifest filename, but in
a different position. For example:
- `IMG_20180830_123540573.jpg(1).json`
- `IMG_20180830_123540573(1).jpg`
- `IMG_20180830_123540573-edited(1).jpg`
Here the manifest filename is `IMG_20180830_123540573.jpg(1).json`, _not_ `IMG_20180830_123540573(1).jpg.json` as would be expected.

If the original filename (excluding extension) is 47 characters or more in length then the json manifest will use the first 46
characters in its filename - because the extension is one character longer. If there is an edited file then it will save the *same*
name as the original, but will have a "uniqueness suffix appended to distinguish it from its own original.

#### EXIF Metadata

EXIF image metadata is often - _but not always_ - present in the images. This includes timestamps, description (if the user has
provided one), and geolocation data. The data is included in the json manifest. It appears that the original EXIF metadata is
preserved, but if it is edited in the Google Photos website then the edits are only reflected in the json manifest.


