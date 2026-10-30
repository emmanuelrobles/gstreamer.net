using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace codecrush.gstreamer.source_generator.tests;

public class DllMapGeneratorTest
{
    private const string SystemConfig = "/virtual/path/gstreamer-sharp.dll.config";
    private const string FullConfig = "/virtual/path/gstreamer-full.dll.config";

    [Fact]
    public void EmitsBothProfilesFromOneConfig()
    {
        // Only the system config is present: the full profile still has to be
        // emitted, mapping names to themselves, so consumers always compile.
        var (driver, outputCompilation) = Run(
            new InMemoryAdditionalText(
                SystemConfig,
                """
                <configuration>
                  <dllmap dll="gstreamer-1.0-0.dll" target="libgstreamer-1.0.so.0" os="linux"/>
                  <dllmap dll="glib-2.0-0.dll" target="libglib-2.0.so.0" os="linux"/>
                </configuration>
                """));

        Assert.Equal(4, outputCompilation.SyntaxTrees.Count()); // source + OsEnum + RuntimeProfile + DllMap

        var dllMap = Generated(driver, "DllMap.g.cs");

        // system/linux is mapped ...
        Assert.Contains("\"gstreamer-1.0-0.dll\" => \"libgstreamer-1.0.so.0\"", dllMap);
        Assert.Contains("GetSystemTargetLinux", dllMap);

        // ... full/linux is not, so it stays the identity mapping
        Assert.Contains("GetFullTarget", dllMap);
        Assert.DoesNotContain("libgstreamer-full-1.0", dllMap);
    }

    [Fact]
    public void SelectsProfileByFileName()
    {
        var (driver, _) = Run(
            new InMemoryAdditionalText(
                SystemConfig,
                """
                <configuration>
                  <dllmap dll="gstreamer-1.0-0.dll" target="libgstreamer-1.0.so.0" os="linux"/>
                  <dllmap dll="glib-2.0-0.dll" target="libglib-2.0.so.0" os="linux"/>
                </configuration>
                """),
            new InMemoryAdditionalText(
                FullConfig,
                """
                <configuration>
                  <dllmap dll="gstreamer-1.0-0.dll" target="libgstreamer-full-1.0.so" os="linux"/>
                  <dllmap dll="gstreamer-1.0-0.dll" target="libgstreamer-full-1.0.dll" os="windows"/>
                  <dllmap dll="glib-2.0-0.dll" target="libglib-2.0.so.0" os="linux"/>
                  <dllmap dll="glib-2.0-0.dll" target="libglib-2.0-0.dll" os="windows"/>
                </configuration>
                """));

        Assert.Empty(driver.GetRunResult().Diagnostics);

        var dllMap = Generated(driver, "DllMap.g.cs");

        // Same DllImport name, two targets, one per profile.
        Assert.Contains("\"gstreamer-1.0-0.dll\" => \"libgstreamer-1.0.so.0\"", dllMap);
        Assert.Contains("\"gstreamer-1.0-0.dll\" => \"libgstreamer-full-1.0.so\"", dllMap);
        Assert.Contains("\"gstreamer-1.0-0.dll\" => \"libgstreamer-full-1.0.dll\"", dllMap);

        // Windows is only present in the full profile.
        Assert.Contains("\"glib-2.0-0.dll\" => \"libglib-2.0-0.dll\"", dllMap);
        Assert.Contains("GetFullTargetWindows", dllMap);
        Assert.DoesNotContain("GetSystemTargetWindows", dllMap);
    }

    [Fact]
    public void OsEnumIsStableRegardlessOfConfigs()
    {
        var (driver, _) = Run();

        var osEnum = Generated(driver, "OsEnum.g.cs");

        // The resolver names the OS it runs on, so these must always exist.
        Assert.Contains("    WINDOWS,", osEnum);
        Assert.Contains("    LINUX,", osEnum);
        Assert.Contains("    OSX,", osEnum);

        var profiles = Generated(driver, "RuntimeProfile.g.cs");
        Assert.Contains("    SYSTEM,", profiles);
        Assert.Contains("    FULL,", profiles);
    }

    [Fact]
    public void ReportsMalformedConfig()
    {
        var (driver, _) = Run(new InMemoryAdditionalText(FullConfig, "<configuration><dllmap"));

        var diagnostic = Assert.Single(driver.GetRunResult().Diagnostics);

        Assert.Equal("GSTMAP001", diagnostic.Id);
        Assert.Contains("gstreamer-full.dll.config", diagnostic.GetMessage());
    }

    [Fact]
    public void OverlayIsAppliedOnTopOfItsBaseFile()
    {
        // Order is deliberately overlay-first: the layer decides which entry
        // wins, not the order the compiler hands the files over.
        var (driver, _) = Run(
            new InMemoryAdditionalText(
                "/virtual/path/gstreamer-sharp.overlay.dll.config",
                """
                <configuration>
                  <dllmap dll="glib-2.0-0.dll" target="libglib-2.0.so.0" os="linux"/>
                  <dllmap dll="gio-2.0-0.dll" target="libgio-2.0.so.0" os="linux"/>
                </configuration>
                """),
            new InMemoryAdditionalText(
                SystemConfig,
                """
                <configuration>
                  <dllmap dll="glib-2.0-0.dll" target="glib-2.0.so.0" os="linux"/>
                  <dllmap dll="gstreamer-1.0-0.dll" target="libgstreamer-1.0.so.0" os="linux"/>
                </configuration>
                """));

        Assert.Empty(driver.GetRunResult().Diagnostics);

        var dllMap = Generated(driver, "DllMap.g.cs");

        // Overridden rather than duplicated.
        Assert.Contains("\"glib-2.0-0.dll\" => \"libglib-2.0.so.0\"", dllMap);
        Assert.DoesNotContain("\"glib-2.0-0.dll\" => \"glib-2.0.so.0\"", dllMap);

        // Added by the overlay, and kept from the base file.
        Assert.Contains("\"gio-2.0-0.dll\" => \"libgio-2.0.so.0\"", dllMap);
        Assert.Contains("\"gstreamer-1.0-0.dll\" => \"libgstreamer-1.0.so.0\"", dllMap);
    }

    [Fact]
    public void OverlayInheritsTheProfileOfItsBaseName()
    {
        var (driver, _) = Run(
            new InMemoryAdditionalText(
                "/virtual/path/gstreamer-full.overlay.dll.config",
                """
                <configuration>
                  <dllmap dll="gstwebrtc-1.0-0.dll" target="libgstreamer-full-1.0.so" os="linux"/>
                </configuration>
                """));

        var dllMap = Generated(driver, "DllMap.g.cs");

        Assert.Contains("GetFullTargetLinux", dllMap);
        Assert.Contains("\"gstwebrtc-1.0-0.dll\" => \"libgstreamer-full-1.0.so\"", dllMap);
        Assert.DoesNotContain("GetSystemTargetLinux", dllMap);
    }

    [Fact]
    public void IgnoresConfigsThatAreNotAProfile()
    {
        // A .dll.config that is not named after a profile must not contribute:
        // guessing would make an unlucky AdditionalFile change loading.
        var (driver, _) = Run(
            new InMemoryAdditionalText(
                "/virtual/path/gst-editing-services-sharp.dll.config",
                """
                <configuration>
                  <dllmap dll="ges-1.0-0.dll" target="libges-1.0.so.0" os="linux"/>
                </configuration>
                """));

        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.DoesNotContain("libges-1.0.so.0", Generated(driver, "DllMap.g.cs"));
    }

    private static string Generated(GeneratorDriver driver, string hintName) =>
        driver.GetRunResult()
            .Results.Single()
            .GeneratedSources.Single(s => s.HintName == hintName)
            .SourceText.ToString();

    private static (GeneratorDriver Driver, Compilation Output) Run(params AdditionalText[] additionalFiles)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("", cancellationToken: TestContext.Current.CancellationToken);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new DllMapperGenerator().AsSourceGenerator()],
            additionalTexts: additionalFiles);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _,
            TestContext.Current.CancellationToken);

        return (driver, outputCompilation);
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }
}
