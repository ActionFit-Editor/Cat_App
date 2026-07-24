using System;
using System.Collections.Generic;
using ActionFit.Content;
using ActionFit.LavaRush;

namespace ActionFit.Cat.App
{
    public interface ICatLavaRushPersistence
    {
        string LoadRuntimeState();
        void SaveRuntimeState(string json);
        void DeleteRuntimeState();
        int LoadMigrationStatus();
        void SaveMigrationStatus(int status);
        void DeleteMigrationStatus();
        string LoadCorruptRuntimeBackup();
        void SaveCorruptRuntimeBackup(string json);
        void DeleteCorruptRuntimeBackup();
        CatLavaRushLegacySnapshot LoadLegacySnapshot(int maxStage);
        void DeleteLegacyState();
        void Flush();
    }

    public sealed class CatLavaRushLegacySnapshot
    {
        public bool EventStarted { get; set; }
        public bool PendingEnd { get; set; }
        public long EventEndTicks { get; set; }
        public int TimeSchemaVersion { get; set; }
        public int TimeBasis { get; set; }
        public int SelectedDifficulty { get; set; }
        public bool TutorialDone { get; set; }
        public int Stage { get; set; }
        public int StageProgress { get; set; }
        public bool AllStagesComplete { get; set; }
        public string PendingResult { get; set; }
        public long StageStartTicks { get; set; }
        public int StageLimitSeconds { get; set; }
        public int SeatCurveIndex { get; set; }
        public int WinRank { get; set; }
        public int ResultSeatCount { get; set; }
        public int ResultSeatCapacity { get; set; }
        public IReadOnlyList<int> ClaimedStageRewards { get; set; } = Array.Empty<int>();
        public bool FinalRewardClaimed { get; set; }
    }

    public sealed class CatLavaRushStateStore : IContentStateStore, IFlushableContentStateStore
    {
        private readonly ICatLavaRushPersistence _persistence;

        public CatLavaRushStateStore(ICatLavaRushPersistence persistence)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        }

        public bool TryLoad(string contentId, out string json)
        {
            json = _persistence.LoadRuntimeState();
            return !string.IsNullOrWhiteSpace(json);
        }

        public void Save(string contentId, string json)
        {
            _persistence.SaveRuntimeState(json ?? throw new ArgumentNullException(nameof(json)));
        }

        public void Delete(string contentId)
        {
            _persistence.DeleteRuntimeState();
        }

        public void Flush()
        {
            _persistence.Flush();
        }
    }

    public sealed class CatLavaRushPersistenceOwner
    {
        public const int CompleteMigrationStatus = 1;

        private readonly ICatLavaRushPersistence _persistence;
        private readonly Func<int, int> _getMaxStage;
        private readonly Func<long> _getLegacyNowTicks;
        private readonly Action<string> _report;
        private readonly Action<string> _reportError;

        public CatLavaRushPersistenceOwner(
            ICatLavaRushPersistence persistence,
            Func<int, int> getMaxStage,
            Func<long> getLegacyNowTicks,
            Action<string> report,
            Action<string> reportError = null)
        {
            _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            _getMaxStage = getMaxStage ?? throw new ArgumentNullException(nameof(getMaxStage));
            _getLegacyNowTicks = getLegacyNowTicks ?? throw new ArgumentNullException(nameof(getLegacyNowTicks));
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _reportError = reportError ?? _report;
        }

        public bool ImportIfNeeded(LavaRushEngine engine)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));
            if (_persistence.LoadMigrationStatus() == CompleteMigrationStatus)
            {
                return false;
            }

            string runtimeJson = _persistence.LoadRuntimeState();
            if (!string.IsNullOrWhiteSpace(runtimeJson))
            {
                try
                {
                    engine.Restore();
                    MarkComplete();
                    return false;
                }
                catch (FormatException)
                {
                    if (string.IsNullOrWhiteSpace(_persistence.LoadCorruptRuntimeBackup()))
                    {
                        _persistence.SaveCorruptRuntimeBackup(runtimeJson);
                    }

                    _persistence.DeleteRuntimeState();
                    _persistence.Flush();
                    _reportError("Corrupt package runtime was backed up; restoring preserved legacy state.");
                }
            }

            CatLavaRushLegacySnapshot initial = _persistence.LoadLegacySnapshot(0);
            if (!AreStoredTimestampsValid(initial.EventEndTicks, initial.StageStartTicks))
            {
                throw new FormatException("Legacy LavaRush timer ticks are outside the DateTime range.");
            }

            bool hasActiveLegacyTimer = HasActiveLegacyTimer(
                initial.EventStarted,
                initial.PendingEnd,
                initial.EventEndTicks,
                initial.StageStartTicks,
                _getLegacyNowTicks());
            if (!TryResolveBasis(
                    initial.TimeSchemaVersion,
                    initial.TimeBasis,
                    hasActiveLegacyTimer,
                    out LavaRushTimeBasis timeBasis))
            {
                throw new FormatException("Legacy LavaRush timer metadata is unsupported.");
            }

            int maxStage = _getMaxStage(initial.SelectedDifficulty);
            CatLavaRushLegacySnapshot legacy = _persistence.LoadLegacySnapshot(maxStage);
            LavaRushImportState importState = CreateImportState(legacy, timeBasis);
            if (!engine.ImportStateIfEmpty(importState))
            {
                throw new InvalidOperationException("LavaRush package state appeared during legacy migration.");
            }

            MarkComplete();
            _report("Imported legacy per-field state into package runtime V1.");
            return true;
        }

        public void Reset()
        {
            _persistence.DeleteLegacyState();
            _persistence.DeleteRuntimeState();
            _persistence.DeleteMigrationStatus();
            _persistence.DeleteCorruptRuntimeBackup();
            _persistence.Flush();
        }

        private void MarkComplete()
        {
            _persistence.SaveMigrationStatus(CompleteMigrationStatus);
            _persistence.Flush();
        }

        private static LavaRushImportState CreateImportState(
            CatLavaRushLegacySnapshot legacy,
            LavaRushTimeBasis timeBasis)
        {
            string result = legacy.PendingResult;
            return new LavaRushImportState
            {
                EventStarted = legacy.EventStarted && legacy.EventEndTicks > 0,
                PendingEnd = legacy.PendingEnd,
                EventEndTicks = Math.Max(0L, legacy.EventEndTicks),
                TimeSchemaVersion = 1,
                TimeBasis = timeBasis,
                SelectedDifficulty = legacy.SelectedDifficulty,
                TutorialDone = legacy.TutorialDone,
                Stage = Math.Max(LavaRushEngine.MinStage, legacy.Stage),
                StageProgress = Math.Max(0, legacy.StageProgress),
                AllStagesComplete = legacy.AllStagesComplete,
                PendingResult = string.Equals(result, "win", StringComparison.Ordinal)
                    ? LavaRushResult.Win
                    : string.Equals(result, "lose", StringComparison.Ordinal)
                        ? LavaRushResult.Lose
                        : LavaRushResult.None,
                StageStartTicks = Math.Max(0L, legacy.StageStartTicks),
                StageLimitSeconds = Math.Max(0, legacy.StageLimitSeconds),
                SeatCurveIndex = Math.Max(0, legacy.SeatCurveIndex),
                WinRank = Math.Max(1, legacy.WinRank),
                ResultSeatCount = Math.Max(0, legacy.ResultSeatCount),
                ResultSeatCapacity = Math.Max(0, legacy.ResultSeatCapacity),
                ClaimedStageRewards = legacy.ClaimedStageRewards ?? Array.Empty<int>(),
                FinalRewardClaimed = legacy.FinalRewardClaimed,
            };
        }

        private static bool TryResolveBasis(
            int schemaVersion,
            int storedTimeBasis,
            bool hasActiveLegacyTimer,
            out LavaRushTimeBasis basis)
        {
            if (schemaVersion == 0 && storedTimeBasis == -1)
            {
                basis = hasActiveLegacyTimer
                    ? LavaRushTimeBasis.LegacyLocalTicks
                    : LavaRushTimeBasis.UtcTicks;
                return true;
            }

            if (schemaVersion != 1 || !Enum.IsDefined(typeof(LavaRushTimeBasis), storedTimeBasis))
            {
                basis = default;
                return false;
            }

            basis = (LavaRushTimeBasis)storedTimeBasis;
            if (basis == LavaRushTimeBasis.LegacyLocalTicks && !hasActiveLegacyTimer)
            {
                basis = LavaRushTimeBasis.UtcTicks;
            }

            return true;
        }

        private static bool HasActiveLegacyTimer(
            bool eventStarted,
            bool pendingEnd,
            long eventEndTicks,
            long stageStartTicks,
            long legacyNowTicks)
        {
            return eventStarted
                || pendingEnd
                || IsStoredTimestamp(stageStartTicks)
                || (IsStoredTimestamp(eventEndTicks) && eventEndTicks > legacyNowTicks);
        }

        private static bool AreStoredTimestampsValid(long eventEndTicks, long stageStartTicks)
        {
            return IsStoredTimestampOrEmpty(eventEndTicks)
                && IsStoredTimestampOrEmpty(stageStartTicks);
        }

        private static bool IsStoredTimestampOrEmpty(long ticks)
        {
            return ticks == 0L || IsStoredTimestamp(ticks);
        }

        private static bool IsStoredTimestamp(long ticks)
        {
            return ticks > 0L && ticks <= DateTime.MaxValue.Ticks;
        }
    }

    public sealed class CatLavaRushStageRow
    {
        public CatLavaRushStageRow(
            int difficulty,
            int stage,
            int capacity,
            int requiredProgress,
            int minLimitSeconds,
            int maxLimitSeconds,
            IReadOnlyList<ContentReward> rewards)
        {
            Difficulty = difficulty;
            Stage = stage;
            Capacity = capacity;
            RequiredProgress = requiredProgress;
            MinLimitSeconds = minLimitSeconds;
            MaxLimitSeconds = maxLimitSeconds;
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        }

        public int Difficulty { get; }
        public int Stage { get; }
        public int Capacity { get; }
        public int RequiredProgress { get; }
        public int MinLimitSeconds { get; }
        public int MaxLimitSeconds { get; }
        public IReadOnlyList<ContentReward> Rewards { get; }
    }

    public sealed class CatLavaRushCatalogResolver : ILavaRushCatalogResolver
    {
        private readonly Dictionary<int, int> _maxStages;
        private readonly LavaRushCatalog _catalog;

        public CatLavaRushCatalogResolver(
            string catalogVersion,
            string balanceRevision,
            IEnumerable<CatLavaRushStageRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            var stages = new Dictionary<int, List<LavaRushStageDefinition>>();
            foreach (CatLavaRushStageRow row in rows)
            {
                if (row == null) throw new ArgumentException("Rows must not contain null.", nameof(rows));
                if (!stages.TryGetValue(row.Difficulty, out List<LavaRushStageDefinition> difficultyStages))
                {
                    difficultyStages = new List<LavaRushStageDefinition>();
                    stages.Add(row.Difficulty, difficultyStages);
                }

                difficultyStages.Add(new LavaRushStageDefinition(
                    row.Stage,
                    row.Capacity,
                    row.RequiredProgress,
                    row.MinLimitSeconds,
                    row.MaxLimitSeconds,
                    row.Rewards));
            }

            var difficulties = new List<LavaRushDifficultyDefinition>(stages.Count);
            _maxStages = new Dictionary<int, int>(stages.Count);
            foreach (KeyValuePair<int, List<LavaRushStageDefinition>> pair in stages)
            {
                difficulties.Add(new LavaRushDifficultyDefinition(pair.Key, pair.Value));
                int maxStage = 0;
                for (int index = 0; index < pair.Value.Count; index++)
                {
                    maxStage = Math.Max(maxStage, pair.Value[index].Stage);
                }

                _maxStages.Add(pair.Key, maxStage);
            }

            _catalog = new LavaRushCatalog(catalogVersion, balanceRevision, difficulties);
        }

        public LavaRushCatalog Current => _catalog;

        public bool TryResolve(string catalogVersion, string balanceRevision, out LavaRushCatalog catalog)
        {
            catalog = string.Equals(_catalog.CatalogVersion, catalogVersion, StringComparison.Ordinal)
                && string.Equals(_catalog.BalanceRevision, balanceRevision, StringComparison.Ordinal)
                ? _catalog
                : null;
            return catalog != null;
        }

        public int GetMaxStage(int selectedDifficulty)
        {
            if (_maxStages.TryGetValue(selectedDifficulty, out int maxStage))
            {
                return maxStage;
            }

            maxStage = 0;
            foreach (int difficultyMaxStage in _maxStages.Values)
            {
                maxStage = Math.Max(maxStage, difficultyMaxStage);
            }

            return maxStage;
        }
    }
}
