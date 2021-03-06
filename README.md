# Takout Extracter

Extracts the contents of a [Google Takeout](https://takeout.google.com/) archive - re-organising it, adding missing metadata, and
applying a uniform file naming convention.

This software currently processes only photo and video files. It is planned to add support for other media types in the future.

- *Photos and Videos* Image files in a Google takeout dataset have inconsistent naming. They also do not contain exif timestamps -
although, confusingly, they do contain other metadata such as location information and camera settings. This software builds a uniformaly
named copy of the image and video files in a takeout dataset and restores their exif timestamps.


## Getting Started

1. Build the solution in Visual Studio 2022

2. The command-line extractor, `tex.exe`, will be found in `TakeoutExtractorCli\bin\Release\net6.0\`.
Run `tex /h` for help.


## Built With

- Visual Studio 2022. Maui-based gui currently requires VS2022 7.3.0 preview 2.0 or later.
- .net 6.0, with nullable reference type checking enabled
- [ExifLibNet](https://www.nuget.org/packages/ExifLibNet) v2.1.4 or later, via nuget


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

*(The following are notes that I made during development, when I was initially attempting to match the image json manifest to
its corresponding image(s) based on file name - to avoid the necessity to parse the json itself. Due to ambiguities in the file
naming conventions used by Google, I eventually abandond this. These notes are left here for posterity, and perhaps future
reference.)*

Google takeout appears to provide access to the original captured form of an image, together with the last edited version of
the image, if any. The images are linked together by a json metadata file.

The relationship between a json metadata file and it's corresponding content file is denoted by their sharing a common name-part.
However, This relationship is sometimes elided by the use of a rather erattic naming convention.

Image file extentions can be ".jpeg", ".jpg", ".png", ".gif" and ".mp4".


The simplest example is:

> IMG_20180713_135308952.jpeg.json
> IMG_20180713_135308952.jpeg


Where a file has been edited the following pattern will exist:

> IMG_20191124_153331470.jpg.json
> IMG_20191124_153331470.jpg
> IMG_20191124_153331470-edited.jpg

Sometimes the suffix is "-edit":

> facebook_1602348303689_6720735899556507268.jpg.json
> facebook_1602348303689_6720735899556507268.jpg
> facebook_1602348303689_6720735899556507268-edit.jpg


It is possible to have an "-edited" file with no original file - for example, if the original has been deleted.
In this case the json file name will contain the "-original" element. E.g.

> IMG_20190329_083618347-edited.jpeg.json
> IMG_20190329_083618347-edited.jpeg


Sequence numbers enclosed by brackets are used to make filenames unique.
The positioning of the bracketed part within the metadata file's name is counter-intuitive.

> IMG_20180713_135308952.jpeg(1).json
> IMG_20180713_135308952(1).jpeg


Where an image has been edited and its name contains a sequence number then then following pattern is observed:

> IMG_20191124_153331470.jpg(1).json
> IMG_20191124_153331470(1).jpg
> IMG_20191124_153331470-edited(1).jpg


The metadata file name (excluding extension) contains at most the first 46 characters of the media file name(s).
This means that we cannot simply remove the .json extension to determine the full file name of the corresponding
media files(s). Examples are:

> LRM_EXPORT_14245560034330_20190529_193902095.j.json
> LRM_EXPORT_14245560034330_20190529_193902095.jpeg

and

> IMG_20200915_184120990_BURST000_COVER_TOP~2.jp.json
> IMG_20200915_184120990_BURST000_COVER_TOP~2.jpg


Sometimes truncation is combined with a sequence number:

> original_01256fa2-4316-4e9c-936c-f7230ea8e5e0_.json
> original_01256fa2-4316-4e9c-936c-f7230ea8e5e0_I.jpg
> original_01256fa2-4316-4e9c-936c-f7230ea8e5e0_I(1).jpg

Here the file with the sequence number is the edited one, and the one without is the original.
