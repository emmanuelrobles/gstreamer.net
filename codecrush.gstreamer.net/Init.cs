using System.Reflection;
using System.Runtime.InteropServices;
using CodeCrush.GStreamer.SourceGenerator;

namespace Gst;

partial class Application
{
    private class NativeLibLoader
    {
        static NativeLibLoader()
        {
            var currentOs = OsEnum.WINDOWS;
            if (OperatingSystem.IsLinux()) currentOs = OsEnum.LINUX;
            else if (OperatingSystem.IsMacOS()) currentOs = OsEnum.OSX;

            var glibAssembly = typeof(GLib.GType).Assembly;
            var gstAssembly = typeof(Gst.Application).Assembly;

            NativeLibrary.SetDllImportResolver(glibAssembly, (name, assembly, path) => Resolve(name, assembly, path, currentOs));
            if (gstAssembly != glibAssembly)
                NativeLibrary.SetDllImportResolver(gstAssembly, (name, assembly, path) => Resolve(name, assembly, path, currentOs));
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, OsEnum currentOs)
        {
            var target = DllMap.GetDllTarget(currentOs, libraryName);
            return NativeLibrary.Load(target, assembly, searchPath);
        }
    }

    private static readonly NativeLibLoader _ = new();
}