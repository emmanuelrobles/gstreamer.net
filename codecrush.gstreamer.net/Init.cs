using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeCrush.GStreamer.SourceGenerator;

namespace Gst;

/// <summary>
/// Maps the library names gstreamer-sharp declares (<c>gstreamer-1.0-0.dll</c>,
/// <c>glib-2.0-0.dll</c>, ...) onto the files that are actually present, and
/// installs that mapping as the DllImport resolver of the assemblies that
/// declare them.
/// </summary>
/// <remarks>
/// Two layouts are supported, selected by <c>GST_SHARP_RUNTIME</c>:
/// <list type="bullet">
/// <item><c>auto</c> (default) - use the gst-full runtime when it can be found
/// next to the application or in <c>GST_SHARP_RUNTIME_DIR</c>, otherwise fall
/// back to a regular, per-library installation.</item>
/// <item><c>system</c> - a regular GStreamer installation.</item>
/// <item><c>full</c> - a gst-full build, i.e. one library holding every
/// GStreamer namespace.</item>
/// </list>
/// </remarks>
internal static class NativeLibLoader
{
    internal const string RuntimeVariable = "GST_SHARP_RUNTIME";
    internal const string RuntimeDirectoryVariable = "GST_SHARP_RUNTIME_DIR";

    /// <summary>File name of the gst-full library on the current platform.</summary>
    internal const string FullLibraryLinux = "libgstreamer-full-1.0.so";
    internal const string FullLibraryWindows = "libgstreamer-full-1.0.dll";

    private static readonly ConcurrentDictionary<string, IntPtr> Handles = new(StringComparer.Ordinal);

    private static readonly Lazy<Selection> Selected = new(Select, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly OsEnum CurrentOs = DetectOs();

    internal static OsEnum Os => CurrentOs;

    /// <summary>The profile the loader settled on. Resolved once, on first use.</summary>
    internal static RuntimeProfile Profile => Selected.Value.Profile;

    /// <summary>Directory the libraries were looked up in, if one was chosen.</summary>
    internal static string? Directory => Selected.Value.Directory;

    /// <summary>
    /// Registers the resolver for every assembly that carries native imports.
    /// Called from a module initializer so that it is in place before any
    /// Gst.Application / Gst.Version / GLib static constructor runs.
    /// </summary>
    internal static void Install()
    {
        TryInstall(typeof(GLib.GType).Assembly);
        TryInstall(typeof(Application).Assembly);
    }

    private static void TryInstall(Assembly assembly)
    {
        try
        {
            NativeLibrary.SetDllImportResolver(assembly, static (name, _, _) => Resolve(name));
        }
        catch (InvalidOperationException)
        {
            // Something else already installed one for this assembly - a
            // consumer that mapped GStreamer itself before this package did.
            // Leave it in charge rather than failing the whole module load.
        }
    }

    private static IntPtr Resolve(string libraryName)
    {
        var selection = Selected.Value;
        PrepareSearchPath(selection);
        var target = DllMap.GetDllTarget(selection.Profile, CurrentOs, libraryName);
        var key = selection.Directory is null ? target : Path.Combine(selection.Directory, target);

        if (Handles.TryGetValue(key, out var cached))
            return cached;

        var handle = Load(target, selection, libraryName);
        Handles[key] = handle;
        return handle;
    }

    /// <summary>
    /// Windows only: gst-full resolves its own imports (libglib-2.0-0.dll,
    /// ...) through the loader's standard search order, which does not include
    /// a directory the library merely sits in - so a runtime directory that is
    /// not the application directory would give it one glib and this resolver
    /// another. On Linux the library carries RPATH=$ORIGIN for the same reason.
    /// </summary>
    private static void PrepareSearchPath(Selection selection)
    {
        if (selection.Directory is null || !OperatingSystem.IsWindows())
            return;

        if (Interlocked.Exchange(ref SearchPathPrepared, 1) != 0)
            return;

        SetDllDirectory(selection.Directory);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDllDirectory([MarshalAs(UnmanagedType.LPWStr)] string path);

    private static int SearchPathPrepared;

    private static IntPtr Load(string target, Selection selection, string requested)
    {
        var tried = new List<string>(3);

        if (selection.Directory is not null)
        {
            var local = Path.Combine(selection.Directory, target);
            tried.Add(local);
            if (NativeLibrary.TryLoad(local, out var localHandle))
                return localHandle;
        }

        tried.Add(target);
        if (NativeLibrary.TryLoad(target, out var mappedHandle))
            return mappedHandle;

        if (target != requested)
        {
            tried.Add(requested);
            if (NativeLibrary.TryLoad(requested, out var originalHandle))
                return originalHandle;
        }

        throw new DllNotFoundException(
            $"{requested}: could not load the native GStreamer library for the {selection.Profile} profile. " +
            $"Tried {string.Join(", ", tried)}. {Describe(selection)}.");
    }

    private static Selection Select()
    {
        var requested = Environment.GetEnvironmentVariable(RuntimeVariable)?.Trim().ToLowerInvariant();
        var configured = EmptyToNull(Environment.GetEnvironmentVariable(RuntimeDirectoryVariable));

        switch (requested)
        {
            case "system":
                return new Selection(RuntimeProfile.SYSTEM, configured);

            case "full":
                // An explicit profile: honour the configured directory as-is so
                // the error message names exactly where it was looked for.
                return new Selection(
                    RuntimeProfile.FULL,
                    configured ?? (ContainsFullLibrary(AppContext.BaseDirectory) ? AppContext.BaseDirectory : null));

            case null:
            case "":
            case "auto":
                if (configured is not null && ContainsFullLibrary(configured))
                    return new Selection(RuntimeProfile.FULL, configured);

                if (ContainsFullLibrary(AppContext.BaseDirectory))
                    return new Selection(RuntimeProfile.FULL, AppContext.BaseDirectory);

                return new Selection(RuntimeProfile.SYSTEM, configured);

            default:
                throw new InvalidOperationException(
                    $"{RuntimeVariable}='{requested}' is not valid. Expected 'auto', 'system' or 'full'.");
        }
    }

    private static bool ContainsFullLibrary(string directory) =>
        File.Exists(Path.Combine(directory, CurrentOs == OsEnum.WINDOWS ? FullLibraryWindows : FullLibraryLinux));

    private static OsEnum DetectOs()
    {
        if (OperatingSystem.IsLinux()) return OsEnum.LINUX;
        if (OperatingSystem.IsMacOS()) return OsEnum.OSX;
        return OsEnum.WINDOWS;
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Describe(Selection selection) =>
        $"{RuntimeVariable}={Environment.GetEnvironmentVariable(RuntimeVariable) ?? "auto"}, " +
        (selection.Directory is null
            ? "no explicit search directory"
            : $"searching '{selection.Directory}'") +
        $"; set {RuntimeVariable} to 'system' or 'full', and {RuntimeDirectoryVariable} to point at the directory holding the libraries";

    private readonly record struct Selection(RuntimeProfile Profile, string? Directory);
}

/// <summary>
/// Installs the resolver before anything else in this assembly can run, so that
/// GLib and Gst static constructors - and any P/Invoke made before
/// <see cref="Gst.Application.Init()"/> - are translated the same way.
/// </summary>
internal static class NativeLibLoaderBootstrap
{
#pragma warning disable CA2255 // Deliberate: a consumer can touch GLib/Gst before this library is asked to do anything.
    [ModuleInitializer]
    internal static void Initialize() => NativeLibLoader.Install();
#pragma warning restore CA2255
}
