// Local override of gstreamer-sharp's generated GtkSharp/ObjectManager.cs
// (1.28.8); the upstream copy is excluded from compilation in
// codecrush.gstreamer.net.csproj.
//
// Differences from upstream:
//  * every registration is attempted on its own instead of the whole list
//    running unguarded. Upstream evaluates X.GType for all 89 generated object
//    types, and every wrapper class's static constructor calls this method, so
//    one module the runtime does not contain breaks all of them: on a gst-full
//    build without WebRTC and RTSP server, touching Gst.App.AppSink died with
//    DllNotFoundException: gstwebrtc-1.0-0.dll before AppSink even finished
//    initialising.
//
// A type whose native module is missing now stays unregistered. That only
// costs the most-derived managed wrapper for objects of that type, and using
// the type directly still fails with the module named. Any other failure,
// including a TypeInitializationException with a different cause, propagates.
//
// Keep the list in sync with upstream: ObjectManagerCoverageTest compares both
// files and fails when a type is missing here.

namespace GtkSharp.GstreamerSharp {

	using System;

	public static class ObjectManager {

		static bool initialized = false;

		// Call this method from the appropriate module init function.
		public static void Initialize () {
			if (initialized)
				return;

			initialized = true;
			Register (static () => Gst.App.AppSink.GType, typeof (Gst.App.AppSink));
			Register (static () => Gst.App.AppSrc.GType, typeof (Gst.App.AppSrc));
			Register (static () => Gst.Audio.AudioBaseSink.GType, typeof (Gst.Audio.AudioBaseSink));
			Register (static () => Gst.Audio.AudioBaseSrc.GType, typeof (Gst.Audio.AudioBaseSrc));
			Register (static () => Gst.Audio.AudioCdSrc.GType, typeof (Gst.Audio.AudioCdSrc));
			Register (static () => Gst.Audio.AudioClock.GType, typeof (Gst.Audio.AudioClock));
			Register (static () => Gst.Audio.AudioDecoder.GType, typeof (Gst.Audio.AudioDecoder));
			Register (static () => Gst.Audio.AudioEncoder.GType, typeof (Gst.Audio.AudioEncoder));
			Register (static () => Gst.Audio.AudioFilter.GType, typeof (Gst.Audio.AudioFilter));
			Register (static () => Gst.Audio.AudioRingBuffer.GType, typeof (Gst.Audio.AudioRingBuffer));
			Register (static () => Gst.Audio.AudioSink.GType, typeof (Gst.Audio.AudioSink));
			Register (static () => Gst.Audio.AudioSrc.GType, typeof (Gst.Audio.AudioSrc));
			Register (static () => Gst.Base.Adapter.GType, typeof (Gst.Base.Adapter));
			Register (static () => Gst.Base.Aggregator.GType, typeof (Gst.Base.Aggregator));
			Register (static () => Gst.Base.AggregatorPad.GType, typeof (Gst.Base.AggregatorPad));
			Register (static () => Gst.Base.BaseParse.GType, typeof (Gst.Base.BaseParse));
			Register (static () => Gst.Base.BaseSink.GType, typeof (Gst.Base.BaseSink));
			Register (static () => Gst.Base.BaseSrc.GType, typeof (Gst.Base.BaseSrc));
			Register (static () => Gst.Base.BaseTransform.GType, typeof (Gst.Base.BaseTransform));
			Register (static () => Gst.Base.CollectPads.GType, typeof (Gst.Base.CollectPads));
			Register (static () => Gst.Base.DataQueue.GType, typeof (Gst.Base.DataQueue));
			Register (static () => Gst.Base.PushSrc.GType, typeof (Gst.Base.PushSrc));
			Register (static () => Gst.Controller.ARGBControlBinding.GType, typeof (Gst.Controller.ARGBControlBinding));
			Register (static () => Gst.Controller.DirectControlBinding.GType, typeof (Gst.Controller.DirectControlBinding));
			Register (static () => Gst.Controller.InterpolationControlSource.GType, typeof (Gst.Controller.InterpolationControlSource));
			Register (static () => Gst.Controller.LFOControlSource.GType, typeof (Gst.Controller.LFOControlSource));
			Register (static () => Gst.Controller.ProxyControlBinding.GType, typeof (Gst.Controller.ProxyControlBinding));
			Register (static () => Gst.Controller.TimedValueControlSource.GType, typeof (Gst.Controller.TimedValueControlSource));
			Register (static () => Gst.Controller.TriggerControlSource.GType, typeof (Gst.Controller.TriggerControlSource));
			Register (static () => Gst.Net.NetClientClock.GType, typeof (Gst.Net.NetClientClock));
			Register (static () => Gst.Net.NetTimeProvider.GType, typeof (Gst.Net.NetTimeProvider));
			Register (static () => Gst.Net.PtpClock.GType, typeof (Gst.Net.PtpClock));
			Register (static () => Gst.PbUtils.AudioVisualizer.GType, typeof (Gst.PbUtils.AudioVisualizer));
			Register (static () => Gst.PbUtils.Discoverer.GType, typeof (Gst.PbUtils.Discoverer));
			Register (static () => Gst.PbUtils.DiscovererAudioInfo.GType, typeof (Gst.PbUtils.DiscovererAudioInfo));
			Register (static () => Gst.PbUtils.DiscovererContainerInfo.GType, typeof (Gst.PbUtils.DiscovererContainerInfo));
			Register (static () => Gst.PbUtils.DiscovererInfo.GType, typeof (Gst.PbUtils.DiscovererInfo));
			Register (static () => Gst.PbUtils.DiscovererStreamInfo.GType, typeof (Gst.PbUtils.DiscovererStreamInfo));
			Register (static () => Gst.PbUtils.DiscovererSubtitleInfo.GType, typeof (Gst.PbUtils.DiscovererSubtitleInfo));
			Register (static () => Gst.PbUtils.DiscovererVideoInfo.GType, typeof (Gst.PbUtils.DiscovererVideoInfo));
			Register (static () => Gst.PbUtils.EncodingAudioProfile.GType, typeof (Gst.PbUtils.EncodingAudioProfile));
			Register (static () => Gst.PbUtils.EncodingContainerProfile.GType, typeof (Gst.PbUtils.EncodingContainerProfile));
			Register (static () => Gst.PbUtils.EncodingProfile.GType, typeof (Gst.PbUtils.EncodingProfile));
			Register (static () => Gst.PbUtils.EncodingTarget.GType, typeof (Gst.PbUtils.EncodingTarget));
			Register (static () => Gst.PbUtils.EncodingVideoProfile.GType, typeof (Gst.PbUtils.EncodingVideoProfile));
			Register (static () => Gst.Rtp.RTPBaseAudioPayload.GType, typeof (Gst.Rtp.RTPBaseAudioPayload));
			Register (static () => Gst.Rtp.RTPBaseDepayload.GType, typeof (Gst.Rtp.RTPBaseDepayload));
			Register (static () => Gst.Rtp.RTPBasePayload.GType, typeof (Gst.Rtp.RTPBasePayload));
			Register (static () => Gst.Rtp.RTPHeaderExtension.GType, typeof (Gst.Rtp.RTPHeaderExtension));
			Register (static () => Gst.Tags.TagDemux.GType, typeof (Gst.Tags.TagDemux));
			Register (static () => Gst.Tags.TagMux.GType, typeof (Gst.Tags.TagMux));
			Register (static () => Gst.Video.ColorBalanceChannel.GType, typeof (Gst.Video.ColorBalanceChannel));
			Register (static () => Gst.Video.VideoAggregator.GType, typeof (Gst.Video.VideoAggregator));
			Register (static () => Gst.Video.VideoAggregatorConvertPad.GType, typeof (Gst.Video.VideoAggregatorConvertPad));
			Register (static () => Gst.Video.VideoAggregatorPad.GType, typeof (Gst.Video.VideoAggregatorPad));
			Register (static () => Gst.Video.VideoAggregatorParallelConvertPad.GType, typeof (Gst.Video.VideoAggregatorParallelConvertPad));
			Register (static () => Gst.Video.VideoBufferPool.GType, typeof (Gst.Video.VideoBufferPool));
			Register (static () => Gst.Video.VideoDecoder.GType, typeof (Gst.Video.VideoDecoder));
			Register (static () => Gst.Video.VideoDmabufPool.GType, typeof (Gst.Video.VideoDmabufPool));
			Register (static () => Gst.Video.VideoEncoder.GType, typeof (Gst.Video.VideoEncoder));
			Register (static () => Gst.Video.VideoFilter.GType, typeof (Gst.Video.VideoFilter));
			Register (static () => Gst.Video.VideoSink.GType, typeof (Gst.Video.VideoSink));
			Register (static () => Gst.WebRTC.WebRTCDTLSTransport.GType, typeof (Gst.WebRTC.WebRTCDTLSTransport));
			Register (static () => Gst.WebRTC.WebRTCDataChannel.GType, typeof (Gst.WebRTC.WebRTCDataChannel));
			Register (static () => Gst.WebRTC.WebRTCICE.GType, typeof (Gst.WebRTC.WebRTCICE));
			Register (static () => Gst.WebRTC.WebRTCICEStream.GType, typeof (Gst.WebRTC.WebRTCICEStream));
			Register (static () => Gst.WebRTC.WebRTCICETransport.GType, typeof (Gst.WebRTC.WebRTCICETransport));
			Register (static () => Gst.WebRTC.WebRTCRTPReceiver.GType, typeof (Gst.WebRTC.WebRTCRTPReceiver));
			Register (static () => Gst.WebRTC.WebRTCRTPSender.GType, typeof (Gst.WebRTC.WebRTCRTPSender));
			Register (static () => Gst.WebRTC.WebRTCRTPTransceiver.GType, typeof (Gst.WebRTC.WebRTCRTPTransceiver));
			Register (static () => Gst.WebRTC.WebRTCSCTPTransport.GType, typeof (Gst.WebRTC.WebRTCSCTPTransport));
			Register (static () => Gst.RtspServer.RTSPAddressPool.GType, typeof (Gst.RtspServer.RTSPAddressPool));
			Register (static () => Gst.RtspServer.RTSPAuth.GType, typeof (Gst.RtspServer.RTSPAuth));
			Register (static () => Gst.RtspServer.RTSPClient.GType, typeof (Gst.RtspServer.RTSPClient));
			Register (static () => Gst.RtspServer.RTSPMedia.GType, typeof (Gst.RtspServer.RTSPMedia));
			Register (static () => Gst.RtspServer.RTSPMediaFactory.GType, typeof (Gst.RtspServer.RTSPMediaFactory));
			Register (static () => Gst.RtspServer.RTSPMediaFactoryURI.GType, typeof (Gst.RtspServer.RTSPMediaFactoryURI));
			Register (static () => Gst.RtspServer.RTSPMountPoints.GType, typeof (Gst.RtspServer.RTSPMountPoints));
			Register (static () => Gst.RtspServer.RTSPOnvifClient.GType, typeof (Gst.RtspServer.RTSPOnvifClient));
			Register (static () => Gst.RtspServer.RTSPOnvifMedia.GType, typeof (Gst.RtspServer.RTSPOnvifMedia));
			Register (static () => Gst.RtspServer.RTSPOnvifMediaFactory.GType, typeof (Gst.RtspServer.RTSPOnvifMediaFactory));
			Register (static () => Gst.RtspServer.RTSPOnvifServer.GType, typeof (Gst.RtspServer.RTSPOnvifServer));
			Register (static () => Gst.RtspServer.RTSPServer.GType, typeof (Gst.RtspServer.RTSPServer));
			Register (static () => Gst.RtspServer.RTSPSession.GType, typeof (Gst.RtspServer.RTSPSession));
			Register (static () => Gst.RtspServer.RTSPSessionMedia.GType, typeof (Gst.RtspServer.RTSPSessionMedia));
			Register (static () => Gst.RtspServer.RTSPSessionPool.GType, typeof (Gst.RtspServer.RTSPSessionPool));
			Register (static () => Gst.RtspServer.RTSPStream.GType, typeof (Gst.RtspServer.RTSPStream));
			Register (static () => Gst.RtspServer.RTSPStreamTransport.GType, typeof (Gst.RtspServer.RTSPStreamTransport));
			Register (static () => Gst.RtspServer.RTSPThreadPool.GType, typeof (Gst.RtspServer.RTSPThreadPool));
		}

		/// <summary>
		/// Registers the managed type for the GType its native module reports.
		/// </summary>
		/// <remarks>
		/// The GType is read through a call rather than passed in, so that the
		/// native get_type() call - and the module load behind it - happens
		/// inside the guard. typeof() alone never runs a static constructor.
		/// </remarks>
		static void Register (Func<GLib.GType> gtype, Type managed) {
			try {
				GLib.GType.Register (gtype (), managed);
			} catch (Exception ex) when (IsModuleUnavailable (ex)) {
			}
		}

		static bool IsModuleUnavailable (Exception ex) {
			return ex is DllNotFoundException or EntryPointNotFoundException
				|| ex is TypeInitializationException { InnerException: DllNotFoundException or EntryPointNotFoundException };
		}
	}
}
