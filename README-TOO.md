# KuzuDB .NET Interface - Build Instructions

## Overview
This repository contains a simplified SWIG-based .NET wrapper for KuzuDB.

## Quickstart
Run these in order from the repo root:

```batch
.\refresh.bat
.\rebuild.bat
.\overwrite.bat
```

Quick notes on each script:
- `refresh.bat` regenerates the SWIG C# classes and the C++ wrapper (`wrapperlib\kuzu_wrap.cpp`).
- `rebuild.bat` builds `kuzunet.dll` using CMake + Ninja (requires VS environment variables).
- `overwrite.bat` copies the generated C# files and native DLLs into the .NET projects.


## Branch Structure
- **`rough-draft`** - Main development branch with stable KuzuDB version
- **`simplified-swig-interface`** - Contains an even rougher draft attempting to rework things

## Prerequisites

### 1. KuzuDB Native Libraries
Download the latest version of KuzuDB's library from:
https://github.com/kuzudb/kuzu/releases/

Look for the `libkuzu-windows-x86_64.zip` file, download and extract it, then copy the files to the `KuzuFiles` folder:
- `kuzu_shared.dll`
- `kuzu_shared.lib` 
- `kuzu.h`
- `kuzu.hpp`

### 2. SWIG (C++ to C# Wrapper Generator)
Download and install SWIG from: https://www.swig.org/download.html

Update the path to SWIG in `refresh.bat` (currently set to `p:\home\tools\swigwin\swig.exe`)

### 3. Visual Studio Build Tools
Ensure you have Visual Studio 2022 with C++ build tools installed. The build process requires:
- CMake (available in VS environment)
- Ninja build system (available in VS environment)
- MSVC C++ compiler

## Building Process

### Method 1: Using Batch Files (Recommended)

#### Step 1: Generate SWIG Wrappers
```batch
.\refresh.bat
```
This will:
- Delete old wrapper files from `KuzuFiles\generated_classes\`
- Run SWIG to generate new C# wrapper classes from `SWIG-InputFile\kuzu.i`
- Generate C++ wrapper code in `wrapperlib\kuzu_wrap.cpp`

#### Step 2: Build Native Library
```batch
.\rebuild.bat
```
This will:
- Configure the build with CMake
- Compile the native `kuzunet.dll` using Ninja
- **Note:** This step requires Visual Studio environment variables

#### Step 3: Deploy to .NET Projects
```batch
.\overwrite.bat
```
This will:
- Copy generated C# classes to both .NET Framework and .NET Core projects
- Copy `kuzunet.dll` and `kuzu_shared.dll` to project directories

### Method 2: Manual Build with VS Environment

If you need to run the build with Visual Studio environment manually:

```powershell
cmd /c '"C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64 && cd wrapperlib && cd build && cmake -G Ninja .. -DCMAKE_BUILD_TYPE=Release && ninja'
```
