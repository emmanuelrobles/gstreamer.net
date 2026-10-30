// Support for gstreamer-sharp's generated GType registration list.
//
// Upstream's generated GtkSharp/ObjectManager.cs registers every object type in
// one unguarded sequence, and each wrapper class's static constructor calls it.
// One module the runtime does not contain therefore breaks all of them: on a
// gst-full build without WebRTC and RTSP server, touching Gst.App.AppSink died
// with DllNotFoundException: gstwebrtc-1.0-0.dll before AppSink finished
// initialising.
//
// The ObjectManager itself is generated from upstream's list, keeping upstream's
// order and set of types - see
// codecrush.gstreamer.source-generator/ObjectManagerGenerator.cs - and routes
// every entry through Register(), so each is attempted on its own.

namespace GtkSharp.GstreamerSharp;

using System.Runtime.InteropServices;

internal static class GTypeRegistry
{
    /// <summary>
    /// Registers the managed type for the GType its native module reports.
    /// </summary>
    /// <param name="gtype">
    /// Reads the GType. A function rather than a value, because reading it is the
    /// P/Invoke that loads the module: <c>typeof (T)</c> never runs a static
    /// constructor, so a value would move the failure out of the guard.
    /// </param>
    /// <param name="managed">The wrapper class to register.</param>
    /// <remarks>
    /// A type whose native module is missing stays unregistered. That costs only
    /// the most-derived managed wrapper for objects of that type, and using the
    /// type directly still fails with the module named. Any other failure,
    /// including a TypeInitializationException with a different cause,
    /// propagates.
    /// </remarks>
    public static void Register(Func<GLib.GType> gtype, Type managed)
    {
        try
        {
            GLib.GType.Register(gtype(), managed);
        }
        catch (Exception ex) when (IsModuleUnavailable(ex))
        {
        }
    }

    /// <summary>
    /// Whether the exception means a native module is not available, as opposed
    /// to something that has to be seen.
    /// </summary>
    private static bool IsModuleUnavailable(Exception ex) =>
        ex is DllNotFoundException or EntryPointNotFoundException
        || ex is TypeInitializationException { InnerException: DllNotFoundException or EntryPointNotFoundException };
}
