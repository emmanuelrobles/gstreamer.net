using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using CodeCrush.GStreamer.SourceGenerator;

namespace codecrush.gstreamer.net.tests;

/// <summary>
/// Guards the two places the dllmap files can drift away from the bindings: a
/// submodule bump that adds a module (a <c>DllImport</c> name nobody maps) and
/// an upstream <c>gstreamer-sharp.dll.config</c> entry that is missing or wrong
/// in a way the overlay does not correct. Both only surface at runtime, as a
/// <see cref="DllNotFoundException"/> on the first call into that module.
/// </summary>
public class DllMapCoverageTest
{
    /// <summary>
    /// Imported by the bindings but not linked into the gst-full artifact - see
    /// the plugin list in <c>packaging/</c>. Naming them here keeps the gap a
    /// decision instead of an accident; they stay on the identity map so that
    /// the load failure names the library rather than the artifact.
    /// </summary>
    private static readonly string[] NotInFullBuild =
    [
        "gstrtspserver-1.0-0.dll",
        "gstrtsp-1.0-0.dll",
        "gstwebrtc-1.0-0.dll",
    ];

    public static TheoryData<string> ImportedLibraries() => new(ImportedDllNames());

    [Theory]
    [MemberData(nameof(ImportedLibraries))]
    public void SystemProfileMapsEveryImportedLibraryOnUnix(string dll)
    {
        foreach (var os in new[] { OsEnum.LINUX, OsEnum.OSX })
            Assert.NotEqual(dll, DllMap.GetDllTarget(RuntimeProfile.SYSTEM, os, dll));
    }

    [Theory]
    [MemberData(nameof(ImportedLibraries))]
    public void SystemProfileLeavesWindowsToTheInstaller(string dll) =>
        // The official Windows runtime already ships the DllImport names.
        Assert.Equal(dll, DllMap.GetDllTarget(RuntimeProfile.SYSTEM, OsEnum.WINDOWS, dll));

    [Theory]
    [MemberData(nameof(ImportedLibraries))]
    public void FullProfileMapsEverythingTheArtifactContains(string dll)
    {
        if (NotInFullBuild.Contains(dll, StringComparer.Ordinal))
        {
            Assert.Equal(dll, DllMap.GetDllTarget(RuntimeProfile.FULL, OsEnum.LINUX, dll));
            Assert.Equal(dll, DllMap.GetDllTarget(RuntimeProfile.FULL, OsEnum.WINDOWS, dll));
            return;
        }

        Assert.NotEqual(dll, DllMap.GetDllTarget(RuntimeProfile.FULL, OsEnum.LINUX, dll));
        Assert.NotEqual(dll, DllMap.GetDllTarget(RuntimeProfile.FULL, OsEnum.WINDOWS, dll));
    }

    [Fact]
    public void UnsupportedListNamesOnlyLibrariesTheBindingsImport()
    {
        var imported = ImportedDllNames().ToHashSet(StringComparer.Ordinal);

        Assert.All(NotInFullBuild, dll => Assert.Contains(dll, imported));
    }

    /// <summary>
    /// Reads the <c>DllImport</c> module names out of the bindings themselves, so
    /// the expectation follows the submodule instead of a copy of it.
    /// </summary>
    private static string[] ImportedDllNames()
    {
        using var stream = File.OpenRead(typeof(DllMap).Assembly.Location);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if ((method.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) == 0)
                continue;

            var name = reader.GetString(reader.GetModuleReference(method.GetImport().Module).Name);

            // gstreamer-sharp declares every module with GLib's Windows naming
            // scheme (glib-2.0-0.dll, gstvideo-1.0-0.dll, ...). The rest -
            // kernel32.dll from this package's own search-path setup - are OS
            // libraries the map is not about.
            if (name.EndsWith("-0.dll", StringComparison.Ordinal))
                names.Add(name);
        }

        Assert.NotEmpty(names);

        return [.. names];
    }
}
