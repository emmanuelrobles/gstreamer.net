namespace codecrush.gstreamer.net.tests;

/// <summary>
/// Runs against a real native installation and is skipped unless one is
/// selected, so a plain <c>dotnet test</c> does not need GStreamer.
/// </summary>
/// <remarks>
/// Every other test here talks to the generated map. This one exists because
/// the generated map can be perfect while the managed wrappers are unusable:
/// each wrapper class's static constructor runs the whole GType registry, so one
/// unguarded registration for a module the installation does not contain took
/// down <c>Gst.App.AppSink</c> and everything else that touches it.
/// </remarks>
public class FullRuntimeSmokeTest
{
    [Fact]
    public void WrapperClassesInitializeWithoutEveryModuleBeingPresent()
    {
        var profile = Environment.GetEnvironmentVariable("GST_SHARP_RUNTIME")?.Trim().ToLowerInvariant();

        Assert.SkipUnless(
            profile is "full" or "system"
            && (profile != "full" || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GST_SHARP_RUNTIME_DIR"))),
            "set GST_SHARP_RUNTIME=full plus GST_SHARP_RUNTIME_DIR, or GST_SHARP_RUNTIME=system, to run against a native runtime");

        Gst.Application.Init();

        // Whichever modules this runtime lacks - WebRTC and RTSP server are not
        // in the packaged gst-full build, nor in a host install without them -
        // the app plugin has to keep working.
        using var sink = new Gst.App.AppSink("sink");
        using var src = new Gst.App.AppSrc("src");

        Assert.NotEqual(IntPtr.Zero, sink.Handle);
        Assert.NotEqual(IntPtr.Zero, src.Handle);
    }
}
