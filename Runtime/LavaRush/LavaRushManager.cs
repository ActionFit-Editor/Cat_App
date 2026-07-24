using System;
using System.Threading.Tasks;
using ActionFit.Cat.App.LavaRush;
using ActionFit.LavaRush;
using UnityEngine;

/// <summary>
/// Package-owned compatibility facade for existing Main.LavaRush callers.
/// Every member delegates to the same CatLavaRushComposition engine.
/// </summary>
public sealed class LavaRushManager
{
    private readonly CatLavaRushComposition _composition;

    public LavaRushManager(CatLavaRushComposition composition)
    {
        _composition = composition ?? throw new ArgumentNullException(nameof(composition));
    }

    public UI_LavaRush Controller => _composition.Controller;
    public UI_LavaRush_EventStart StartEvent => _composition.StartEvent;
    public ViewController StartPopup => _composition.StartPopup;
    public UI_LavaRush_OrderReward OrderRewardEffect => _composition.OrderRewardEffect;

    public bool IsEventStarted => _composition.IsEventStarted;
    public bool PendingEnd
    {
        get => _composition.PendingEnd;
        set => _composition.PendingEnd = value;
    }
    public bool IsEventActive => _composition.IsEventActive;
    public bool HasValidTimeMetadata => _composition.HasValidTimeMetadata;
    public bool IsContentUnlocked => _composition.IsContentUnlocked;
    public bool IsEventDay => _composition.IsEventDay;
    public DateTime EventEndTime => _composition.EventEndTime;
    public TimeSpan EventRemainTime => _composition.EventRemainTime;
    public TimeSpan ExpectedRemainTime => _composition.ExpectedRemainTime;

    public int SelectedDifficulty => _composition.SelectedDifficulty;
    public bool IsTutorialDone => _composition.IsTutorialDone;
    public int CurrentStage => _composition.CurrentStage;
    public int StageProgress => _composition.StageProgress;
    public bool AllStagesComplete => _composition.AllStagesComplete;
    public LavaRushResult PendingResult => _composition.PendingResult;
    public long StageStartTicks => _composition.StageStartTicks;
    public int StageLimitSeconds => _composition.StageLimitSeconds;
    public int SeatCurveIndex => _composition.SeatCurveIndex;
    public int WinRank => _composition.WinRank;
    public int ResultSeatCount => _composition.ResultSeatCount;
    public int ResultSeatCapacity => _composition.ResultSeatCapacity;
    public int StageCount => _composition.StageCount;
    public int RequiredProgress => _composition.RequiredProgress;
    public int SeatCapacity => _composition.SeatCapacity;
    public int FakeSeatCount => _composition.FakeSeatCount;
    public bool IsStagePlaying => _composition.IsStagePlaying;
    public bool IsStageGoalReached => _composition.IsStageGoalReached;
    public bool IsFinalFoothold => _composition.IsFinalFoothold;
    public TimeSpan StageRemainTime => _composition.StageRemainTime;

    public Task InitializeAsync() => _composition.InitializeAsync();
    public void Clear() => _composition.Clear();
    public Task<UI_LavaRush> GetAsync() => _composition.GetAsync();
    public void OpenContent() => _composition.OpenContent();
    public void ClearAllEventData() => _composition.ClearAllEventData();
    public void SaveEventEndTime() => _composition.SaveEventEndTime();
    public void EndEvent() => _composition.EndEvent();
    public void CheckEventTimeout() => _composition.CheckEventTimeout();
    public void SetIconsActive() => _composition.SetIconsActive();

    public bool TryGetRuntimeEngine(out LavaRushEngine engine) =>
        _composition.TryGetRuntimeEngine(out engine);

    public bool TryPrepareTimerWrite(out long nowTicks) =>
        _composition.TryPrepareTimerWrite(out nowTicks);

    public TimeSpan GetTimerRemaining(long deadlineTicks) =>
        _composition.GetTimerRemaining(deadlineTicks);

    public bool HasTimerReachedDeadline(long deadlineTicks) =>
        _composition.HasTimerReachedDeadline(deadlineTicks);

    public int GetTimerElapsedSeconds(long startTicks) =>
        _composition.GetTimerElapsedSeconds(startTicks);

    public void ResetGameplay() => _composition.ResetGameplay();
    public bool SelectDifficulty(int difficulty) => _composition.SelectDifficulty(difficulty);
    public void SetTutorialDone(bool done) => _composition.SetTutorialDone(done);
    public bool StartStage() => _composition.StartStage();
    public LavaRushResult AddProgress(int amount) => _composition.AddProgress(amount);
    public LavaRushResult EvaluateStageResult() => _composition.EvaluateStageResult();
    public LavaRushResult ForceWin() => _composition.ForceWin();
    public LavaRushResult ForceLose() => _composition.ForceLose();
    public bool ClaimPendingReward(Vector3 grantOrigin) =>
        _composition.ClaimPendingReward(grantOrigin);
    public bool IsStageRewardClaimed(int stage) =>
        _composition.IsStageRewardClaimed(stage);
    public void ClearPendingResult() => _composition.ClearPendingResult();
}
