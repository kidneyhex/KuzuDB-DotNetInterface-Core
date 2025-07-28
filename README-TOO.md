# To update the version of KuzuDB

## Presteps
1) Download the latest version of KuzuDB's library from here:
https://github.com/kuzudb/kuzu/releases/

Look for the `libkuzu-windows-x86_64.zip` file and download that. Unzip it and copy the files from there to the `KuzuFiles` folder.

2) Download and install SWIG from https://www.swig.org/download.html

3) Update the path to SWIG in `refresh.bat`

## Building

### 1 - REFRESH.BAT

Run `refresh.bat` or manually do the steps in it.

This will delete the old kuzu wrapper files, and run SWIG to regenerate new wrapper files


### 2 - REBUILD.BAT  
Open an "x64 Native Tools Commandline" and go to the `\wrapperlib` folder.

Run `wrapperlib\rebuild.bat` from inside that commandline -- so that it has access to the Visual Studio CMake and Build commands.

This will generate the new kuzunet.dll for .NET to use


### 3 - OVERWRITE.BAT
Run `overwrite.bat`

This moves the new version of the kuzu wrapper down into the .NET projects.