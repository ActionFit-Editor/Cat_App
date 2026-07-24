using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActionFit.Cat.App.LavaRush;
using ActionFit.Cat.App.Order;
using ActionFit.LavaRush.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace ActionFit.Cat.App.Tests.LavaRush
{
    public sealed class CatLavaRushEventShellTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [Test]
        public void OrderAdapter_AggregatesDuplicatesAndPreservesProviderLifetime()
        {
            bool active = true;
            var registeredProviders = new List<object>
            {
                new object(),
                new object(),
            };
            var identity = new object();
            var progressView = CreateProgressView();
            var adapter = new CatLavaRushOrderRewardAdapter(
                new CatLavaRushOrderRewardBinding(
                    () => active,
                    level => level == 9 ? 0 : level * 2,
                    candidate => ReferenceEquals(candidate, identity) ? progressView : null,
                    (_, _) => { },
                    provider => registeredProviders.Add(provider),
                    provider => registeredProviders.Remove(provider)));

            IDisposable first = adapter.Attach();
            IDisposable second = adapter.Attach();
            var snapshot = new CatOrderCompletionSnapshot(identity, new[] { 2, 2, 9, 3 });

            Assert.That(adapter.Priority, Is.EqualTo(100));
            Assert.That(adapter.TryGetReward(snapshot, out int amount), Is.True);
            Assert.That(amount, Is.EqualTo(14));
            Assert.That(registeredProviders, Has.Count.EqualTo(3));
            Assert.That(registeredProviders.Count(value => ReferenceEquals(value, adapter)),
                Is.EqualTo(1));

            first.Dispose();
            Assert.That(registeredProviders, Has.Count.EqualTo(3));
            second.Dispose();
            Assert.That(registeredProviders, Has.Count.EqualTo(2));
            Assert.That(
                registeredProviders.Any(value => ReferenceEquals(value, adapter)),
                Is.False);

            active = false;
            Assert.That(adapter.TryGetReward(snapshot, out amount), Is.False);
            Assert.That(amount, Is.Zero);
        }

        [Test]
        public void OrderProgressSource_PlaysMatchingEffectBeforeExactlyOneProgress()
        {
            Action<CatOrderCompletionSnapshot> completion = null;
            var sequence = new List<string>();
            var identity = new object();
            var progressView = CreateProgressView();
            object resolvedIdentity = null;
            var adapter = new CatLavaRushOrderRewardAdapter(
                new CatLavaRushOrderRewardBinding(
                    () => true,
                    level => level,
                    candidate =>
                    {
                        resolvedIdentity = candidate;
                        return progressView;
                    },
                    (_, amount) => sequence.Add($"effect:{amount}"),
                    _ => { },
                    _ => { }));
            var source = new CatLavaRushOrderProgressSource(
                handler =>
                {
                    completion += handler;
                    return new CallbackDisposable(() => completion -= handler);
                },
                adapter);

            using (source.Subscribe(amount => sequence.Add($"progress:{amount}")))
            {
                completion?.Invoke(new CatOrderCompletionSnapshot(identity, new[] { 1, 1, 3 }));
            }

            completion?.Invoke(new CatOrderCompletionSnapshot(identity, new[] { 9 }));
            Assert.That(resolvedIdentity, Is.SameAs(identity));
            Assert.That(sequence, Is.EqualTo(new[] { "effect:5", "progress:5" }));
        }

        [Test]
        public void OrderProgressSource_VisualFailureDoesNotBlockProgress()
        {
            Action<CatOrderCompletionSnapshot> completion = null;
            int failures = 0;
            int progressCalls = 0;
            var adapter = new CatLavaRushOrderRewardAdapter(
                new CatLavaRushOrderRewardBinding(
                    () => true,
                    level => level,
                    _ => CreateProgressView(),
                    (_, _) => throw new InvalidOperationException("visual"),
                    _ => { },
                    _ => { }));
            var source = new CatLavaRushOrderProgressSource(
                handler =>
                {
                    completion = handler;
                    return new CallbackDisposable(() => completion = null);
                },
                adapter,
                _ => failures++);

            using (source.Subscribe(_ => progressCalls++))
            {
                completion?.Invoke(new CatOrderCompletionSnapshot(new object(), new[] { 4 }));
            }

            Assert.That(failures, Is.EqualTo(1));
            Assert.That(progressCalls, Is.EqualTo(1));
        }

        [Test]
        public void EventAccessRegistration_PreservesKeysTypesSlotAndOneLifetime()
        {
            int registered = 0;
            CatLavaRushEventAccessDescriptor descriptor = default;
            var registration = new CatLavaRushEventAccessRegistration(
                new CatLavaRushEventAccessRegistryBinding(
                    value =>
                    {
                        descriptor = value;
                        registered++;
                    },
                    _ => registered--));

            IDisposable first = registration.Attach();
            IDisposable second = registration.Attach();

            Assert.That(registered, Is.EqualTo(1));
            Assert.That(descriptor.LobbyKey, Is.EqualTo("UI_LavaRush_Icon"));
            Assert.That(descriptor.InGameKey, Is.EqualTo("UI_LavaRush_Cell"));
            Assert.That(descriptor.LobbyAdapterType, Is.EqualTo(typeof(CatLavaRushLobbyAccessAdapter)));
            Assert.That(descriptor.InGameAdapterType, Is.EqualTo(typeof(CatLavaRushInGameAccessAdapter)));
            Assert.That(descriptor.LobbyKey, Is.Not.EqualTo(descriptor.LobbyAdapterType.Name));
            Assert.That(descriptor.InGameKey, Is.Not.EqualTo(descriptor.InGameAdapterType.Name));
            Assert.That(descriptor.SlotOrder, Is.EqualTo(2));

            second.Dispose();
            Assert.That(registered, Is.EqualTo(1));
            first.Dispose();
            Assert.That(registered, Is.Zero);
        }

        [Test]
        public async Task LobbyAccess_RechecksPostAwaitStateAndRetries()
        {
            bool active = false;
            int openCount = 0;
            var countdown = new TestCountdownScheduler();
            var service = CreateAccessService(
                () => active,
                () => true,
                () => openCount++);
            var adapter = new CatLavaRushLobbyAccessAdapter(service, countdown);
            TMP_Text firstTimer = CreateTimer("first");
            TMP_Text secondTimer = CreateTimer("second");

            await Task.Yield();
            Assert.That(adapter.TryBind(firstTimer), Is.False);
            active = true;
            await Task.Yield();
            Assert.That(adapter.TryBind(firstTimer), Is.True);
            Assert.That(adapter.TryBind(firstTimer), Is.True);
            Assert.That(adapter.TryBind(secondTimer), Is.False);
            Assert.That(adapter.TryActivate(
                CancellationToken.None,
                null,
                out IDisposable lifetime), Is.True);
            Assert.That(countdown.Target, Is.SameAs(firstTimer));
            Assert.That(countdown.Formatter(TimeSpan.FromHours(27)), Is.EqualTo("27:00:00"));
            Assert.That(adapter.TryOpenContent(), Is.True);
            Assert.That(openCount, Is.EqualTo(1));

            lifetime.Dispose();
            Assert.That(countdown.CancellationToken.IsCancellationRequested, Is.True);
            active = false;
            Assert.That(adapter.TryOpenContent(), Is.False);

            adapter.Clear();
            active = true;
            Assert.That(adapter.TryBind(secondTimer), Is.True);
        }

        [Test]
        public void InGameAccess_ValidatesBindingAndOwnsFrameAndCountdownLifetime()
        {
            bool active = true;
            int openCount = 0;
            int frameCalls = 0;
            var frame = new TestFrameScheduler();
            var countdown = new TestCountdownScheduler();
            var service = CreateAccessService(
                () => active,
                () => true,
                () => openCount++);
            var adapter = new CatLavaRushInGameAccessAdapter(service, frame, countdown);
            TMP_Text timer = CreateTimer("cell");
            var invalidProgress = new TestProgressView(null);
            var progress = CreateProgressView();

            Assert.That(adapter.TryBind(timer, invalidProgress), Is.False);
            Assert.That(adapter.TryBind(timer, progress), Is.True);
            Assert.That(adapter.TryActivate(
                CancellationToken.None,
                null,
                _ => frameCalls++,
                out IDisposable lifetime), Is.True);
            frame.Publish(0.2f);
            adapter.NotifyProgressArrived();

            Assert.That(frameCalls, Is.EqualTo(1));
            Assert.That(progress.ArrivalCount, Is.EqualTo(1));
            Assert.That(adapter.TryOpenContent(), Is.True);
            Assert.That(openCount, Is.EqualTo(1));

            lifetime.Dispose();
            frame.Publish(0.2f);
            Assert.That(frameCalls, Is.EqualTo(1));
            Assert.That(countdown.CancellationToken.IsCancellationRequested, Is.True);
        }

        [Test]
        public async Task DynamicController_SharesColdPrewarmAndConcurrentRequests()
        {
            int createCount = 0;
            int destroyCount = 0;
            CatLavaRushDynamicControllerRequest request = default;
            var completion =
                new TaskCompletionSource<CatLavaRushDynamicControllerInstance>();
            CatLavaRushDynamicControllerInstance second = null;
            var binding = new CatLavaRushDynamicControllerBinding(
                value =>
                {
                    request = value;
                    createCount++;
                    if (createCount == 1)
                    {
                        return completion.Task;
                    }

                    second = CreateControllerInstance("second");
                    return Task.FromResult(second);
                },
                root =>
                {
                    destroyCount++;
                    UnityEngine.Object.DestroyImmediate(root);
                });
            var controller = new CatLavaRushDynamicController(binding);

            Task<LavaRushBootstrap> cold = controller.GetAsync();
            Task<LavaRushBootstrap> concurrent = controller.GetAsync();
            Task<LavaRushBootstrap> prewarm = controller.PrewarmAsync();
            CatLavaRushDynamicControllerInstance instance = CreateControllerInstance("first");
            completion.SetResult(instance);

            LavaRushBootstrap first = await cold;
            Assert.That(concurrent, Is.SameAs(cold));
            Assert.That(prewarm, Is.SameAs(cold));
            Assert.That(await concurrent, Is.SameAs(first));
            Assert.That(await prewarm, Is.SameAs(first));
            Assert.That(await controller.GetAsync(), Is.SameAs(first));
            Assert.That(createCount, Is.EqualTo(1));
            Assert.That(destroyCount, Is.Zero);
            Assert.That(request.AddressableKey, Is.EqualTo("UI_LavaRush"));
            Assert.That(request.CanvasType, Is.EqualTo(CatLavaRushCanvasType.Half));
            Assert.That(request.EnsureCamera, Is.True);
            Assert.That(request.CaptureFonts, Is.True);

            controller.Clear();
            Assert.That(controller.Controller, Is.Null);
            Assert.That(instance.Root, Is.Not.Null);
            Assert.That(destroyCount, Is.Zero);
            Assert.That(await controller.GetAsync(), Is.SameAs(second.Controller));
            Assert.That(createCount, Is.EqualTo(2));
            Assert.That(destroyCount, Is.Zero);
        }

        [Test]
        public async Task DynamicController_CleansPartialFailureAndRetries()
        {
            int createCount = 0;
            int destroyCount = 0;
            var valid = CreateControllerInstance("valid");
            var binding = new CatLavaRushDynamicControllerBinding(
                _ =>
                {
                    createCount++;
                    if (createCount == 1)
                    {
                        GameObject root = CreateObject("partial-root");
                        GameObject detached = CreateObject("detached-controller");
                        var detachedController = detached.AddComponent<LavaRushBootstrap>();
                        return Task.FromResult(
                            new CatLavaRushDynamicControllerInstance(root, detachedController));
                    }

                    return Task.FromResult(valid);
                },
                root =>
                {
                    destroyCount++;
                    UnityEngine.Object.DestroyImmediate(root);
                });
            var controller = new CatLavaRushDynamicController(binding);

            Assert.That(await controller.GetAsync(), Is.Null);
            Assert.That(controller.IsLoading, Is.False);
            Assert.That(destroyCount, Is.EqualTo(1));
            Assert.That(await controller.GetAsync(), Is.SameAs(valid.Controller));
            Assert.That(createCount, Is.EqualTo(2));
        }

        [Test]
        public async Task DynamicController_ReloadsAfterTheCachedUnityObjectIsDestroyed()
        {
            int createCount = 0;
            CatLavaRushDynamicControllerInstance first = CreateControllerInstance("first");
            CatLavaRushDynamicControllerInstance second = CreateControllerInstance("second");
            var binding = new CatLavaRushDynamicControllerBinding(
                _ =>
                {
                    createCount++;
                    return Task.FromResult(createCount == 1 ? first : second);
                },
                root => UnityEngine.Object.DestroyImmediate(root));
            var controller = new CatLavaRushDynamicController(binding);

            Assert.That(await controller.GetAsync(), Is.SameAs(first.Controller));
            UnityEngine.Object.DestroyImmediate(first.Root);
            Assert.That(controller.Controller, Is.Null);
            Assert.That(await controller.GetAsync(), Is.SameAs(second.Controller));
            Assert.That(createCount, Is.EqualTo(2));
        }

        [Test]
        public void DynamicController_OuterBindingDoesNotOwnInnerPresentationHost()
        {
            Type hostType = typeof(ILavaRushUIViewHost);
            Type[] bindingSurface = typeof(CatLavaRushDynamicControllerBinding)
                .GetProperties()
                .Select(property => property.PropertyType)
                .Concat(typeof(CatLavaRushDynamicControllerBinding)
                    .GetConstructors()
                    .SelectMany(constructor => constructor.GetParameters())
                    .Select(parameter => parameter.ParameterType))
                .ToArray();

            Assert.That(bindingSurface, Has.None.EqualTo(hostType));
            Assert.That(typeof(CatLavaRushDynamicControllerBinding).Assembly,
                Is.Not.EqualTo(hostType.Assembly));
        }

        private CatLavaRushAccessService CreateAccessService(
            Func<bool> active,
            Func<bool> started,
            Action open)
        {
            return new CatLavaRushAccessService(
                new CatLavaRushAccessBinding(
                    active,
                    started,
                    () => new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                    () => TimeSpan.FromHours(5),
                    open));
        }

        private TestProgressView CreateProgressView()
        {
            GameObject target = CreateObject("progress", typeof(RectTransform));
            return new TestProgressView(target.GetComponent<RectTransform>());
        }

        private TMP_Text CreateTimer(string name)
        {
            return CreateObject(name, typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
        }

        private CatLavaRushDynamicControllerInstance CreateControllerInstance(string name)
        {
            GameObject root = CreateObject(name);
            LavaRushBootstrap controller = root.AddComponent<LavaRushBootstrap>();
            return new CatLavaRushDynamicControllerInstance(root, controller);
        }

        private GameObject CreateObject(string name, params Type[] components)
        {
            var value = new GameObject(name, components);
            _objects.Add(value);
            return value;
        }

        private sealed class TestProgressView : ILavaRushProgressView
        {
            public TestProgressView(RectTransform targetProgress)
            {
                TargetProgress = targetProgress;
            }

            public RectTransform TargetProgress { get; }
            public int ArrivalCount { get; private set; }

            public void NotifyProgressArrived() => ArrivalCount++;
        }

        private sealed class TestFrameScheduler : ILavaRushFrameScheduler
        {
            private Action<float> _update;

            public IDisposable SubscribeUpdate(Action<float> handler)
            {
                _update += handler;
                return new CallbackDisposable(() => _update -= handler);
            }

            public IDisposable SubscribeLateUpdate(Action<float> handler) =>
                new CallbackDisposable(() => { });

            public IDisposable SubscribeEverySecond(Action handler) =>
                new CallbackDisposable(() => { });

            public void Publish(float deltaTime) => _update?.Invoke(deltaTime);
        }

        private sealed class TestCountdownScheduler : ILavaRushCountdownScheduler
        {
            public DateTime Now => new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public TMP_Text Target { get; private set; }
            public CancellationToken CancellationToken { get; private set; }
            public Func<TimeSpan, string> Formatter { get; private set; }

            public bool TryGetNow(out DateTime now)
            {
                now = Now;
                return true;
            }

            public void Register(
                TMP_Text target,
                DateTime endTime,
                CancellationToken cancellationToken,
                Action onExpired = null,
                Func<TimeSpan, string> formatter = null)
            {
                Target = target;
                CancellationToken = cancellationToken;
                Formatter = formatter;
            }
        }

        private sealed class CallbackDisposable : IDisposable
        {
            private Action _dispose;

            public CallbackDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                Action dispose = _dispose;
                _dispose = null;
                dispose?.Invoke();
            }
        }
    }
}
