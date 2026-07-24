using System;
using ActionFit.Cat.App.Order;
using ActionFit.LavaRush.UI;

namespace ActionFit.Cat.App.LavaRush
{
    /// <summary>Converts synchronous Cat order completions into enabled-lifetime Lava Rush progress.</summary>
    public sealed class CatLavaRushOrderProgressSource : ILavaRushOrderProgressSource
    {
        #region Fields

        private readonly Func<Action<CatOrderCompletionSnapshot>, IDisposable> _subscribeToCompletion;
        private readonly CatLavaRushOrderRewardAdapter _rewardAdapter;
        private readonly Action<Exception> _failureObserver;

        #endregion

        public CatLavaRushOrderProgressSource(
            Func<Action<CatOrderCompletionSnapshot>, IDisposable> subscribeToCompletion,
            CatLavaRushOrderRewardAdapter rewardAdapter,
            Action<Exception> failureObserver = null)
        {
            _subscribeToCompletion = subscribeToCompletion
                ?? throw new ArgumentNullException(nameof(subscribeToCompletion));
            _rewardAdapter = rewardAdapter ?? throw new ArgumentNullException(nameof(rewardAdapter));
            _failureObserver = failureObserver;
        }

        #region Public Methods

        /// <summary>Pairs the Cat completion feed and OrderList provider with one enabled lifetime.</summary>
        public IDisposable Subscribe(Action<int> onOrderProgress)
        {
            if (onOrderProgress == null)
            {
                throw new ArgumentNullException(nameof(onOrderProgress));
            }

            IDisposable providerLifetime = _rewardAdapter.Attach();
            try
            {
                IDisposable completionLifetime = _subscribeToCompletion(
                    snapshot => OnOrderCompleted(snapshot, onOrderProgress));
                if (completionLifetime == null)
                {
                    throw new InvalidOperationException(
                        "[CatLavaRushOrderProgressSource] Subscribe: completion lifetime is null");
                }

                return new Subscription(completionLifetime, providerLifetime);
            }
            catch
            {
                providerLifetime.Dispose();
                throw;
            }
        }

        #endregion

        #region Event Handlers

        private void OnOrderCompleted(
            CatOrderCompletionSnapshot snapshot,
            Action<int> onOrderProgress)
        {
            int amount;
            try
            {
                if (!_rewardAdapter.TryGetReward(snapshot, out amount))
                {
                    return;
                }
            }
            catch (Exception exception)
            {
                ObserveFailure(exception);
                return;
            }

            try
            {
                _rewardAdapter.TryPlayCompletionEffect(snapshot, amount);
            }
            catch (Exception exception)
            {
                ObserveFailure(exception);
            }

            try
            {
                onOrderProgress(amount);
            }
            catch (Exception exception)
            {
                ObserveFailure(exception);
            }
        }

        #endregion

        #region Private Methods

        private void ObserveFailure(Exception exception)
        {
            try
            {
                _failureObserver?.Invoke(exception);
            }
            catch
            {
                // A diagnostic observer must not change Order completion or progress sequencing.
            }
        }

        #endregion

        private sealed class Subscription : IDisposable
        {
            private IDisposable _completionLifetime;
            private IDisposable _providerLifetime;

            public Subscription(
                IDisposable completionLifetime,
                IDisposable providerLifetime)
            {
                _completionLifetime = completionLifetime;
                _providerLifetime = providerLifetime;
            }

            public void Dispose()
            {
                IDisposable completionLifetime = _completionLifetime;
                IDisposable providerLifetime = _providerLifetime;
                _completionLifetime = null;
                _providerLifetime = null;

                try
                {
                    completionLifetime?.Dispose();
                }
                finally
                {
                    providerLifetime?.Dispose();
                }
            }
        }
    }
}
