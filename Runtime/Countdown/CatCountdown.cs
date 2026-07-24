using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;

namespace ActionFit.Cat.App
{
    public delegate bool CatTryGetNow(out DateTime now);

    public sealed class CatCountdown
    {
        private readonly List<Subscription> _subscriptions = new List<Subscription>(64);
        private readonly CatLoop _loop;
        private readonly Func<DateTime> _getNow;
        private readonly CatTryGetNow _tryGetNow;
        private readonly Action<Exception> _reportCallbackFailure;
        private bool _attached;

        public CatCountdown(
            CatLoop loop,
            Func<DateTime> getNow,
            CatTryGetNow tryGetNow,
            Action<Exception> reportCallbackFailure)
        {
            _loop = loop ?? throw new ArgumentNullException(nameof(loop));
            _getNow = getNow ?? throw new ArgumentNullException(nameof(getNow));
            _tryGetNow = tryGetNow ?? throw new ArgumentNullException(nameof(tryGetNow));
            _reportCallbackFailure = reportCallbackFailure ?? throw new ArgumentNullException(nameof(reportCallbackFailure));
            Attach();
        }

        public DateTime Now => _getNow();

        public bool TryGetNow(out DateTime now)
        {
            return _tryGetNow(out now);
        }

        public void Register(
            TMP_Text target,
            DateTime endTime,
            CancellationToken cancellationToken,
            Action onExpired = null,
            Func<TimeSpan, string> formatter = null)
        {
            if (target == null)
            {
                return;
            }

            Func<TimeSpan, string> resolvedFormatter = formatter ?? FormatDefault;
            _subscriptions.Add(new Subscription(
                target,
                endTime,
                resolvedFormatter,
                onExpired,
                cancellationToken));

            if (!TryGetNow(out DateTime now))
            {
                return;
            }

            TimeSpan remaining = endTime - now;
            target.text = resolvedFormatter(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
        }

        public void Clear()
        {
            if (_attached)
            {
                _loop.EverySecondRequested -= TickAll;
                _attached = false;
            }

            _subscriptions.Clear();
        }

        public static string FormatHourMinSec(TimeSpan remaining)
        {
            return $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        private void Attach()
        {
            if (_attached)
            {
                return;
            }

            _loop.EverySecondRequested += TickAll;
            _attached = true;
        }

        private void TickAll()
        {
            bool hasTrustedTime = TryGetNow(out DateTime now);
            for (int index = _subscriptions.Count - 1; index >= 0; index--)
            {
                Subscription subscription = _subscriptions[index];
                if (subscription.CancellationToken.IsCancellationRequested || subscription.Target == null)
                {
                    _subscriptions.RemoveAt(index);
                    continue;
                }

                if (!hasTrustedTime)
                {
                    continue;
                }

                TimeSpan remaining = subscription.EndTime - now;
                if (remaining <= TimeSpan.Zero)
                {
                    subscription.Target.text = subscription.Formatter(TimeSpan.Zero);
                    try
                    {
                        subscription.OnExpired?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        _reportCallbackFailure(exception);
                    }

                    _subscriptions.RemoveAt(index);
                    continue;
                }

                subscription.Target.text = subscription.Formatter(remaining);
            }
        }

        private static string FormatDefault(TimeSpan remaining)
        {
            return (int)remaining.TotalDays > 0
                ? $"{(int)remaining.TotalDays}D {remaining.Hours:00}:{remaining.Minutes:00}"
                : FormatHourMinSec(remaining);
        }

        private readonly struct Subscription
        {
            public Subscription(
                TMP_Text target,
                DateTime endTime,
                Func<TimeSpan, string> formatter,
                Action onExpired,
                CancellationToken cancellationToken)
            {
                Target = target;
                EndTime = endTime;
                Formatter = formatter;
                OnExpired = onExpired;
                CancellationToken = cancellationToken;
            }

            public TMP_Text Target { get; }
            public DateTime EndTime { get; }
            public Func<TimeSpan, string> Formatter { get; }
            public Action OnExpired { get; }
            public CancellationToken CancellationToken { get; }
        }
    }
}
