using System.Runtime.InteropServices;

namespace Gst;

public partial class Sample
{
    [DllImport("gstreamer-1.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void gst_sample_unref(IntPtr sample);

    public void Unref()
    {
        if(Handle != IntPtr.Zero)
        {
            gst_sample_unref(Handle);
        }
    }
}