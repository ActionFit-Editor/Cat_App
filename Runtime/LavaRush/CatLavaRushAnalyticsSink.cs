using System;
using System.Collections.Generic;
using ActionFit.LavaRush;

namespace ActionFit.Cat.App
{
    public enum CatAnalyticsRewardKind
    {
        Gold,
        Energy,
        Dia,
        Item,
        Other,
    }

    public readonly struct CatAnalyticsReward
    {
        public CatAnalyticsReward(
            CatAnalyticsRewardKind kind,
            string itemId,
            int amount,
            string otherType = null)
        {
            Kind = kind;
            ItemId = itemId ?? string.Empty;
            Amount = amount;
            OtherType = otherType ?? string.Empty;
        }

        public CatAnalyticsRewardKind Kind { get; }
        public string ItemId { get; }
        public int Amount { get; }
        public string OtherType { get; }

        public string Type => Kind switch
        {
            CatAnalyticsRewardKind.Gold => "gold",
            CatAnalyticsRewardKind.Energy => "energy",
            CatAnalyticsRewardKind.Dia => "dia",
            CatAnalyticsRewardKind.Item => "item",
            _ => string.IsNullOrWhiteSpace(OtherType) ? "other" : OtherType,
        };
    }

    /// <summary>Project Shell conversion of generated reward rows into primitive analytics input.</summary>
    public abstract class CatLavaRushRewardAnalyticsCatalogBase
    {
        public abstract bool TryGet(
            int difficulty,
            int stage,
            out IReadOnlyList<CatAnalyticsReward> rewards);
    }

    /// <summary>
    /// Implements the existing six-method engine sink with Cat event names, schemas, drop rules,
    /// and TD-to-Singular ordering.
    /// </summary>
    public sealed class CatLavaRushAnalyticsSink : LavaRushAnalyticsSinkBase
    {
        public const string EventStartName = "te_lavarush_event_start";
        public const string TutorialCompleteName = "te_lavarush_tutorial_complete";
        public const string StageStartName = "te_lavarush_stage_start";
        public const string StageEndName = "te_lavarush_stage_end";
        public const string RewardClaimName = "te_lavarush_reward_claim";
        public const string EventEndName = "te_lavarush_event_end";

        private readonly CatAnalyticsRouter _router;
        private readonly CatLavaRushRewardAnalyticsCatalogBase _rewards;

        public CatLavaRushAnalyticsSink(
            CatAnalyticsRouter router,
            CatLavaRushRewardAnalyticsCatalogBase rewards)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        }

        public override void EventStarted(int remainingSeconds)
        {
            _router.Track(
                EventStartName,
                new Dictionary<string, object>
                {
                    ["remain_sec"] = remainingSeconds,
                });
        }

        public override void TutorialCompleted(int difficulty)
        {
            _router.Track(
                TutorialCompleteName,
                new Dictionary<string, object>
                {
                    ["difficulty"] = difficulty,
                });
        }

        public override void StageStarted(
            int difficulty,
            int stage,
            int requiredProgress,
            int limitSeconds)
        {
            _router.Track(
                StageStartName,
                new Dictionary<string, object>
                {
                    ["difficulty"] = difficulty,
                    ["stage"] = stage,
                    ["required_progress"] = requiredProgress,
                    ["stage_limit_sec"] = limitSeconds,
                });
        }

        public override void StageEnded(
            int difficulty,
            int stage,
            LavaRushResult result,
            int progress,
            int requiredProgress,
            int durationSeconds)
        {
            _router.Track(
                StageEndName,
                new Dictionary<string, object>
                {
                    ["difficulty"] = difficulty,
                    ["stage"] = stage,
                    ["result"] = result == LavaRushResult.Win ? "win" : "lose",
                    ["stage_progress"] = progress,
                    ["required_progress"] = requiredProgress,
                    ["duration_sec"] = durationSeconds,
                });
        }

        public override void RewardClaimed(int difficulty, int stage, bool isFinal)
        {
            if (!_rewards.TryGet(difficulty, stage, out IReadOnlyList<CatAnalyticsReward> rewards)
                || rewards == null
                || rewards.Count == 0)
            {
                return;
            }

            var rewardInfo = new List<Dictionary<string, object>>(rewards.Count);
            for (int index = 0; index < rewards.Count; index++)
            {
                CatAnalyticsReward reward = rewards[index];
                rewardInfo.Add(new Dictionary<string, object>
                {
                    ["type"] = reward.Type,
                    ["item_id"] = reward.ItemId,
                    ["amount"] = reward.Amount,
                });
            }

            _router.Track(
                RewardClaimName,
                new Dictionary<string, object>
                {
                    ["difficulty"] = difficulty,
                    ["stage"] = stage,
                    ["is_final"] = isFinal,
                    ["reward_info"] = rewardInfo,
                },
                flattenRewardForMirror: true);
        }

        public override void EventEnded(int difficulty, int completedStages, bool completed)
        {
            // Intentionally no replay guard: repeated direct EndEvent calls remain observable.
            _router.Track(
                EventEndName,
                new Dictionary<string, object>
                {
                    ["difficulty"] = difficulty,
                    ["completed_stages"] = completedStages,
                    ["is_all_complete"] = completed,
                });
        }
    }
}
