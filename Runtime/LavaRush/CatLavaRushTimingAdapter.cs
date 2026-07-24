using System;
using System.Threading;
using ActionFit.LavaRush.UI;
using TMPro;

namespace ActionFit.Cat.App
{
    public sealed class CatLavaRushTimingAdapter :
        ILavaRushFrameScheduler,
        ILavaRushCountdownScheduler
    {
        private readonly CatLoop _loop;
        private readonly CatCountdown _countdown;

        public CatLavaRushTimingAdapter(CatLoop loop, CatCountdown countdown)
        {
            _loop = loop ?? throw new ArgumentNullException(nameof(loop));
            _countdown = countdown ?? throw new ArgumentNullException(nameof(countdown));
        }

        public DateTime Now => _countdown.Now;

        public bool TryGetNow(out DateTime now)
        {
            return _countdown.TryGetNow(out now);
        }

        public IDisposable SubscribeUpdate(Action<float> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _loop.UpdateRequested += handler;
            return new Subscription(() => _loop.UpdateRequested -= handler);
        }

        public IDisposable SubscribeLateUpdate(Action<float> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _loop.LateUpdateRequested += handler;
            return new Subscription(() => _loop.LateUpdateRequested -= handler);
        }

        public IDisposable SubscribeEverySecond(Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _loop.EverySecondRequested += handler;
            return new Subscription(() => _loop.EverySecondRequested -= handler);
        }

        public void Register(
            TMP_Text target,
            DateTime endTime,
            CancellationToken cancellationToken,
            Action onExpired = null,
            Func<TimeSpan, string> formatter = null)
        {
            _countdown.Register(target, endTime, cancellationToken, onExpired, formatter);
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;

            public Subscription(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _dispose, null)?.Invoke();
            }
        }
    }
}
