using System;
using System.Threading;
using ActionFit.LavaRush.UI;
using TMPro;

namespace ActionFit.Cat.App.LavaRush
{
    /// <summary>Separates preserved Addressable keys from Cat adapter type identities.</summary>
    public readonly struct CatLavaRushEventAccessDescriptor
    {
        public const string LobbyAddressableKey = "UI_LavaRush_Icon";
        public const string InGameAddressableKey = "UI_LavaRush_Cell";
        public const int LavaRushSlotOrder = 2;

        public string LobbyKey => LobbyAddressableKey;
        public string InGameKey => InGameAddressableKey;
        public Type LobbyAdapterType => typeof(CatLavaRushLobbyAccessAdapter);
        public Type InGameAdapterType => typeof(CatLavaRushInGameAccessAdapter);
        public int SlotOrder => LavaRushSlotOrder;
    }

    /// <summary>Defines the additive project EventAccess registry calls.</summary>
    public sealed class CatLavaRushEventAccessRegistryBinding
    {
        public CatLavaRushEventAccessRegistryBinding(
            Action<CatLavaRushEventAccessDescriptor> register,
            Action<CatLavaRushEventAccessDescriptor> unregister)
        {
            Register = register ?? throw new ArgumentNullException(nameof(register));
            Unregister = unregister ?? throw new ArgumentNullException(nameof(unregister));
        }

        public Action<CatLavaRushEventAccessDescriptor> Register { get; }
        public Action<CatLavaRushEventAccessDescriptor> Unregister { get; }
    }

    /// <summary>Pairs deterministic EventAccess registration with the Cat manager lifetime.</summary>
    public sealed class CatLavaRushEventAccessRegistration
    {
        #region Fields

        private readonly CatLavaRushEventAccessRegistryBinding _binding;
        private int _attachmentCount;

        #endregion

        public CatLavaRushEventAccessRegistration(CatLavaRushEventAccessRegistryBinding binding)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        #region Public Methods

        public IDisposable Attach()
        {
            if (_attachmentCount == 0)
            {
                _binding.Register(new CatLavaRushEventAccessDescriptor());
            }

            _attachmentCount++;
            return new Attachment(this);
        }

        #endregion

        #region Private Methods

        private void Detach()
        {
            if (_attachmentCount <= 0)
            {
                return;
            }

            _attachmentCount--;
            if (_attachmentCount == 0)
            {
                _binding.Unregister(new CatLavaRushEventAccessDescriptor());
            }
        }

        #endregion

        private sealed class Attachment : IDisposable
        {
            private CatLavaRushEventAccessRegistration _owner;

            public Attachment(CatLavaRushEventAccessRegistration owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                CatLavaRushEventAccessRegistration owner = _owner;
                _owner = null;
                owner?.Detach();
            }
        }
    }

    /// <summary>Forwards the package lobby icon click and countdown through injected Cat ports.</summary>
    public sealed class CatLavaRushLobbyAccessAdapter
    {
        #region Fields

        private readonly ILavaRushAccessService _accessService;
        private readonly ILavaRushCountdownScheduler _countdownScheduler;
        private TMP_Text _timerText;
        private CatLavaRushAccessLifetime _activeLifetime;

        #endregion

        public CatLavaRushLobbyAccessAdapter(
            ILavaRushAccessService accessService,
            ILavaRushCountdownScheduler countdownScheduler)
        {
            _accessService = accessService ?? throw new ArgumentNullException(nameof(accessService));
            _countdownScheduler = countdownScheduler
                ?? throw new ArgumentNullException(nameof(countdownScheduler));
        }

        #region Public Methods

        /// <summary>Rechecks post-load state and leaves a failed binding retryable.</summary>
        public bool TryBind(TMP_Text timerText)
        {
            if (timerText == null || !_accessService.IsEventActive || !_accessService.IsEventStarted)
            {
                return false;
            }

            if (_timerText != null)
            {
                return ReferenceEquals(_timerText, timerText);
            }

            _timerText = timerText;
            return true;
        }

        /// <summary>Starts one cancellation-owned countdown for the bound icon.</summary>
        public bool TryActivate(
            CancellationToken cancellationToken,
            Action onExpired,
            out IDisposable lifetime)
        {
            lifetime = null;
            if (_activeLifetime != null
                || _timerText == null
                || !_accessService.IsEventActive
                || !_accessService.IsEventStarted)
            {
                return false;
            }

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                _countdownScheduler.Register(
                    _timerText,
                    _accessService.EventEndTime,
                    cancellation.Token,
                    onExpired,
                    LavaRushTimeText.FormatHourMinSec);
                _activeLifetime = new CatLavaRushAccessLifetime(
                    cancellation,
                    null,
                    OnLifetimeEnded);
                lifetime = _activeLifetime;
                return true;
            }
            catch
            {
                cancellation.Cancel();
                cancellation.Dispose();
                throw;
            }
        }

        public bool TryOpenContent()
        {
            if (!_accessService.IsEventActive || !_accessService.IsEventStarted)
            {
                return false;
            }

            _accessService.OpenContent();
            return true;
        }

        public void Clear()
        {
            _activeLifetime?.Dispose();
            _activeLifetime = null;
            _timerText = null;
        }

        #endregion

        private void OnLifetimeEnded() => _activeLifetime = null;
    }

    /// <summary>Forwards the package in-game cell click, countdown, frame, and progress behavior.</summary>
    public sealed class CatLavaRushInGameAccessAdapter
    {
        #region Fields

        private readonly ILavaRushAccessService _accessService;
        private readonly ILavaRushFrameScheduler _frameScheduler;
        private readonly ILavaRushCountdownScheduler _countdownScheduler;
        private TMP_Text _timerText;
        private ILavaRushProgressView _progressView;
        private CatLavaRushAccessLifetime _activeLifetime;

        #endregion

        public CatLavaRushInGameAccessAdapter(
            ILavaRushAccessService accessService,
            ILavaRushFrameScheduler frameScheduler,
            ILavaRushCountdownScheduler countdownScheduler)
        {
            _accessService = accessService ?? throw new ArgumentNullException(nameof(accessService));
            _frameScheduler = frameScheduler ?? throw new ArgumentNullException(nameof(frameScheduler));
            _countdownScheduler = countdownScheduler
                ?? throw new ArgumentNullException(nameof(countdownScheduler));
        }

        #region Public Methods

        /// <summary>Rechecks post-load state and attaches one explicit package binding.</summary>
        public bool TryBind(TMP_Text timerText, ILavaRushProgressView progressView)
        {
            if (timerText == null
                || progressView == null
                || progressView.TargetProgress == null
                || !_accessService.IsEventActive
                || !_accessService.IsEventStarted)
            {
                return false;
            }

            if (_timerText != null || _progressView != null)
            {
                return ReferenceEquals(_timerText, timerText)
                    && ReferenceEquals(_progressView, progressView);
            }

            _timerText = timerText;
            _progressView = progressView;
            return true;
        }

        /// <summary>Starts one countdown and frame subscription for the bound cell lifetime.</summary>
        public bool TryActivate(
            CancellationToken cancellationToken,
            Action onExpired,
            Action<float> onFrame,
            out IDisposable lifetime)
        {
            lifetime = null;
            if (onFrame == null)
            {
                throw new ArgumentNullException(nameof(onFrame));
            }

            if (_activeLifetime != null
                || _timerText == null
                || _progressView == null
                || !_accessService.IsEventActive
                || !_accessService.IsEventStarted)
            {
                return false;
            }

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IDisposable frameLifetime = null;
            try
            {
                _countdownScheduler.Register(
                    _timerText,
                    _accessService.EventEndTime,
                    cancellation.Token,
                    onExpired,
                    LavaRushTimeText.FormatHourMinSec);
                frameLifetime = _frameScheduler.SubscribeUpdate(onFrame);
                if (frameLifetime == null)
                {
                    throw new InvalidOperationException(
                        "[CatLavaRushInGameAccessAdapter] TryActivate: frame lifetime is null");
                }

                _activeLifetime = new CatLavaRushAccessLifetime(
                    cancellation,
                    frameLifetime,
                    OnLifetimeEnded);
                lifetime = _activeLifetime;
                return true;
            }
            catch
            {
                frameLifetime?.Dispose();
                cancellation.Cancel();
                cancellation.Dispose();
                throw;
            }
        }

        public bool TryOpenContent()
        {
            if (!_accessService.IsEventActive || !_accessService.IsEventStarted)
            {
                return false;
            }

            _accessService.OpenContent();
            return true;
        }

        public void NotifyProgressArrived() => _progressView?.NotifyProgressArrived();

        public void Clear()
        {
            _activeLifetime?.Dispose();
            _activeLifetime = null;
            _timerText = null;
            _progressView = null;
        }

        #endregion

        private void OnLifetimeEnded() => _activeLifetime = null;
    }

    internal sealed class CatLavaRushAccessLifetime : IDisposable
    {
        private CancellationTokenSource _cancellation;
        private IDisposable _frameLifetime;
        private Action _onDisposed;

        public CatLavaRushAccessLifetime(
            CancellationTokenSource cancellation,
            IDisposable frameLifetime,
            Action onDisposed)
        {
            _cancellation = cancellation;
            _frameLifetime = frameLifetime;
            _onDisposed = onDisposed;
        }

        public void Dispose()
        {
            CancellationTokenSource cancellation = _cancellation;
            IDisposable frameLifetime = _frameLifetime;
            Action onDisposed = _onDisposed;
            _cancellation = null;
            _frameLifetime = null;
            _onDisposed = null;

            try
            {
                frameLifetime?.Dispose();
            }
            finally
            {
                if (cancellation != null)
                {
                    cancellation.Cancel();
                    cancellation.Dispose();
                }

                onDisposed?.Invoke();
            }
        }
    }
}
