# FH3DecryptNames
A tool used to auto-rename distorted file and directory names in dumped version of Forza Horizon 3.

## Download
- <a href="https://github.com/oxmaulmike2581/FH3DecryptNames/releases/latest">version 1.0.0.0</a>

## Features
- batch rename whole folder with all subfolders and files in it, given as input path
- batch rename all entries inside zip archives

## Usage
- First of all, you need to rename all files and directories. Just type that inside a Command Line/Terminal window:<br />
  `fh3decryptnames.exe "input_path"`
  <br /><br />
  Where `input_path` is a path where your files is placed, for example: `C:\FH3`.<br />
  Note: if your path contains non-ASCII characters *(like cyrillic or spaces, etc.)*, you must use quotes.
  <br /><br />
- Secondly, you need to do the same with zip archives. Just add `-zip` option to the command:<br />
  `fh3decryptnames.exe "input_path" -zip`

## Known bugs / Errors / Not implemented options
- The tool can't process zip files inside `/media/audio/enginesynth/pc` folder because these archives are encrypted. 7-Zip, WinRAR, and even QuickBMS with `zip.bms` script can't unpack these archives, so the better way to bypass it is a temporarily move whole `/media/audio` folder somewhere else, for example, in Desktop and move it back when the tool is done its work.

## Notes
- This tool uses 7-Zip 22.00 (x64) to work with zip archives.
- To run the tool, you need .NET Framework 4.7.2 or newer installed.
