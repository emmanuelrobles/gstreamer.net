# CodeCrush.gstreamer

`CodeCrush.gstreamer` provides .NET integration helpers for GStreamer.

## Installation

```bash
dotnet add package CodeCrush.gstreamer
```

## Prerequisites

- GStreamer must be installed separately on the host machine.
- This package provides .NET bindings/helpers and does not bundle GStreamer itself.

Two layouts are supported, and the right one is picked automatically:

- a regular per-library installation (distro packages, the official Windows
  installer), where `libgstreamer-1.0.so.0`, `libgstapp-1.0.so.0`, … each exist
  on their own;
- a `gst-full` build, where every GStreamer library and plugin is folded into a
  single `libgstreamer-full-1.0.so` / `libgstreamer-full-1.0.dll`, with glib,
  gobject and gio shipped next to it.

## Runtime selection

| variable | values | meaning |
|---|---|---|
| `GST_SHARP_RUNTIME` | `auto` (default), `system`, `full` | `auto` uses the gst-full runtime when the library is found next to the application or in `GST_SHARP_RUNTIME_DIR`, otherwise the per-library installation. |
| `GST_SHARP_RUNTIME_DIR` | directory | where `libgstreamer-full-1.0.so` / `libgstreamer-full-1.0.dll` and the bundled glib live. Defaults to the application directory. |

The mapping is generated from `dllmap` documents - `gstreamer-sharp.dll.config`
for `system`, `gstreamer-full.dll.config` for `full` - and installed from a
module initializer, so it is in place before the first `Gst`/`GLib` static
constructor runs. Names that the selected runtime does not provide (for example
`gstwebrtc`, `gstrtsp`, `gstrtspserver`) are left unmapped: only code that
actually calls into those namespaces fails, not `Gst.Application.Init()`.

The `system` map is built from the `gstreamer-sharp.dll.config` that ships in the
submodule, taken as-is, plus a small `gstreamer-sharp.overlay.dll.config` holding
the entries upstream gets wrong (the glib/gobject/gio targets) and wins per
`(os, dll)`. A submodule bump that adds a library therefore needs no copy step
here. `DllMapCoverageTest` fails if a `DllImport` in the bindings has no target
for the platform it is imported on, which is the check for both drift sources.

If a `DllImport` cannot be resolved, the exception names every path that was
tried, the profile that was in use, and the two environment variables above.

### gst-full specifics

`Application.Init()` and `Application.InitCheck()` call
`gst_init_static_plugins()` when the `full` profile is active. `gst_init()`
cannot do it itself: it looks the symbol up with `g_module_open(NULL)`, which
does not see a library that was `dlopen`'d with `RTLD_LOCAL` - which is how .NET
loads native libraries. Without that call the registry stays empty and every
element factory lookup returns null.

`Gst.Application`'s static constructor no longer registers
`Gst.WebRTC.WebRTCSessionDescription`'s GType. That registration runs before
`gst_init()` and needs `gst_webrtc_session_description_get_type()`, which does
not exist unless `gstreamer-webrtc-1.0` is installed. It is required only for
`GLib.Value` boxing of that boxed type.

## Notes

- Targets: `net10.0`
- Do not install your own `DllImport` resolver for the `Gst`/`GLib` assemblies
  alongside this package: only one resolver can be registered per assembly.
  Installing one first is tolerated, but that resolver then decides how the
  libraries are found.

## Repository & Issues

- Primary repository: https://forgejo.codecrush.dev/CodeCrush/codecrush.gstreamer.net
- GitHub mirror: https://github.com/emmanuelrobles/gstreamer.net
- Issue tracker: https://github.com/emmanuelrobles/gstreamer.net/issues/new/choose

Please open all bug reports and feature requests on the GitHub issue tracker.
