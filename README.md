# TatoosExtractV

## Overview

Simple .NET 4.8 console application designed to extract all V tattoos and their corresponding preview images using [Codewalker](https://github.com/dexyfex/CodeWalker)

## Getting Started

### Usage

To run the application, open a command prompt, navigate to the directory containing the executable, and use the following command:

```bash
TatoosExtractV.exe [options]
```

### Command-Line Options

- `--output=<path>`: Specify the output directory for the extracted files (default is the current directory).
- `--disable-texture-extract`: Disables the extraction of tattoo texture images.

### Example

```bash
TatoosExtractV.exe --output="C:\GTA_V_Tattoos" --disable-texture-extract
```

## Output

The extracted tattoo data will be saved in a file named `tatoos.json`, and if enabled, the corresponding tattoo images will be saved as PNG files in the specified output directory.
