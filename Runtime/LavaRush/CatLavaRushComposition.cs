using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActionFit.Cat.App.Order;
using ActionFit.Content;
using ActionFit.LavaRush;
using ActionFit.LavaRush.UI;
using ActionFit.Time;
using UnityEngine;

namespace ActionFit.Cat.App.LavaRush
{
    /// <summary>Immutable engine inputs selected by the Cat project composition seam.</summary>
    public sealed class CatLavaRushEngineBinding
    {
        public CatLavaRushEngineBinding(
            ContentStateStoreBase stateStore,
            ContentRewardServiceBase rewardService,
            LavaRushCatalogResolverBase catalogResolver,
            ClockBase utcClock,
            TimeZoneInfo calendarTimeZone,
            ILavaRushLegacyLocalClock legacyLocalClock,
            LavaRushRandomBase random,
            LavaRushSeatCurveProviderBase seatCurveProvider,
            LavaRushAccessPolicyBase accessPolicy,
            LavaRushSchedulePolicyBase schedulePolicy,
            LavaRushAnalyticsSinkBase analytics,
            CatLavaRushPersistenceOwner persistenceOwner,
            Func<bool> isPersistenceReady,
            Func<DateTime> getLocalNow,
            Action<Vector3> setRewardOrigin,
            Action clearRewardOrigin,
            TimeSpan calendarDayBoundaryOffset)
        {
            StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            RewardService = rewardService ?? throw new ArgumentNullException(nameof(rewardService));
            CatalogResolver = catalogResolver ?? throw new ArgumentNullException(nameof(catalogResolver));
            UtcClock = utcClock ?? throw new ArgumentNullException(nameof(utcClock));
            CalendarTimeZone = calendarTimeZone
                ?? throw new ArgumentNullException(nameof(calendarTimeZone));
            LegacyLocalClock = legacyLocalClock
                ?? throw new ArgumentNullException(nameof(legacyLocalClock));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            SeatCurveProvider = seatCurveProvider
                ?? throw new ArgumentNullException(nameof(seatCurveProvider));
            AccessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
            SchedulePolicy = schedulePolicy ?? throw new ArgumentNullException(nameof(schedulePolicy));
            Analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            PersistenceOwner = persistenceOwner
                ?? throw new ArgumentNullException(nameof(persistenceOwner));
            IsPersistenceReady = isPersistenceReady
                ?? throw new ArgumentNullException(nameof(isPersistenceReady));
            GetLocalNow = getLocalNow ?? throw new ArgumentNullException(nameof(getLocalNow));
            SetRewardOrigin = setRewardOrigin
                ?? throw new ArgumentNullException(nameof(setRewardOrigin));
            ClearRewardOrigin = clearRewardOrigin
                ?? throw new ArgumentNullException(nameof(clearRewardOrigin));
            CalendarDayBoundaryOffset = calendarDayBoundaryOffset;
        }

        public ContentStateStoreBase StateStore { get; }
        public ContentRewardServiceBase RewardService { get; }
        public LavaRushCatalogResolverBase CatalogResolver { get; }
        public ClockBase UtcClock { get; }
        public TimeZoneInfo CalendarTimeZone { get; }
        public ILavaRushLegacyLocalClock LegacyLocalClock { get; }
        public LavaRushRandomBase Random { get; }
        public LavaRushSeatCurveProviderBase SeatCurveProvider { get; }
        public LavaRushAccessPolicyBase AccessPolicy { get; }
        public LavaRushSchedulePolicyBase SchedulePolicy { get; }
        public LavaRushAnalyticsSinkBase Analytics { get; }
        public CatLavaRushPersistenceOwner PersistenceOwner { get; }
        public Func<bool> IsPersistenceReady { get; }
        public Func<DateTime> GetLocalNow { get; }
        public Action<Vector3> SetRewardOrigin { get; }
        public Action ClearRewardOrigin { get; }
        public TimeSpan CalendarDayBoundaryOffset { get; }
    }

    /// <summary>One initialized set of controller services and their project-owned lifetime.</summary>
    public sealed class CatLavaRushControllerRuntime : IDisposable
    {
        private IDisposable _lifetime;

        public CatLavaRushControllerRuntime(
            ILavaRushFrameScheduler frameScheduler,
            ILavaRushCountdownScheduler countdownScheduler,
            LavaRushAudioBase audio,
            ILavaRushUILocalizer localizer,
            LavaRushUIRewardRendererBase rewardRenderer,
            LavaRushProfileRosterBase profiles,
            LavaRushProfileGroupFactoryBase profileGroupFactory,
            LavaRushTutorialFocusSpriteProviderBase tutorialFocusSprites,
            LavaRushRewardPresentationProviderBase rewardPresentation,
            IDisposable lifetime)
        {
            FrameScheduler = frameScheduler
                ?? throw new ArgumentNullException(nameof(frameScheduler));
            CountdownScheduler = countdownScheduler
                ?? throw new ArgumentNullException(nameof(countdownScheduler));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            RewardRenderer = rewardRenderer
                ?? throw new ArgumentNullException(nameof(rewardRenderer));
            Profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
            ProfileGroupFactory = profileGroupFactory
                ?? throw new ArgumentNullException(nameof(profileGroupFactory));
            TutorialFocusSprites = tutorialFocusSprites
                ?? throw new ArgumentNullException(nameof(tutorialFocusSprites));
            RewardPresentation = rewardPresentation
                ?? throw new ArgumentNullException(nameof(rewardPresentation));
            _lifetime = lifetime;
        }

        public ILavaRushFrameScheduler FrameScheduler { get; }
        public ILavaRushCountdownScheduler CountdownScheduler { get; }
        public LavaRushAudioBase Audio { get; }
        public ILavaRushUILocalizer Localizer { get; }
        public LavaRushUIRewardRendererBase RewardRenderer { get; }
        public LavaRushProfileRosterBase Profiles { get; }
        public LavaRushProfileGroupFactoryBase ProfileGroupFactory { get; }
        public LavaRushTutorialFocusSpriteProviderBase TutorialFocusSprites { get; }
        public LavaRushRewardPresentationProviderBase RewardPresentation { get; }

        public void Dispose()
        {
            IDisposable lifetime = Interlocked.Exchange(ref _lifetime, null);
            lifetime?.Dispose();
        }
    }

    /// <summary>Controller loading plus neutral Order completion leaves.</summary>
    public sealed class CatLavaRushControllerBinding
    {
        public CatLavaRushControllerBinding(
            CatLavaRushDynamicControllerBinding dynamicController,
            Func<CatLavaRushControllerRuntime> createRuntime,
            Func<Action<CatOrderCompletionSnapshot>, IDisposable> subscribeOrderCompleted,
            Func<int, int> resolveOrderProgress,
            Func<object, ILavaRushProgressView> resolveMatchingOrderProgressView,
            Action<ILavaRushProgressView, int> playOrderRewardEffect,
            Action<CatLavaRushOrderRewardAdapter> registerOrderReward,
            Action<CatLavaRushOrderRewardAdapter> unregisterOrderReward)
        {
            DynamicController = dynamicController
                ?? throw new ArgumentNullException(nameof(dynamicController));
            CreateRuntime = createRuntime ?? throw new ArgumentNullException(nameof(createRuntime));
            SubscribeOrderCompleted = subscribeOrderCompleted
                ?? throw new ArgumentNullException(nameof(subscribeOrderCompleted));
            ResolveOrderProgress = resolveOrderProgress
                ?? throw new ArgumentNullException(nameof(resolveOrderProgress));
            ResolveMatchingOrderProgressView = resolveMatchingOrderProgressView
                ?? throw new ArgumentNullException(nameof(resolveMatchingOrderProgressView));
            PlayOrderRewardEffect = playOrderRewardEffect
                ?? throw new ArgumentNullException(nameof(playOrderRewardEffect));
            RegisterOrderReward = registerOrderReward
                ?? throw new ArgumentNullException(nameof(registerOrderReward));
            UnregisterOrderReward = unregisterOrderReward
                ?? throw new ArgumentNullException(nameof(unregisterOrderReward));
        }

        public CatLavaRushDynamicControllerBinding DynamicController { get; }
        public Func<CatLavaRushControllerRuntime> CreateRuntime { get; }
        public Func<Action<CatOrderCompletionSnapshot>, IDisposable>
            SubscribeOrderCompleted { get; }
        public Func<int, int> ResolveOrderProgress { get; }
        public Func<object, ILavaRushProgressView> ResolveMatchingOrderProgressView { get; }
        public Action<ILavaRushProgressView, int> PlayOrderRewardEffect { get; }
        public Action<CatLavaRushOrderRewardAdapter> RegisterOrderReward { get; }
        public Action<CatLavaRushOrderRewardAdapter> UnregisterOrderReward { get; }
    }

    /// <summary>Primitive merge fact copied from the current Cat gameplay publication.</summary>
    public readonly struct CatLavaRushMergeSnapshot
    {
        public CatLavaRushMergeSnapshot(int level, bool isEligibleGroup, Vector3 worldPosition)
        {
            Level = level;
            IsEligibleGroup = isEligibleGroup;
            WorldPosition = worldPosition;
        }

        public int Level { get; }
        public bool IsEligibleGroup { get; }
        public Vector3 WorldPosition { get; }
    }

    /// <summary>Project event, access-registry, and diagnostics leaves.</summary>
    public sealed class CatLavaRushShellBinding
    {
        public CatLavaRushShellBinding(
            Func<Action, IDisposable> subscribeSwitchedInOut,
            Func<Action, IDisposable> subscribeNextDay,
            Func<Action, IDisposable> subscribeEventUnlock,
            Func<Action<CatLavaRushMergeSnapshot>, IDisposable> subscribeMerged,
            Func<int, int> resolveMergeProgress,
            Action<Vector3, int> playMergeRewardEffect,
            Action refreshAccess,
            Action<CatLavaRushEventAccessDescriptor> registerEventAccess,
            Action<CatLavaRushEventAccessDescriptor> unregisterEventAccess,
            Func<bool> isPrewarmReady,
            Action<string> report,
            Action<string> reportError)
        {
            SubscribeSwitchedInOut = subscribeSwitchedInOut
                ?? throw new ArgumentNullException(nameof(subscribeSwitchedInOut));
            SubscribeNextDay = subscribeNextDay
                ?? throw new ArgumentNullException(nameof(subscribeNextDay));
            SubscribeEventUnlock = subscribeEventUnlock
                ?? throw new ArgumentNullException(nameof(subscribeEventUnlock));
            SubscribeMerged = subscribeMerged
                ?? throw new ArgumentNullException(nameof(subscribeMerged));
            ResolveMergeProgress = resolveMergeProgress
                ?? throw new ArgumentNullException(nameof(resolveMergeProgress));
            PlayMergeRewardEffect = playMergeRewardEffect
                ?? throw new ArgumentNullException(nameof(playMergeRewardEffect));
            RefreshAccess = refreshAccess ?? throw new ArgumentNullException(nameof(refreshAccess));
            RegisterEventAccess = registerEventAccess
                ?? throw new ArgumentNullException(nameof(registerEventAccess));
            UnregisterEventAccess = unregisterEventAccess
                ?? throw new ArgumentNullException(nameof(unregisterEventAccess));
            IsPrewarmReady = isPrewarmReady
                ?? throw new ArgumentNullException(nameof(isPrewarmReady));
            Report = report ?? throw new ArgumentNullException(nameof(report));
            ReportError = reportError ?? throw new ArgumentNullException(nameof(reportError));
        }

        public Func<Action, IDisposable> SubscribeSwitchedInOut { get; }
        public Func<Action, IDisposable> SubscribeNextDay { get; }
        public Func<Action, IDisposable> SubscribeEventUnlock { get; }
        public Func<Action<CatLavaRushMergeSnapshot>, IDisposable> SubscribeMerged { get; }
        public Func<int, int> ResolveMergeProgress { get; }
        public Action<Vector3, int> PlayMergeRewardEffect { get; }
        public Action RefreshAccess { get; }
        public Action<CatLavaRushEventAccessDescriptor> RegisterEventAccess { get; }
        public Action<CatLavaRushEventAccessDescriptor> UnregisterEventAccess { get; }
        public Func<bool> IsPrewarmReady { get; }
        public Action<string> Report { get; }
        public Action<string> ReportError { get; }
    }

    /// <summary>
    /// Sole Cat Lava Rush product authority. It owns one engine, controller composition, and
    /// deterministic subscription lifetime while every project dependency remains injected.
    /// </summary>
    public sealed class CatLavaRushComposition
    {
        public const string RuntimeContentId = "cat-merge/lava-rush";

        private readonly CatLavaRushEngineBinding _engineBinding;
        private readonly CatLavaRushControllerBinding _controllerBinding;
        private readonly CatLavaRushShellBinding _shellBinding;
        private readonly List<IDisposable> _subscriptions = new();

        private LavaRushEngine _engine;
        private CatLavaRushControllerRuntime _controllerRuntime;
        private CatLavaRushDynamicController _dynamicController;
        private CatLavaRushOrderProgressSource _orderProgress;
        private IDisposable _eventAccessLifetime;
        private CancellationTokenSource _prewarmCancellation;
        private bool _initialized;
        private bool _runtimeInitializing;
        private bool _runtimeReady;
        private bool _runtimeErrorLogged;

        public CatLavaRushComposition(
            CatLavaRushEngineBinding engineBinding,
            CatLavaRushControllerBinding controllerBinding,
            CatLavaRushShellBinding shellBinding)
        {
            _engineBinding = engineBinding
                ?? throw new ArgumentNullException(nameof(engineBinding));
            _controllerBinding = controllerBinding
                ?? throw new ArgumentNullException(nameof(controllerBinding));
            _shellBinding = shellBinding
                ?? throw new ArgumentNullException(nameof(shellBinding));
        }

        public LavaRushEngine Engine => _engine;
        public global::UI_LavaRush Controller => _dynamicController?.Controller;
        public global::UI_LavaRush_EventStart StartEvent => Controller?.refs?.uiEventStart;
        public global::ViewController StartPopup => Controller?.PackageFlow ?? StartEvent;
        public global::UI_LavaRush_OrderReward OrderRewardEffect =>
            Controller != null
                ? Controller.GetComponent<global::UI_LavaRush_OrderReward>()
                : null;

        public bool IsEventStarted => EnsureRuntimeReady() && _engine.IsEventStarted;
        public bool IsEventActive => EnsureRuntimeReady() && _engine.IsEventActive;
        public bool HasValidTimeMetadata =>
            EnsureRuntimeReady() && _engine.HasValidTimeMetadata;
        public bool IsContentUnlocked => _engineBinding.AccessPolicy.IsAccessAllowed;
        public bool IsEventDay => EnsureRuntimeReady() && _engine.IsEventDay;
        public DateTime EventEndTime => _engineBinding.GetLocalNow() + EventRemainTime;
        public TimeSpan EventRemainTime =>
            EnsureRuntimeReady() ? _engine.EventRemainingTime : TimeSpan.Zero;
        public TimeSpan ExpectedRemainTime =>
            EnsureRuntimeReady() ? _engine.ExpectedRemainingTime : TimeSpan.Zero;

        public bool PendingEnd
        {
            get => EnsureRuntimeReady() && _engine.PendingEnd;
            set
            {
                if (EnsureRuntimeReady())
                    _engine.SetPendingEnd(value);
            }
        }

        public int SelectedDifficulty =>
            EnsureRuntimeReady() ? _engine.SelectedDifficulty : LavaRushEngine.NoDifficulty;
        public bool IsTutorialDone => EnsureRuntimeReady() && _engine.TutorialDone;
        public int CurrentStage =>
            EnsureRuntimeReady() ? _engine.Stage : LavaRushEngine.MinStage;
        public int StageProgress => EnsureRuntimeReady() ? _engine.StageProgress : 0;
        public bool AllStagesComplete =>
            EnsureRuntimeReady() && _engine.AllStagesComplete;
        public LavaRushResult PendingResult =>
            EnsureRuntimeReady() ? _engine.PendingResult : LavaRushResult.None;
        public long StageStartTicks => EnsureRuntimeReady() ? _engine.StageStartTicks : 0L;
        public int StageLimitSeconds =>
            EnsureRuntimeReady() ? _engine.StageLimitSeconds : 0;
        public int SeatCurveIndex => EnsureRuntimeReady() ? _engine.SeatCurveIndex : 0;
        public int WinRank => EnsureRuntimeReady() ? _engine.WinRank : 1;
        public int ResultSeatCount => EnsureRuntimeReady() ? _engine.ResultSeatCount : 0;
        public int ResultSeatCapacity =>
            EnsureRuntimeReady() ? _engine.ResultSeatCapacity : 0;
        public int StageCount => EnsureRuntimeReady() ? _engine.StageCount : 0;
        public int RequiredProgress => EnsureRuntimeReady() ? _engine.RequiredProgress : 0;
        public int SeatCapacity => EnsureRuntimeReady() ? _engine.SeatCapacity : 0;
        public int FakeSeatCount => EnsureRuntimeReady() ? _engine.FakeSeatCount : 0;
        public bool IsStagePlaying => EnsureRuntimeReady() && _engine.IsStagePlaying;
        public bool IsStageGoalReached =>
            EnsureRuntimeReady() && _engine.IsStageGoalReached;
        public bool IsFinalFoothold => EnsureRuntimeReady() && _engine.IsFinalFoothold;
        public TimeSpan StageRemainTime =>
            EnsureRuntimeReady() ? _engine.StageRemainingTime : TimeSpan.Zero;

        public Task InitializeAsync()
        {
            if (_initialized)
                return Task.CompletedTask;

            try
            {
                _engine = new LavaRushEngine(
                    _engineBinding.StateStore,
                    _engineBinding.RewardService,
                    _engineBinding.CatalogResolver,
                    _engineBinding.UtcClock,
                    _engineBinding.CalendarTimeZone,
                    _engineBinding.LegacyLocalClock,
                    _engineBinding.Random,
                    _engineBinding.SeatCurveProvider,
                    RuntimeContentId,
                    _engineBinding.AccessPolicy,
                    _engineBinding.SchedulePolicy,
                    _engineBinding.Analytics,
                    _engineBinding.CalendarDayBoundaryOffset);
                _controllerRuntime = _controllerBinding.CreateRuntime()
                    ?? throw new InvalidOperationException(
                        "Cat Lava Rush controller runtime is unavailable.");

                var orderReward = new CatLavaRushOrderRewardAdapter(
                    new CatLavaRushOrderRewardBinding(
                        IsOrderProgressActive,
                        _controllerBinding.ResolveOrderProgress,
                        _controllerBinding.ResolveMatchingOrderProgressView,
                        _controllerBinding.PlayOrderRewardEffect,
                        _controllerBinding.RegisterOrderReward,
                        _controllerBinding.UnregisterOrderReward));
                _orderProgress = new CatLavaRushOrderProgressSource(
                    _controllerBinding.SubscribeOrderCompleted,
                    orderReward,
                    ObserveFailure);
                _dynamicController = new CatLavaRushDynamicController(
                    _controllerBinding.DynamicController,
                    InitializeController,
                    ObserveFailure);

                _engine.StateChanged += OnRuntimeStateChanged;
                AddSubscription(_shellBinding.SubscribeSwitchedInOut(OnSwitchedInOut));
                AddSubscription(_shellBinding.SubscribeNextDay(OnNextDay));
                AddSubscription(_shellBinding.SubscribeEventUnlock(OnEventUnlock));
                AddSubscription(_shellBinding.SubscribeMerged(OnMerged));

                var accessRegistration = new CatLavaRushEventAccessRegistration(
                    new CatLavaRushEventAccessRegistryBinding(
                        _shellBinding.RegisterEventAccess,
                        _shellBinding.UnregisterEventAccess));
                _eventAccessLifetime = accessRegistration.Attach();
                _prewarmCancellation = new CancellationTokenSource();
                _initialized = true;
                _ = PrewarmAsync(_prewarmCancellation.Token);
                return Task.CompletedTask;
            }
            catch
            {
                Clear();
                throw;
            }
        }

        public void Clear()
        {
            _prewarmCancellation?.Cancel();
            _prewarmCancellation?.Dispose();
            _prewarmCancellation = null;

            _eventAccessLifetime?.Dispose();
            _eventAccessLifetime = null;
            for (int index = _subscriptions.Count - 1; index >= 0; index--)
                _subscriptions[index]?.Dispose();
            _subscriptions.Clear();

            if (_engine != null)
                _engine.StateChanged -= OnRuntimeStateChanged;
            _dynamicController?.Clear();
            _dynamicController = null;
            _orderProgress = null;
            _controllerRuntime?.Dispose();
            _controllerRuntime = null;
            _engine = null;
            _runtimeInitializing = false;
            _runtimeReady = false;
            _runtimeErrorLogged = false;
            _initialized = false;
        }

        public async Task<global::UI_LavaRush> GetAsync()
        {
            while (!_engineBinding.IsPersistenceReady())
                await Task.Yield();

            if (!EnsureRuntimeReady())
                return null;
            return await _dynamicController.GetAsync();
        }

        public void OpenContent()
        {
            if (Controller != null)
            {
                Controller.OpenMatchFlow();
                return;
            }

            _ = OpenContentAsync();
        }

        public void ClearAllEventData()
        {
            _engineBinding.PersistenceOwner.Reset();
            _runtimeReady = false;
            _runtimeErrorLogged = false;
            _shellBinding.Report(
                "All Lava Rush data was cleared. Restart the app to fully apply the reset.");
        }

        public void SaveEventEndTime()
        {
            if (!EnsureRuntimeReady() || !_engine.TryStartEvent())
                return;
            SetIconsActive();
        }

        public void EndEvent()
        {
            if (!EnsureRuntimeReady())
                return;
            _engine.EndEvent();
            SetIconsActive();
        }

        public void CheckEventTimeout()
        {
            if (!EnsureRuntimeReady())
                return;
            _engine.EvaluateEventTimeout();
            if (PendingEnd)
                Controller?.TryOpenEndPopup();
        }

        public void SetIconsActive() => _shellBinding.RefreshAccess();

        public bool TryGetRuntimeEngine(out LavaRushEngine engine)
        {
            if (EnsureRuntimeReady())
            {
                engine = _engine;
                return true;
            }

            engine = null;
            return false;
        }

        public bool TryPrepareTimerWrite(out long nowTicks)
        {
            nowTicks = 0L;
            if (!EnsureRuntimeReady())
                return false;
            nowTicks = _engine.State.TimeBasis == LavaRushTimeBasis.LegacyLocalTicks
                ? _engineBinding.GetLocalNow().Ticks
                : _engineBinding.UtcClock.UtcNow.Ticks;
            return true;
        }

        public TimeSpan GetTimerRemaining(long deadlineTicks) =>
            EnsureRuntimeReady() ? _engine.GetRemaining(deadlineTicks) : TimeSpan.Zero;

        public bool HasTimerReachedDeadline(long deadlineTicks) =>
            deadlineTicks > 0
            && EnsureRuntimeReady()
            && _engine.GetRemaining(deadlineTicks) <= TimeSpan.Zero;

        public int GetTimerElapsedSeconds(long startTicks) =>
            EnsureRuntimeReady() ? _engine.GetElapsedSeconds(startTicks) : 0;

        public void ResetGameplay()
        {
            if (EnsureRuntimeReady())
                _engine.ResetGameplay();
        }

        public bool SelectDifficulty(int difficulty) =>
            EnsureRuntimeReady() && _engine.SelectDifficulty(difficulty);

        public void SetTutorialDone(bool done)
        {
            if (EnsureRuntimeReady())
                _engine.SetTutorialDone(done);
        }

        public bool StartStage() => EnsureRuntimeReady() && _engine.StartStage();

        public LavaRushResult AddProgress(int amount) =>
            EnsureRuntimeReady() ? _engine.AddProgress(amount) : LavaRushResult.None;

        public LavaRushResult EvaluateStageResult() =>
            EnsureRuntimeReady() ? _engine.EvaluateStageResult() : LavaRushResult.None;

        public LavaRushResult ForceWin() =>
            EnsureRuntimeReady() ? _engine.ForceWin() : LavaRushResult.None;

        public LavaRushResult ForceLose() =>
            EnsureRuntimeReady() ? _engine.ForceLose() : LavaRushResult.None;

        public bool ClaimPendingReward(Vector3 grantOrigin)
        {
            if (!EnsureRuntimeReady())
                return false;

            _engineBinding.SetRewardOrigin(grantOrigin);
            try
            {
                return _engine.ClaimPendingReward();
            }
            finally
            {
                _engineBinding.ClearRewardOrigin();
            }
        }

        public bool IsStageRewardClaimed(int stage) =>
            EnsureRuntimeReady() && _engine.IsStageRewardClaimed(stage);

        public void ClearPendingResult()
        {
            if (EnsureRuntimeReady())
                _engine.ClearPendingResult();
        }

        private void InitializeController(global::UI_LavaRush controller)
        {
            var access = new CatLavaRushAccessService(
                new CatLavaRushAccessBinding(
                    () => IsEventActive,
                    () => IsEventStarted,
                    () => EventEndTime,
                    () => EventRemainTime,
                    OpenContent));
            var context = new LavaRushControllerContext(
                _engine,
                _controllerRuntime.FrameScheduler,
                _controllerRuntime.CountdownScheduler,
                _controllerRuntime.Audio,
                _controllerRuntime.Localizer,
                _controllerRuntime.RewardRenderer,
                _controllerRuntime.Profiles,
                _orderProgress,
                access,
                ClaimPendingReward,
                SetIconsActive,
                _controllerRuntime.ProfileGroupFactory,
                _controllerRuntime.TutorialFocusSprites,
                _controllerRuntime.RewardPresentation);
            controller.Initialize(context, false);
            if (controller.GetComponent<global::UI_LavaRush_OrderReward>() == null)
                controller.gameObject.AddComponent<global::UI_LavaRush_OrderReward>();
            SetIconsActive();
        }

        private async Task OpenContentAsync()
        {
            try
            {
                global::UI_LavaRush controller = await GetAsync();
                controller?.OpenMatchFlow();
            }
            catch (Exception exception)
            {
                ObserveFailure(exception);
            }
        }

        private async Task PrewarmAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!_shellBinding.IsPrewarmReady())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                if (!EnsureRuntimeReady())
                    return;
                CheckEventTimeout();
                if (Controller != null || !ShouldBeLoaded())
                    return;
                await GetAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ObserveFailure(exception);
            }
        }

        private bool ShouldBeLoaded()
        {
            if (!IsContentUnlocked || !_engineBinding.SchedulePolicy.IsEnabled)
                return false;
            return IsEventStarted || PendingEnd || IsEventDay;
        }

        private bool EnsureRuntimeReady()
        {
            if (_runtimeReady)
                return true;
            if (_runtimeInitializing
                || _engine == null
                || !_engineBinding.IsPersistenceReady())
            {
                return false;
            }

            _runtimeInitializing = true;
            try
            {
                _engineBinding.PersistenceOwner.ImportIfNeeded(_engine);
                _engine.Restore();
                _runtimeReady = true;
                return true;
            }
            catch (Exception exception) when (exception is FormatException
                or NotSupportedException
                or InvalidOperationException)
            {
                if (!_runtimeErrorLogged)
                {
                    _runtimeErrorLogged = true;
                    _shellBinding.ReportError(
                        $"Package runtime is blocked: {exception.Message}");
                }

                return false;
            }
            finally
            {
                _runtimeInitializing = false;
            }
        }

        private bool IsOrderProgressActive() =>
            IsEventActive
            && IsEventStarted
            && IsStagePlaying
            && !AllStagesComplete;

        private void OnSwitchedInOut() => CheckEventTimeout();

        private void OnNextDay()
        {
            CheckEventTimeout();
            if (_prewarmCancellation != null)
                _ = PrewarmAsync(_prewarmCancellation.Token);
        }

        private void OnEventUnlock()
        {
            if (_prewarmCancellation != null)
                _ = PrewarmAsync(_prewarmCancellation.Token);
        }

        private void OnMerged(CatLavaRushMergeSnapshot snapshot)
        {
            if (!snapshot.IsEligibleGroup
                || !IsOrderProgressActive())
            {
                return;
            }

            int progress = _shellBinding.ResolveMergeProgress(snapshot.Level);
            if (progress <= 0)
                return;

            _shellBinding.PlayMergeRewardEffect(snapshot.WorldPosition, progress);
            ApplyProductProgress(progress);
        }

        private void ApplyProductProgress(int amount)
        {
            if (Controller != null)
            {
                Controller.AddProgress(amount);
                return;
            }

            AddProgress(amount);
            global::UI_LavaRush_Cell.NotifyProgressArrived();
        }

        private void OnRuntimeStateChanged(LavaRushState state)
        {
            if (_runtimeReady)
                SetIconsActive();
        }

        private void AddSubscription(IDisposable subscription)
        {
            if (subscription == null)
                throw new InvalidOperationException(
                    "A Cat Lava Rush project subscription returned a null lifetime.");
            _subscriptions.Add(subscription);
        }

        private void ObserveFailure(Exception exception)
        {
            try
            {
                _shellBinding.ReportError(exception?.ToString() ?? "Unknown Lava Rush failure.");
            }
            catch
            {
                // Diagnostics cannot change product state or subscription ordering.
            }
        }
    }
}
