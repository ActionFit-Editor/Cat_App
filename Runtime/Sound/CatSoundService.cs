using System;
using System.Collections.Generic;

namespace ActionFit.Cat.App
{
    public enum CatSoundChannel
    {
        Overlay,
        Immediate,
        Single,
    }

    public readonly struct CatSoundPlayback
    {
        public CatSoundPlayback(
            string clipId,
            CatSoundChannel channel,
            string mixerGroup,
            float volume,
            bool pitched,
            float pitchMin,
            float pitchMax)
        {
            if (string.IsNullOrWhiteSpace(clipId))
                throw new ArgumentException("A product clip identifier is required.", nameof(clipId));
            if (volume < 0f)
                throw new ArgumentOutOfRangeException(nameof(volume));
            if (pitched && pitchMax < pitchMin)
                throw new ArgumentOutOfRangeException(nameof(pitchMax));

            ClipId = clipId;
            Channel = channel;
            MixerGroup = mixerGroup ?? string.Empty;
            Volume = volume;
            Pitched = pitched;
            PitchMin = pitchMin;
            PitchMax = pitchMax;
        }

        public string ClipId { get; }
        public CatSoundChannel Channel { get; }
        public string MixerGroup { get; }
        public float Volume { get; }
        public bool Pitched { get; }
        public float PitchMin { get; }
        public float PitchMax { get; }
    }

    /// <summary>Project Shell leaf that resolves product clip IDs to current clips and channels.</summary>
    public interface ICatSoundPlaybackBackend
    {
        bool IsAvailable { get; }
        void Play(CatSoundPlayback playback);
        void RecoverAudioDevice();
    }

    public interface ICatSoundOptions
    {
        bool SoundEffectsEnabled { get; }
    }

    /// <summary>Project Shell signal for the existing Unity audio-configuration callback.</summary>
    public interface ICatAudioConfigurationEvents
    {
        event Action<bool> ConfigurationChanged;
    }

    /// <summary>
    /// Owns Cat playback policy and callback lifetime while concrete clips, sources, mixer, and
    /// device APIs remain injected Project Shell leaves.
    /// </summary>
    public sealed class CatSoundService : IDisposable
    {
        private readonly ICatSoundPlaybackBackend _backend;
        private readonly ICatSoundOptions _options;
        private readonly ICatAudioConfigurationEvents _configurationEvents;
        private bool _initialized;
        private bool _disposed;

        public CatSoundService(
            ICatSoundPlaybackBackend backend,
            ICatSoundOptions options,
            ICatAudioConfigurationEvents configurationEvents)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _configurationEvents = configurationEvents
                ?? throw new ArgumentNullException(nameof(configurationEvents));
        }

        public bool IsInitialized => _initialized && !_disposed;

        public void Initialize()
        {
            ThrowIfDisposed();
            if (_initialized)
                return;

            _configurationEvents.ConfigurationChanged += HandleAudioConfigurationChanged;
            _initialized = true;
        }

        public void Play(CatSoundPlayback playback)
        {
            ThrowIfDisposed();
            if (!_initialized || !_backend.IsAvailable || !_options.SoundEffectsEnabled)
                return;
            _backend.Play(playback);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_initialized)
                _configurationEvents.ConfigurationChanged -= HandleAudioConfigurationChanged;
            _initialized = false;
            _disposed = true;
        }

        private void HandleAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (!deviceWasChanged || !_backend.IsAvailable)
                return;
            _backend.RecoverAudioDevice();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CatSoundService));
        }
    }

    public sealed class CatSoundCatalog
    {
        private readonly IReadOnlyDictionary<string, CatSoundPlayback> _playbackByCue;

        public CatSoundCatalog(IReadOnlyDictionary<string, CatSoundPlayback> playbackByCue)
        {
            _playbackByCue = playbackByCue
                ?? throw new ArgumentNullException(nameof(playbackByCue));
        }

        public bool TryGet(string cueId, out CatSoundPlayback playback)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                playback = default;
                return false;
            }

            return _playbackByCue.TryGetValue(cueId, out playback);
        }
    }
}
