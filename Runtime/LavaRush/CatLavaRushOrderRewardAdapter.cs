using System;
using ActionFit.Cat.App.Order;
using ActionFit.LavaRush.UI;

namespace ActionFit.Cat.App.LavaRush
{
    /// <summary>Defines the project-owned OrderList calls used by the Lava Rush provider.</summary>
    public sealed class CatLavaRushOrderRewardBinding
    {
        public CatLavaRushOrderRewardBinding(
            Func<bool> isActive,
            Func<int, int> resolveProgress,
            Func<object, ILavaRushProgressView> resolveMatchingProgressView,
            Action<ILavaRushProgressView, int> playEffect,
            Action<CatLavaRushOrderRewardAdapter> register,
            Action<CatLavaRushOrderRewardAdapter> unregister)
        {
            IsActive = isActive ?? throw new ArgumentNullException(nameof(isActive));
            ResolveProgress = resolveProgress ?? throw new ArgumentNullException(nameof(resolveProgress));
            ResolveMatchingProgressView = resolveMatchingProgressView
                ?? throw new ArgumentNullException(nameof(resolveMatchingProgressView));
            PlayEffect = playEffect ?? throw new ArgumentNullException(nameof(playEffect));
            Register = register ?? throw new ArgumentNullException(nameof(register));
            Unregister = unregister ?? throw new ArgumentNullException(nameof(unregister));
        }

        public Func<bool> IsActive { get; }
        public Func<int, int> ResolveProgress { get; }
        public Func<object, ILavaRushProgressView> ResolveMatchingProgressView { get; }
        public Action<ILavaRushProgressView, int> PlayEffect { get; }
        public Action<CatLavaRushOrderRewardAdapter> Register { get; }
        public Action<CatLavaRushOrderRewardAdapter> Unregister { get; }
    }

    /// <summary>Owns the Cat priority-100 Lava Rush OrderList provider behavior.</summary>
    public sealed class CatLavaRushOrderRewardAdapter
    {
        public const int ProviderPriority = 100;

        #region Fields

        private readonly CatLavaRushOrderRewardBinding _binding;
        private int _attachmentCount;

        #endregion

        #region Properties

        public int Priority => ProviderPriority;
        public bool IsActive => _binding.IsActive();

        #endregion

        public CatLavaRushOrderRewardAdapter(CatLavaRushOrderRewardBinding binding)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        #region Public Methods

        /// <summary>Registers this provider for the owning controller's enabled lifetime.</summary>
        public IDisposable Attach()
        {
            if (_attachmentCount == 0)
            {
                _binding.Register(this);
            }

            _attachmentCount++;
            return new Attachment(this);
        }

        /// <summary>Converts every ordered item level, including duplicates, into one positive reward.</summary>
        public bool TryGetReward(CatOrderCompletionSnapshot snapshot, out int amount)
        {
            amount = 0;
            if (snapshot == null || !IsActive)
            {
                return false;
            }

            for (int index = 0; index < snapshot.ItemLevels.Count; index++)
            {
                amount += _binding.ResolveProgress(snapshot.ItemLevels[index]);
            }

            return amount > 0;
        }

        /// <summary>Requests the matching live OrderList cell effect without mutating progress.</summary>
        public bool TryPlayCompletionEffect(CatOrderCompletionSnapshot snapshot, int amount)
        {
            if (snapshot == null || amount <= 0)
            {
                return false;
            }

            ILavaRushProgressView progressView =
                _binding.ResolveMatchingProgressView(snapshot.CompletionIdentity);
            if (progressView == null)
            {
                return false;
            }

            _binding.PlayEffect(progressView, amount);
            return true;
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
                _binding.Unregister(this);
            }
        }

        #endregion

        private sealed class Attachment : IDisposable
        {
            private CatLavaRushOrderRewardAdapter _owner;

            public Attachment(CatLavaRushOrderRewardAdapter owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                CatLavaRushOrderRewardAdapter owner = _owner;
                _owner = null;
                owner?.Detach();
            }
        }
    }
}
