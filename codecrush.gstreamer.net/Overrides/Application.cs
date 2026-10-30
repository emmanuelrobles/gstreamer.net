// Copyright (C) 2013  Stephan Sundermann <stephansundermann@gmail.com>
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
// 02110-1301  USA

// Local override of gstreamer-sharp's custom/Application.cs (1.28.8); the
// upstream copy is excluded from compilation in codecrush.gstreamer.net.csproj.
//
// Differences from upstream:
//  * no Gst.WebRTC.WebRTCSessionDescription GType registration. It runs in the
//    static constructor and therefore before gst_init(), and it needs
//    gst_webrtc_session_description_get_type(), which does not exist unless
//    gstreamer-webrtc-1.0 is part of the installation. Its only purpose is
//    GLib.Value boxing for that boxed type, which nothing here uses.
//  * gst_init_static_plugins() is called after gst_init() for the gst-full
//    runtime. gst_init() only finds that function through
//    g_module_open(NULL), which cannot see symbols of a library dlopen'ed with
//    RTLD_LOCAL - the way .NET loads it - so without this call the registry
//    stays empty and every element factory lookup fails.

namespace Gst {
	using System;
	using System.Runtime.InteropServices;
	using CodeCrush.GStreamer.SourceGenerator;

	partial class Application {
		// Because of: https://bugzilla.gnome.org/show_bug.cgi?id=743062#c30
		private static uint MIN_GSTREAMER_MINOR = 14;

		static Application() {
			GLib.GType.Register(List.GType, typeof(List));
			GLib.GType.Register(Fraction.GType, typeof(Fraction));
			GLib.GType.Register(DoubleRange.GType, typeof(DoubleRange));
			GLib.GType.Register(IntRange.GType, typeof(IntRange));
			GLib.GType.Register(FractionRange.GType, typeof(FractionRange));
			GLib.GType.Register(DateTime.GType, typeof(DateTime));
			GLib.GType.Register(Gst.Array.GType, typeof(Gst.Array));
			GLib.GType.Register(Promise.GType, typeof(Promise));
		}

		[DllImport("gstreamer-1.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gst_init(ref int argc, ref IntPtr[] argv);

		[DllImport("gstreamer-1.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gst_init(IntPtr argc, IntPtr argv);

		[DllImport("gstreamer-1.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void gst_init_static_plugins();

		/// <summary>
		/// Registers the plugins that a gst-full runtime links in. Exported by
		/// libgstreamer-full-1.0 only, so it is skipped for a regular
		/// installation, where the registry is filled by scanning the plugin
		/// directories. Safe to call more than once.
		/// </summary>
		private static void InitializeStaticPlugins() {
			if (NativeLibLoader.Profile != RuntimeProfile.FULL)
				return;

			gst_init_static_plugins();
		}

		private static void CheckVersion() {
			if (Gst.Version.Minor < MIN_GSTREAMER_MINOR)
				throw new Exception(Gst.Version.Description + " found but GStreamer 1." +
					MIN_GSTREAMER_MINOR + " required.");
		}

		public static void Init() {
			gst_init(IntPtr.Zero, IntPtr.Zero);
			InitializeStaticPlugins();
			CheckVersion();
		}

		public static void Init(ref string[] argv) {
			int cnt_argv = argv == null ? 0 : argv.Length;
			System.Collections.Generic.List<IntPtr> native_arg_list = new System.Collections.Generic.List<IntPtr>();
			for (int i = 0; i < cnt_argv; i++)
				native_arg_list.Add(GLib.Marshaller.StringToPtrGStrdup(argv[i]));
			IntPtr[] native_argv = native_arg_list.ToArray();
			gst_init(ref cnt_argv, ref native_argv);
			foreach (var native_arg in native_arg_list)
				GLib.Marshaller.Free(native_arg);

			InitializeStaticPlugins();
			CheckVersion();
		}

		[DllImport("gstreamer-1.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gst_init_check(ref int argc, ref IntPtr[] argv, out IntPtr error);

		[DllImport("gstreamer-1.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern bool gst_init_check(IntPtr argc, IntPtr argv, out IntPtr error);

		public static bool InitCheck() {
			IntPtr error = IntPtr.Zero;
			bool ret = gst_init_check(IntPtr.Zero, IntPtr.Zero, out error);

			if (ret && error == IntPtr.Zero)
				InitializeStaticPlugins();

			CheckVersion();
			if (error != IntPtr.Zero) throw new GLib.GException(error);
			return ret;
		}

		public static bool InitCheck(ref string[] argv) {
			int cnt_argv = argv == null ? 0 : argv.Length;
			System.Collections.Generic.List<IntPtr> native_arg_list = new System.Collections.Generic.List<IntPtr>();
			for (int i = 0; i < cnt_argv; i++)
				native_arg_list.Add(GLib.Marshaller.StringToPtrGStrdup(argv[i]));
			IntPtr[] native_argv = native_arg_list.ToArray();
			IntPtr error = IntPtr.Zero;
			bool ret = gst_init_check(ref cnt_argv, ref native_argv, out error);
			foreach (var native_arg in native_arg_list)
				GLib.Marshaller.Free(native_arg);
			if (error != IntPtr.Zero) throw new GLib.GException(error);

			if (ret)
				InitializeStaticPlugins();

			CheckVersion();
			return ret;
		}
	}
}
