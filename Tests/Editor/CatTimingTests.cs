using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace ActionFit.Cat.App.Tests
{
    public sealed class CatTimingTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _objects.Count; index++)
            {
                if (_objects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void Countdown_UntrustedRegistrationRetainsTextAndCancellationStillCleansUp()
        {
            var loop = new CatLoop();
            bool trusted = false;
            DateTime now = new DateTime(2026, 7, 24, 3, 0, 0);
            var countdown = new CatCountdown(
                loop,
                () => trusted ? now : throw new InvalidOperationException("untrusted"),
                (out DateTime value) =>
                {
                    value = now;
                    return trusted;
                },
                _ => { });
            TMP_Text target = CreateText("waiting");
            var cancellation = new CancellationTokenSource();
            int expired = 0;

            countdown.Register(
                target,
                now.AddSeconds(-1),
                cancellation.Token,
                () => expired++);
            Assert.That(target.text, Is.EqualTo("waiting"));

            cancellation.Cancel();
            loop.AdvanceFrame(0f, 1f);
            trusted = true;
            loop.AdvanceFrame(0f, 1f);

            Assert.That(target.text, Is.EqualTo("waiting"));
            Assert.That(expired, Is.Zero);
            cancellation.Dispose();
        }

        [Test]
        public void Countdown_ImmediateDisplayDefersExpiredCallbacksAndUsesReverseOrder()
        {
            var loop = new CatLoop();
            DateTime now = new DateTime(2026, 7, 24, 3, 0, 0);
            var failures = new List<Exception>();
            var countdown = CreateTrustedCountdown(loop, () => now, failures.Add);
            TMP_Text target = CreateText("stale");
            var callbacks = new List<string>();

            countdown.Register(
                target,
                now.AddSeconds(-1),
                CancellationToken.None,
                () => callbacks.Add("old"),
                remaining => remaining == TimeSpan.Zero ? "expired" : "active");
            countdown.Register(
                target,
                now.AddSeconds(-1),
                CancellationToken.None,
                () => callbacks.Add("new"),
                remaining => remaining == TimeSpan.Zero ? "expired" : "active");

            Assert.That(target.text, Is.EqualTo("expired"));
            Assert.That(callbacks, Is.Empty);
            loop.AdvanceFrame(0f, 1f);

            Assert.That(callbacks, Is.EqualTo(new[] { "new", "old" }));
            Assert.That(failures, Is.Empty);
            loop.AdvanceFrame(0f, 1f);
            Assert.That(callbacks, Has.Count.EqualTo(2));
        }

        [Test]
        public void Countdown_CallbackFailureIsReportedAndDoesNotBlockOlderCallback()
        {
            var loop = new CatLoop();
            DateTime now = new DateTime(2026, 7, 24, 3, 0, 0);
            var failures = new List<Exception>();
            var countdown = CreateTrustedCountdown(loop, () => now, failures.Add);
            TMP_Text target = CreateText("active");
            int completed = 0;
            countdown.Register(
                target,
                now,
                CancellationToken.None,
                () => completed++);
            countdown.Register(
                target,
                now,
                CancellationToken.None,
                () => throw new InvalidOperationException("expected"));

            Assert.DoesNotThrow(() => loop.AdvanceFrame(0f, 1f));

            Assert.That(completed, Is.EqualTo(1));
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0].Message, Is.EqualTo("expected"));
        }

        [Test]
        public void Countdown_DestroyedTargetAndClearRemoveWorkWithoutExpiry()
        {
            var loop = new CatLoop();
            DateTime now = new DateTime(2026, 7, 24, 3, 0, 0);
            var countdown = CreateTrustedCountdown(loop, () => now, _ => { });
            TMP_Text destroyed = CreateText("destroyed");
            TMP_Text cleared = CreateText("cleared");
            int expired = 0;
            countdown.Register(destroyed, now, CancellationToken.None, () => expired++);
            countdown.Register(cleared, now.AddHours(1), CancellationToken.None, () => expired++);

            UnityEngine.Object.DestroyImmediate(destroyed.gameObject);
            loop.AdvanceFrame(0f, 1f);
            countdown.Clear();
            now = now.AddHours(2);
            loop.AdvanceFrame(0f, 2f);

            Assert.That(expired, Is.Zero);
            Assert.That(cleared.text, Is.EqualTo("01:00:00"));
        }

        [Test]
        public void LavaRushTimingAdapter_SubscriptionsAreIndependentAndDisposeIdempotently()
        {
            var loop = new CatLoop();
            DateTime now = new DateTime(2026, 7, 24, 3, 0, 0);
            var countdown = CreateTrustedCountdown(loop, () => now, _ => { });
            var adapter = new CatLavaRushTimingAdapter(loop, countdown);
            var observed = new List<float>();
            IDisposable first = adapter.SubscribeUpdate(observed.Add);
            IDisposable second = adapter.SubscribeUpdate(observed.Add);

            loop.AdvanceFrame(0.5f, 0f);
            first.Dispose();
            first.Dispose();
            loop.AdvanceFrame(0.25f, 0f);
            second.Dispose();
            loop.AdvanceFrame(1f, 0f);

            Assert.That(observed, Is.EqualTo(new[] { 0.5f, 0.5f, 0.25f }));
            Assert.That(adapter.TryGetNow(out DateTime actual), Is.True);
            Assert.That(actual, Is.EqualTo(now));
            Assert.That(adapter.Now, Is.EqualTo(now));
        }

        [Test]
        public void Countdown_FormatsDayAndAccumulatedHoursExactly()
        {
            var loop = new CatLoop();
            DateTime now = new DateTime(2026, 7, 24, 3, 0, 0);
            var countdown = CreateTrustedCountdown(loop, () => now, _ => { });
            TMP_Text target = CreateText("stale");

            countdown.Register(
                target,
                now.AddDays(1).AddHours(2).AddMinutes(3),
                CancellationToken.None);

            Assert.That(target.text, Is.EqualTo("1D 02:03"));
            Assert.That(
                CatCountdown.FormatHourMinSec(
                    TimeSpan.FromDays(1)
                        .Add(TimeSpan.FromHours(4))
                        .Add(TimeSpan.FromMinutes(5))
                        .Add(TimeSpan.FromSeconds(30))),
                Is.EqualTo("28:05:30"));
        }

        private static CatCountdown CreateTrustedCountdown(
            CatLoop loop,
            Func<DateTime> getNow,
            Action<Exception> report)
        {
            return new CatCountdown(
                loop,
                getNow,
                (out DateTime now) =>
                {
                    now = getNow();
                    return true;
                },
                report);
        }

        private TMP_Text CreateText(string text)
        {
            var gameObject = new GameObject("CatTimingTests");
            _objects.Add(gameObject);
            var target = gameObject.AddComponent<TextMeshProUGUI>();
            target.text = text;
            return target;
        }
    }
}
