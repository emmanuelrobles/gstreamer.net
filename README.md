# CodeCrush.GStreamer

`CodeCrush.GStreamer` is a .NET package that builds on top of the `gstreamer-sharp` sources shipped inside the upstream GStreamer repository.

This repository packages those bindings for .NET, adds a Roslyn source generator that reads `gstreamer-sharp.dll.config`, and installs a runtime DLL import resolver so native GStreamer libraries can be loaded using the correct platform-specific names.

## Repository Layout

- `codecrush.gstreamer.net/`: the packable `net10.0` library published as `CodeCrush.gstreamer`
- `codecrush.gstreamer.source-generator/`: incremental source generator that turns `dllmap` entries into strongly typed lookup code
- `codecrush.gstreamer.source-generator.tests/`: tests for the generator output
- `gstreamer/`: upstream GStreamer repository checked out as a git submodule

## How It Works

The package project includes generated and custom C# sources directly from `gstreamer/subprojects/gstreamer-sharp/sources/`.

At build time, the source generator:

- reads `gstreamer-sharp.dll.config`
- extracts each `<dllmap>` entry
- generates an `OsEnum`
- generates a `DllMap` helper used to map Windows-style library names to Linux and macOS targets

At runtime, `Gst.Application` installs a `DllImportResolver` that uses the generated map before calling `NativeLibrary.Load(...)`.

## Prerequisites

- .NET SDK 10.0 or newer
- Git submodules initialized
- GStreamer installed on the target machine

This repository does not bundle native GStreamer binaries. It provides the managed bindings plus the native-library mapping needed to load an existing GStreamer installation.

## Getting The Source

Clone the repository and initialize the submodule:

```bash
git clone https://forgejo.codecrush.dev/CodeCrush/codecrush.gstreamer.net.git
cd codecrush.gstreamer
git submodule update --init --recursive
```

If the `gstreamer/` submodule is missing, the main package project will not compile because it imports source files from that tree.

## Build

```bash
dotnet build codecrush.gstreamer.slnx
```

## Test

```bash
dotnet test codecrush.gstreamer.slnx
```

## Packaging

The NuGet package metadata is defined in `codecrush.gstreamer.net/codecrush.gstreamer.net.csproj`.

To create a package locally:

```bash
dotnet pack codecrush.gstreamer.net/codecrush.gstreamer.net.csproj -c Release
```

## Package Notes

- Package ID: `CodeCrush.gstreamer`
- Target framework: `net10.0`
- The package includes a package-level README from `codecrush.gstreamer.net/README.md`
- The source generator is referenced as an analyzer by the main package project

## License

This repository includes upstream GStreamer sources and `gstreamer-sharp` sources via the `gstreamer` submodule. Review `LICENSE.md` and the licenses inside the upstream submodule before redistributing derived packages.
