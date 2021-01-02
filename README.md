This is a simple application designed to convert all docx or txt files in a specified folder into a single epub. Folder is specified through json settings.

The code is now meant to be mostly extensible for future file types.

By Default, a json file will be generated to generate based off current directory. By default, the file name will match the application name, followed by _settings.

Different json files can be passed in as the first Command Line Argument.


An old precompiled portable executable can be downloaded in http://kiradien.com/DocXFoldertoEpub.zip - exe contains all necessary dlls.

Portable executable can be compiled after building for release and running "msbuild /t:ILMerge";
