using UnityEngine;
using System;
using System.Collections.Generic;

//-----------------------------------------------------------------------------
// Copyright 2015-2022 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	public partial class MediaPlayer : MonoBehaviour
	{
#region PlatformOptions
		[System.Serializable]
		public class PlatformOptions
		{
			public enum TextureFormat : int
			{
				BGRA,
				YCbCr420_OES,
				// Restored the old name from v2 as this allows the old AppleMediaPlayer.cs script to compile when
				// upgrading to v3
				[Obsolete] YCbCr420 = YCbCr420_OES,
			}

			public enum Resolution : int
			{
				NoPreference,
				_480p,
				_720p,
				_1080p,
				_1440p,
				_2160p,
				Custom
			}

			public enum AudioMode : int
			{
				SystemDirect,
				Unity,
				SystemDirectWithCapture,
				FacebookAudio360,
			}

			public enum BitRateUnits : int
			{
				bps,
				Kbps,
				Mbps
			}

			public virtual bool IsModified()
			{
				return (httpHeaders.IsModified()
				|| keyAuth.IsModified()
				);
			}

			public virtual bool HasChanged()
			{
				return false;
			}

			public virtual void ClearChanges()
			{

			}

			public HttpHeaderData httpHeaders = new HttpHeaderData();
			public KeyAuthData keyAuth = new KeyAuthData();

			// Decryption support
			public virtual string GetKeyServerAuthToken() { return keyAuth.keyServerToken; }
			//public virtual string GetKeyServerURL() { return null; }
			public virtual byte[] GetOverrideDecryptionKey() { return keyAuth.overrideDecryptionKey; }

			public virtual bool StartWithHighestBandwidth() { return false; }
		}

		[System.Serializable]
		public class OptionsWindows : PlatformOptions, ISerializationCallbackReceiver
		{
			public Windows.VideoApi videoApi = Windows.VideoApi.MediaFoundation;
			public bool useHardwareDecoding = true;
			public bool useRendererSync = true;
			public bool useTextureMips = false;
			public bool use10BitTextures = false;
			public bool hintAlphaChannel = false;
			public bool useLowLatency = false;
			public bool useCustomMovParser = false;
			public bool useHapNotchLC = false;
			public bool useStereoDetection = true;
			public bool useTextTrackSupport = true;
			public bool useFacebookAudio360Support = true;
			public bool useAudioDelay = false;
			public string forceAudioOutputDeviceName = string.Empty;
			public List<string> preferredFilters = new List<string>();
			public Windows.AudioOutput _audioMode = Windows.AudioOutput.System;
			public Audio360ChannelMode audio360ChannelMode = Audio360ChannelMode.TBE_8_2;

			/// WinRT only
			public bool startWithHighestBitrate = false;

			/// WinRT only
			public bool useLowLiveLatency = false;

			/// Hap & NotchLC only
			[Range(1, 16)]
			public int parallelFrameCount = 3;
			/// Hap & NotchLC only
			[Range(1, 16)]
			public int prerollFrameCount = 4;

			public override bool IsModified()
			{
				return (base.IsModified()
				|| !useHardwareDecoding
				|| !useRendererSync
				|| useTextureMips
				|| use10BitTextures
				|| hintAlphaChannel
				|| useLowLatency
				|| useCustomMovParser
				|| useHapNotchLC
				|| !useStereoDetection
				|| !useTextTrackSupport
				|| !useFacebookAudio360Support
				|| useAudioDelay
				|| videoApi != Windows.VideoApi.MediaFoundation
				|| _audioMode != Windows.AudioOutput.System
				|| audio360ChannelMode != Audio360ChannelMode.TBE_8_2
				|| !string.IsNullOrEmpty(forceAudioOutputDeviceName)
				|| preferredFilters.Count != 0
				|| startWithHighestBitrate
				|| useLowLiveLatency
				|| parallelFrameCount != 3
				|| prerollFrameCount != 4
				);
			}

			public override bool StartWithHighestBandwidth() { return startWithHighestBitrate; }

			#region Upgrade from Version 1.x
			[SerializeField, HideInInspector]
			private bool useUnityAudio = false;
			[SerializeField, HideInInspector]
			private bool enableAudio360 = false;

			void ISerializationCallbackReceiver.OnBeforeSerialize() { }

			void ISerializationCallbackReceiver.OnAfterDeserialize()
			{
				if (useUnityAudio && _audioMode == Windows.AudioOutput.System)
				{
					_audioMode = Windows.AudioOutput.Unity;
					useUnityAudio = false;
				}
				if (enableAudio360 && _audioMode == Windows.AudioOutput.System)
				{
					_audioMode = Windows.AudioOutput.FacebookAudio360;
					enableAudio360 = false;
				}
			}
			#endregion	// Upgrade from Version 1.x
		}

		[System.Serializable]
		public class OptionsWindowsUWP : PlatformOptions
		{
			public bool useHardwareDecoding = true;
			public bool useRendererSync = true;
			public bool useTextureMips = false;
			public bool use10BitTextures = false;
			public bool hintOutput10Bit = false;
			public bool useLowLatency = false;
			public WindowsUWP.VideoApi videoApi = WindowsUWP.VideoApi.WinRT;
			public WindowsUWP.AudioOutput _audioMode = WindowsUWP.AudioOutput.System;
			public Audio360ChannelMode audio360ChannelMode = Audio360ChannelMode.TBE_8_2;

			/// WinRT only
			public bool startWithHighestBitrate = false;

			/// WinRT only
			public bool useLowLiveLatency = false;

			public override bool IsModified()
			{
				return (base.IsModified()
				|| !useHardwareDecoding
				|| !useRendererSync
				|| useTextureMips
				|| use10BitTextures
				|| useLowLatency
				|| _audioMode != WindowsUWP.AudioOutput.System
				|| (audio360ChannelMode != Audio360ChannelMode.TBE_8_2)
				|| videoApi != WindowsUWP.VideoApi.WinRT
				|| startWithHighestBitrate
				|| useLowLiveLatency
				);
			}

			public override bool StartWithHighestBandwidth() { return startWithHighestBitrate; }
		}

		[System.Serializable]
		public class OptionsApple: PlatformOptions
		{
			[Flags]
			public enum Flags: int
			{
				// Common
				None = 0,
				GenerateMipMaps = 1 << 0,

				// iOS & macOS
				AllowExternalPlayback = 1 <<  8,
				PlayWithoutBuffering  = 1 <<  9,
				UseSinglePlayerItem   = 1 << 10,

				// iOS
				ResumeMediaPlaybackAfterAudioSessionRouteChange = 1 << 16,
			}

			private readonly TextureFormat DefaultTextureFormat;
			private readonly Flags DefaultFlags;
			public TextureFormat textureFormat;
			
			private AudioMode _previousAudioMode = AudioMode.SystemDirect;
			public AudioMode previousAudioMode
			{
				get { return _previousAudioMode; }
			}

			[SerializeField]
			private AudioMode _audioMode;
			public AudioMode audioMode
			{
				get { return _audioMode; }
				set
				{
					if (_audioMode != value)
					{
						_previousAudioMode = _audioMode;
						_audioMode = value;
						_changed |= ChangeFlags.AudioMode;
					}
				}
			}

			[SerializeField]
			private Flags _flags;
			public Flags flags
			{
				get { return _flags; }
				set
				{
					Flags changed = _flags ^ value;
					if (changed != 0)
					{
						if ((changed & Flags.PlayWithoutBuffering) == Flags.PlayWithoutBuffering)
						{
							_changed |= ChangeFlags.PlayWithoutBuffering;
						}
						if ((changed & Flags.ResumeMediaPlaybackAfterAudioSessionRouteChange) == Flags.ResumeMediaPlaybackAfterAudioSessionRouteChange)
						{
							_changed |= ChangeFlags.ResumeMediaPlaybackAfterAudioSessionRouteChange;
						}
						_flags = value;
					}
				}
			}

			public float maximumPlaybackRate = 2.0f;

			[Flags]
			public enum ChangeFlags: int
			{
				None                                            = 0,
				PreferredPeakBitRate                            = 1 << 1,
				PreferredForwardBufferDuration                  = 1 << 2,
				PlayWithoutBuffering                            = 1 << 3,
				PreferredMaximumResolution                      = 1 << 4,
				AudioMode                                       = 1 << 5,
				ResumeMediaPlaybackAfterAudioSessionRouteChange = 1 << 6,
				All = -1
			}

			private ChangeFlags _changed = ChangeFlags.None;

			[SerializeField]
			private float _preferredPeakBitRate = 0.0f;
			public float preferredPeakBitRate
			{
				get { return _preferredPeakBitRate; }
				set
				{
					if (_preferredPeakBitRate != value)
					{
						_changed |= ChangeFlags.PreferredPeakBitRate;
						_preferredPeakBitRate = value;
					}
				}
			}

			[SerializeField]
			private BitRateUnits _preferredPeakBitRateUnits = BitRateUnits.Kbps;
			public BitRateUnits preferredPeakBitRateUnits
			{
				get { return _preferredPeakBitRateUnits; }
				set
				{
					if (_preferredPeakBitRateUnits != value)
					{
						_changed |= ChangeFlags.PreferredPeakBitRate;
						_preferredPeakBitRateUnits = value;
					}
				}
			}

			[SerializeField]
			private double _preferredForwardBufferDuration = 0.0;
			public double preferredForwardBufferDuration
			{
				get
				{
					return _preferredForwardBufferDuration;
				}
				set
				{
					if (_preferredForwardBufferDuration != value)
					{
						_changed |= ChangeFlags.PreferredForwardBufferDuration;
						_preferredForwardBufferDuration = value;
					}
				}
			}

			[SerializeField]
			private Resolution _preferredMaximumResolution = Resolution.NoPreference;
			public Resolution preferredMaximumResolution
			{
				get
				{
					return _preferredMaximumResolution;
				}
				set
				{
					if (_preferredMaximumResolution != value)
					{
						_changed |= ChangeFlags.PreferredMaximumResolution;
						_preferredMaximumResolution = value;
					}
				}
			}

#if UNITY_2017_2_OR_NEWER
			[SerializeField]
			private Vector2Int _customPreferredMaximumResolution = Vector2Int.zero;
			public Vector2Int customPreferredMaximumResolution
			{
				get
				{
					return _customPreferredMaximumResolution;
				}
				set
				{
					if (_customPreferredMaximumResolution != value)
					{
						_changed |= ChangeFlags.PreferredMaximumResolution;
						_customPreferredMaximumResolution = value;
					}
				}
			}
#endif

			private static double BitRateInBitsPerSecond(float value, BitRateUnits units)
			{
				switch (units)
				{
					case BitRateUnits.bps:
						return (double)value;
					case BitRateUnits.Kbps:
						return (double)value * 1000.0;
					case BitRateUnits.Mbps:
						return (double)value * 1000000.0;
					default:
						return 0.0;
				}
			}

			public double GetPreferredPeakBitRateInBitsPerSecond()
			{
				return BitRateInBitsPerSecond(preferredPeakBitRate, preferredPeakBitRateUnits);
			}

			public OptionsApple(TextureFormat defaultTextureFormat, Flags defaultFlags)
			{
				DefaultTextureFormat = defaultTextureFormat;
				DefaultFlags = defaultFlags;
				textureFormat = defaultTextureFormat;
				audioMode = AudioMode.SystemDirect;
				flags = defaultFlags;
			}

			public override bool IsModified()
			{
				return base.IsModified()
					|| textureFormat != DefaultTextureFormat
					|| audioMode != AudioMode.SystemDirect
					|| flags != DefaultFlags
					|| preferredMaximumResolution != Resolution.NoPreference
					|| preferredPeakBitRate != 0.0f
					|| preferredForwardBufferDuration != 0.0;
			}

			public override bool HasChanged()
			{
				return HasChanged(ChangeFlags.All);
			}

			public bool HasChanged(ChangeFlags flags)
			{
				return (_changed & flags) != ChangeFlags.None;
			}

			public override void ClearChanges()
			{
				_changed = ChangeFlags.None;
			}
		}

		[System.Serializable]
		public class OptionsAndroid : PlatformOptions, ISerializationCallbackReceiver
		{
			[Flags]
			public enum ChangeFlags : int
			{
				None = 0,
				PreferredPeakBitRate			= 1 << 1,
				PreferredMaximumResolution		= 1 << 2,
				PreferredCustomResolution		= 1 << 3,
				AudioMode						= 1 << 4,
				GenerateMipmaps                 = 1 << 5,
				All = -1
			}

			private ChangeFlags _changed = ChangeFlags.None;

			private readonly TextureFormat DefaultTextureFormat;
			public TextureFormat textureFormat;

			[SerializeField]
			private bool _generateMipmaps = false;
			public bool generateMipmaps
			{
				get
				{
					return _generateMipmaps;
				}
				set
				{
					if (value != _generateMipmaps)
					{
						_generateMipmaps = value;
						_changed |= ChangeFlags.GenerateMipmaps;
					}
				}
			}

			private AudioMode _previousAudioMode = AudioMode.SystemDirect;
			public AudioMode previousAudioMode
			{
				get { return _previousAudioMode; }
			}

			[SerializeField]
			private AudioMode _audioMode;
			public AudioMode audioMode
			{
				get { return _audioMode; }
				set
				{
					if (_audioMode != value)
					{
						_previousAudioMode = _audioMode;
						_audioMode = value;
						_changed |= ChangeFlags.AudioMode;
					}
				}
			}

			[SerializeField]
			private Resolution _preferredMaximumResolution = Resolution.NoPreference;
			public Resolution preferredMaximumResolution
			{
				get { return _preferredMaximumResolution; }
				set
				{
					if (_preferredMaximumResolution != value)
					{
						_changed |= ChangeFlags.PreferredMaximumResolution;
						_preferredMaximumResolution = value;
					}
				}
			}

#if UNITY_2017_2_OR_NEWER
			[SerializeField]
			private Vector2Int _customPreferredMaximumResolution = Vector2Int.zero;
			public Vector2Int customPreferredMaximumResolution
			{
				get { return _customPreferredMaximumResolution; }
				set
				{
					if (_customPreferredMaximumResolution != value)
					{
						_changed |= ChangeFlags.PreferredCustomResolution;
						_customPreferredMaximumResolution = value;
					}
				}
			}
#endif

			[SerializeField]
			private float _preferredPeakBitRate = 0.0f;
			public float preferredPeakBitRate
			{
				get { return _preferredPeakBitRate; }
				set
				{
					if (_preferredPeakBitRate != value)
					{
						_changed |= ChangeFlags.PreferredPeakBitRate;
						_preferredPeakBitRate = value;
					}
				}
			}

			[SerializeField]
			private BitRateUnits _preferredPeakBitRateUnits = BitRateUnits.Kbps;
			public BitRateUnits preferredPeakBitRateUnits
			{
				get { return _preferredPeakBitRateUnits; }
				set
				{
					if (_preferredPeakBitRateUnits != value)
					{
						_changed |= ChangeFlags.PreferredPeakBitRate;
						_preferredPeakBitRateUnits = value;
					}
				}
			}


			public Android.VideoApi videoApi = Android.VideoApi.ExoPlayer;
			public bool showPosterFrame = false;	// NOTE 2024.09.26: DEPRECATED
			public Audio360ChannelMode audio360ChannelMode = Audio360ChannelMode.TBE_8_2;
			public int audio360LatencyMS = 0;
			public bool preferSoftwareDecoder = false;
			public bool forceRtpTCP = false;
			public bool forceEnableMediaCodecAsynchronousQueueing = false;

			[SerializeField, Tooltip("Byte offset into the file where the media file is located.  This is useful when hiding or packing media files within another file.")]
			public int fileOffset = 0;

			public bool startWithHighestBitrate = false;

			public int minBufferMs							= Android.Default_MinBufferTimeMs;
			public int maxBufferMs							= Android.Default_MaxBufferTimeMs;
			public int bufferForPlaybackMs					= Android.Default_BufferForPlaybackMs;
			public int bufferForPlaybackAfterRebufferMs		= Android.Default_BufferForPlaybackAfterRebufferMs;

			[Obsolete("useFastOesPath is deprecated and replaced with TextureFormat")]
			public bool useFastOesPath;
			[Obsolete("audioOutput is deprecated and replaced with audioMode")]
			public int audioOutput;
			[Obsolete("blitTextureFiltering is deprecated and its functionality has been removed")]
			public int blitTextureFiltering;
			[Obsolete("forceEnableMediaCodecAsyncQueueing is deprecated and replaced with forceEnableMediaCodecAsynchronousQueueing")]
			public bool forceEnableMediaCodecAsyncQueueing;

			public override bool IsModified()
			{
				return (base.IsModified()
					|| (fileOffset != 0)
					|| textureFormat != DefaultTextureFormat
					|| audioMode != AudioMode.SystemDirect
//					|| showPosterFrame
					|| (videoApi != Android.VideoApi.ExoPlayer)
					|| (audio360ChannelMode != Audio360ChannelMode.TBE_8_2)
					|| (audio360LatencyMS != 0 )
					|| preferSoftwareDecoder
					|| forceRtpTCP
					|| forceEnableMediaCodecAsynchronousQueueing
					|| startWithHighestBitrate
					|| (minBufferMs != Android.Default_MinBufferTimeMs)
					|| (maxBufferMs != Android.Default_MaxBufferTimeMs)
					|| (bufferForPlaybackMs != Android.Default_BufferForPlaybackMs)
					|| (bufferForPlaybackAfterRebufferMs != Android.Default_BufferForPlaybackAfterRebufferMs)
					|| (preferredMaximumResolution != Resolution.NoPreference)
					|| (preferredPeakBitRate != 0.0f)
				);
			}

			private static double BitRateInBitsPerSecond(float value, BitRateUnits units)
			{
				switch (units)
				{
					case BitRateUnits.bps:
						return (double)value;
					case BitRateUnits.Kbps:
						return (double)value * 1000.0;
					case BitRateUnits.Mbps:
						return (double)value * 1000000.0;
					default:
						return 0.0;
				}
			}

			public double GetPreferredPeakBitRateInBitsPerSecond()
			{
				_changed &= ~ChangeFlags.PreferredPeakBitRate;
				return BitRateInBitsPerSecond(preferredPeakBitRate, preferredPeakBitRateUnits);
			}

			public override bool StartWithHighestBandwidth()
			{
				return startWithHighestBitrate;
			}

			public override bool HasChanged()
			{
				return HasChanged(ChangeFlags.All, false);
			}
			
			public bool HasChanged(ChangeFlags flags, bool bClearFlags = false)
			{
				bool bReturn = ((_changed & flags) != ChangeFlags.None);
				if (bClearFlags)
				{
					_changed = ChangeFlags.None;
				}
				return bReturn;
			}

			public override void ClearChanges()
			{
				_changed = ChangeFlags.None;
			}

			#region Upgrade from Version 1.x
			[SerializeField, HideInInspector]
			private bool enableAudio360 = false;

			void ISerializationCallbackReceiver.OnBeforeSerialize()	{ }

			void ISerializationCallbackReceiver.OnAfterDeserialize()
			{
#if false
				if (enableAudio360 && audioOutput == Android.AudioOutput.System)
				{
					audioOutput = Android.AudioOutput.FacebookAudio360;
					enableAudio360 = false;
				}
#else
				if (enableAudio360 && audioMode == AudioMode.SystemDirect)
				{
					audioMode = AudioMode.FacebookAudio360;
					enableAudio360 = false;
				}
#endif
			}
			#endregion	// Upgrade from Version 1.x
		}

		[System.Serializable]
		public class OptionsOpenHarmony : PlatformOptions, ISerializationCallbackReceiver
		{
			[Flags]
			public enum ChangeFlags : int
			{
				None = 0,
				PreferredPeakBitRate = 1 << 1,
				PreferredMaximumResolution = 1 << 2,
				PreferredCustomResolution = 1 << 3,
				AudioMode = 1 << 4,
				GenerateMipmaps = 1 << 5,
				All = -1
			}

			private ChangeFlags _changed = ChangeFlags.None;

			private readonly TextureFormat DefaultTextureFormat;
			public TextureFormat textureFormat;

			[SerializeField]
			private bool _generateMipmapsOH = false;
			public bool generateMipmaps
			{
				get
				{
					return _generateMipmapsOH;
				}
				set
				{
					if ( value != _generateMipmapsOH )
					{
						_generateMipmapsOH = value;
						_changed |= ChangeFlags.GenerateMipmaps;
					}
				}
			}

			private AudioMode _previousAudioMode = AudioMode.SystemDirect;
			public AudioMode previousAudioMode
			{
				get { return _previousAudioMode; }
			}

			[SerializeField]
			private AudioMode _audioMode;
			public AudioMode audioMode
			{
				get { return _audioMode; }
				set
				{
					if ( _audioMode != value )
					{
						_previousAudioMode = _audioMode;
						_audioMode = value;
						_changed |= ChangeFlags.AudioMode;
					}
				}
			}

			[SerializeField]
			private Resolution _preferredMaximumResolution = Resolution.NoPreference;
			public Resolution preferredMaximumResolution
			{
				get { return _preferredMaximumResolution; }
				set
				{
					if ( _preferredMaximumResolution != value )
					{
						_changed |= ChangeFlags.PreferredMaximumResolution;
						_preferredMaximumResolution = value;
					}
				}
			}

#if UNITY_2017_2_OR_NEWER
			[SerializeField]
			private Vector2Int _customPreferredMaximumResolution = Vector2Int.zero;
			public Vector2Int customPreferredMaximumResolution
			{
				get { return _customPreferredMaximumResolution; }
				set
				{
					if ( _customPreferredMaximumResolution != value )
					{
						_changed |= ChangeFlags.PreferredCustomResolution;
						_customPreferredMaximumResolution = value;
					}
				}
			}
#endif

/*
			[SerializeField]
			private float _preferredPeakBitRate = 0.0f;
			public float preferredPeakBitRate
			{
				get { return _preferredPeakBitRate; }
				set
				{
					if ( _preferredPeakBitRate != value )
					{
						_changed |= ChangeFlags.PreferredPeakBitRate;
						_preferredPeakBitRate = value;
					}
				}
			}

			[SerializeField]
			private BitRateUnits _preferredPeakBitRateUnits = BitRateUnits.Kbps;
			public BitRateUnits preferredPeakBitRateUnits
			{
				get { return _preferredPeakBitRateUnits; }
				set
				{
					if ( _preferredPeakBitRateUnits != value )
					{
						_changed |= ChangeFlags.PreferredPeakBitRate;
						_preferredPeakBitRateUnits = value;
					}
				}
			}
*/

//			[SerializeField, Tooltip("Byte offset into the file where the media file is located.  This is useful when hiding or packing media files within another file.")]
//			public int fileOffset = 0;

//			public bool startWithHighestBitrate = false;

//			public int minBufferMs = Android.Default_MinBufferTimeMs;
//			public int maxBufferMs = Android.Default_MaxBufferTimeMs;
//			public int bufferForPlaybackMs = Android.Default_BufferForPlaybackMs;
//			public int bufferForPlaybackAfterRebufferMs = Android.Default_BufferForPlaybackAfterRebufferMs;

			public override bool IsModified()
			{
				return ( base.IsModified()
//					|| ( fileOffset != 0 )
					|| textureFormat != DefaultTextureFormat
					|| audioMode != AudioMode.SystemDirect
//					|| showPosterFrame
//					|| startWithHighestBitrate
//					|| ( minBufferMs != Android.Default_MinBufferTimeMs )
//					|| ( maxBufferMs != Android.Default_MaxBufferTimeMs )
//					|| ( bufferForPlaybackMs != Android.Default_BufferForPlaybackMs )
//					|| ( bufferForPlaybackAfterRebufferMs != Android.Default_BufferForPlaybackAfterRebufferMs )
//					|| ( preferredMaximumResolution != Resolution.NoPreference )
//					|| ( preferredPeakBitRate != 0.0f )
				);
			}

			private static double BitRateInBitsPerSecond(float value, BitRateUnits units)
			{
				switch ( units )
				{
					case BitRateUnits.bps:
						return (double)value;
					case BitRateUnits.Kbps:
						return (double)value * 1000.0;
					case BitRateUnits.Mbps:
						return (double)value * 1000000.0;
					default:
						return 0.0;
				}
			}

/*
			public double GetPreferredPeakBitRateInBitsPerSecond()
			{
				_changed &= ~ChangeFlags.PreferredPeakBitRate;
				return BitRateInBitsPerSecond(preferredPeakBitRate, preferredPeakBitRateUnits);
			}

			public override bool StartWithHighestBandwidth()
			{
				return startWithHighestBitrate;
			}
*/

			public override bool HasChanged()
			{
				return HasChanged(ChangeFlags.All, false);
			}

			public bool HasChanged(ChangeFlags flags, bool bClearFlags = false)
			{
				bool bReturn = ( ( _changed & flags ) != ChangeFlags.None );
				if ( bClearFlags )
				{
					_changed = ChangeFlags.None;
				}
				return bReturn;
			}

			public override void ClearChanges()
			{
				_changed = ChangeFlags.None;
			}

			void ISerializationCallbackReceiver.OnBeforeSerialize() { }

			void ISerializationCallbackReceiver.OnAfterDeserialize() { }
		}

		[System.Serializable]
		public class OptionsWebGL : PlatformOptions
		{
			public enum ChangeFlags : int
			{
				None = 0,
				PreferredPeakBitRate			= 1 << 1,
				PreferredMaximumResolution		= 1 << 2,
				PreferredCustomResolution		= 1 << 3,
				AudioMode						= 1 << 4,
				GenerateMipmaps                 = 1 << 5,
				All = -1
			}

			private ChangeFlags _changed = ChangeFlags.None;

			public WebGL.ExternalLibrary externalLibrary = WebGL.ExternalLibrary.None;
			public bool useTextureMips = false;

			private AudioMode _previousAudioMode = AudioMode.SystemDirect;
			public AudioMode previousAudioMode
			{
				get { return _previousAudioMode; }
			}

			[SerializeField]
			private AudioMode _audioMode;
			public AudioMode audioMode
			{
				get
				{
					return _audioMode;
				}
				set
				{
					if (_audioMode != value)
					{
						_previousAudioMode = _audioMode;
						_audioMode = value;
						_changed |= ChangeFlags.AudioMode;
					}
				}
			}

			public override bool IsModified()
			{
				return (base.IsModified() || externalLibrary != WebGL.ExternalLibrary.None || useTextureMips);
			}

			public override bool HasChanged()
			{
				return HasChanged(ChangeFlags.All);
			}

			public bool HasChanged(ChangeFlags flags)
			{
				return (_changed & flags) != ChangeFlags.None;
			}

			public override void ClearChanges()
			{
				_changed = ChangeFlags.None;
			}

			// Decryption support
			public override string GetKeyServerAuthToken() { return null; }
			public override byte[] GetOverrideDecryptionKey() { return null; }
		}

		// TODO: move these to a Setup object
		[SerializeField] OptionsWindows _optionsWindows = new OptionsWindows();
		[SerializeField] OptionsApple _options_macOS = new OptionsApple(OptionsApple.TextureFormat.BGRA, OptionsApple.Flags.None);
		[SerializeField] OptionsApple _options_iOS = new OptionsApple(OptionsApple.TextureFormat.BGRA, OptionsApple.Flags.None);
		[SerializeField] OptionsApple _options_tvOS = new OptionsApple(OptionsApple.TextureFormat.BGRA, OptionsApple.Flags.None);
		[SerializeField] OptionsApple _options_visionOS = new OptionsApple(OptionsApple.TextureFormat.BGRA, OptionsApple.Flags.None);
		[SerializeField] OptionsAndroid _optionsAndroid = new OptionsAndroid();
		[SerializeField] OptionsOpenHarmony _optionsOpenHarmony = new OptionsOpenHarmony();
		[SerializeField] OptionsWindowsUWP _optionsWindowsUWP = new OptionsWindowsUWP();
		[SerializeField] OptionsWebGL _optionsWebGL = new OptionsWebGL();

		public OptionsWindows PlatformOptionsWindows { get { return _optionsWindows; } }
		public OptionsApple PlatformOptions_macOS { get { return _options_macOS; } }
		public OptionsApple PlatformOptions_iOS { get { return _options_iOS; } }
		public OptionsApple PlatformOptions_tvOS { get { return _options_tvOS; } }
		public OptionsApple PlatformOptions_visionOS { get { return _options_visionOS; } }
		public OptionsAndroid PlatformOptionsAndroid { get { return _optionsAndroid; } }
		public OptionsOpenHarmony PlatformOptionsOpenHarmony { get { return _optionsOpenHarmony; } }
		public OptionsWindowsUWP PlatformOptionsWindowsUWP { get { return _optionsWindowsUWP; } }
		public OptionsWebGL PlatformOptionsWebGL { get { return _optionsWebGL; } }

#endregion // PlatformOptions
	}

#region PlatformOptionsExtensions
	public static class OptionsAppleExtensions
	{
		public static bool GenerateMipmaps(this MediaPlayer.OptionsApple.Flags flags)
		{
			return (flags & MediaPlayer.OptionsApple.Flags.GenerateMipMaps) == MediaPlayer.OptionsApple.Flags.GenerateMipMaps;
		}

		public static MediaPlayer.OptionsApple.Flags SetGenerateMipMaps(this MediaPlayer.OptionsApple.Flags flags, bool b)
		{
			if (flags.GenerateMipmaps() ^ b)
			{
				flags = b ? flags | MediaPlayer.OptionsApple.Flags.GenerateMipMaps
						  : flags & ~MediaPlayer.OptionsApple.Flags.GenerateMipMaps;
			}
			return flags;
		}

		public static bool AllowExternalPlayback(this MediaPlayer.OptionsApple.Flags flags)
		{
			return (flags & MediaPlayer.OptionsApple.Flags.AllowExternalPlayback) == MediaPlayer.OptionsApple.Flags.AllowExternalPlayback;
		}

		public static MediaPlayer.OptionsApple.Flags SetAllowExternalPlayback(this MediaPlayer.OptionsApple.Flags flags, bool b)
		{
			if (flags.AllowExternalPlayback() ^ b)
			{
				flags = b ? flags | MediaPlayer.OptionsApple.Flags.AllowExternalPlayback
						  : flags & ~MediaPlayer.OptionsApple.Flags.AllowExternalPlayback;
			}
			return flags;
		}

		public static bool PlayWithoutBuffering(this MediaPlayer.OptionsApple.Flags flags)
		{
			return (flags & MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering) == MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
		}

		public static MediaPlayer.OptionsApple.Flags SetPlayWithoutBuffering(this MediaPlayer.OptionsApple.Flags flags, bool b)
		{
			if (flags.PlayWithoutBuffering() ^ b)
			{
				flags = b ? flags | MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering
						  : flags & ~MediaPlayer.OptionsApple.Flags.PlayWithoutBuffering;
			}
			return flags;
		}

		public static bool UseSinglePlayerItem(this MediaPlayer.OptionsApple.Flags flags)
		{
			return (flags & MediaPlayer.OptionsApple.Flags.UseSinglePlayerItem) == MediaPlayer.OptionsApple.Flags.UseSinglePlayerItem;
		}

		public static MediaPlayer.OptionsApple.Flags SetUseSinglePlayerItem(this MediaPlayer.OptionsApple.Flags flags, bool b)
		{
			if (flags.UseSinglePlayerItem() ^ b)
			{
				flags = b ? flags | MediaPlayer.OptionsApple.Flags.UseSinglePlayerItem
						  : flags & ~MediaPlayer.OptionsApple.Flags.UseSinglePlayerItem;
			}
			return flags;
		}

		public static bool ResumePlaybackAfterAudioSessionRouteChange(this MediaPlayer.OptionsApple.Flags flags)
		{
			return (flags & MediaPlayer.OptionsApple.Flags.ResumeMediaPlaybackAfterAudioSessionRouteChange) == MediaPlayer.OptionsApple.Flags.ResumeMediaPlaybackAfterAudioSessionRouteChange;
		}

		public static MediaPlayer.OptionsApple.Flags SetResumePlaybackAfterAudioSessionRouteChange(this MediaPlayer.OptionsApple.Flags flags, bool b)
		{
			if (flags.ResumePlaybackAfterAudioSessionRouteChange() ^ b)
			{
				flags = b ? flags | MediaPlayer.OptionsApple.Flags.ResumeMediaPlaybackAfterAudioSessionRouteChange
						  : flags & ~MediaPlayer.OptionsApple.Flags.ResumeMediaPlaybackAfterAudioSessionRouteChange;
			}
			return flags;
		}
	}
#endregion // PlatformOptionsExtensions
}
