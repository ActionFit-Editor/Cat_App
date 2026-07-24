using System;
using System.Collections.Generic;
using ActionFit.Content;
using ActionFit.LavaRush;
using ActionFit.Time;
using NUnit.Framework;

namespace ActionFit.Cat.App.Tests
{
    public sealed class CatCoreDurabilityTests
    {
        private static readonly DateTime TestUtc =
            new DateTime(2026, 7, 18, 3, 0, 0, DateTimeKind.Utc);

        [Test]
        public void CatLoop_ForwardsFramesAndCatchesUpUnscaledSeconds()
        {
            var loop = new CatLoop();
            var updates = new List<float>();
            int seconds = 0;
            loop.UpdateRequested += updates.Add;
            loop.EverySecondRequested += () => seconds++;

            loop.AdvanceFrame(0.25f, 2.25f);

            Assert.That(updates, Is.EqualTo(new[] { 0.25f }));
            Assert.That(seconds, Is.EqualTo(2));
        }

        [Test]
        public void CatLoop_PreservesGameGateSpeedResetAndClearContracts()
        {
            var loop = new CatLoop();
            var deltas = new List<float>();
            loop.GameUpdateRequested += deltas.Add;
            loop.SetGameSpeed(2.5f);

            loop.AdvanceGame(2f);
            loop.IsGameActive = false;
            loop.AdvanceGame(4f);
            loop.ResetGameEvent();
            loop.IsGameActive = true;
            loop.AdvanceGame(1f);

            Assert.That(deltas, Is.EqualTo(new[] { 5f }));
            Assert.That(loop.GameSpeed, Is.EqualTo(2.5f));
            loop.Clear();
            Assert.That(loop.GameSpeed, Is.EqualTo(2.5f));
            Assert.That(loop.IsGameActive, Is.True);
        }

        [Test]
        public void StateStore_UsesOneFixedProductPersistenceAndFlushesExplicitly()
        {
            var persistence = new FakeLavaRushPersistence();
            var store = new CatLavaRushStateStore(persistence);

            store.Save("ignored-content-id", "{\"schema\":1}");
            store.Flush();

            Assert.That(store.TryLoad("another-ignored-id", out string json), Is.True);
            Assert.That(json, Is.EqualTo("{\"schema\":1}"));
            Assert.That(persistence.Calls, Is.EqualTo(new[] { "save-runtime", "flush", "load-runtime" }));
        }

        [Test]
        public void PersistenceOwner_ImportsLegacyBeforeDurableMarker()
        {
            var persistence = new FakeLavaRushPersistence
            {
                Legacy = new CatLavaRushLegacySnapshot
                {
                    EventStarted = true,
                    EventEndTicks = TestUtc.AddHours(8).Ticks,
                    TimeSchemaVersion = 1,
                    TimeBasis = (int)LavaRushTimeBasis.UtcTicks,
                    SelectedDifficulty = 1,
                    Stage = 1,
                    WinRank = 1,
                },
            };
            CatLavaRushCatalogResolver catalog = CreateCatalog();
            LavaRushEngine engine = CreateEngine(persistence, catalog);
            var owner = new CatLavaRushPersistenceOwner(
                persistence,
                catalog.GetMaxStage,
                () => TestUtc.Ticks,
                _ => { });

            Assert.That(owner.ImportIfNeeded(engine), Is.True);
            Assert.That(persistence.MigrationStatus, Is.EqualTo(1));
            Assert.That(persistence.RuntimeJson, Is.Not.Empty);
            AssertOrdered(
                persistence.Calls,
                "save-runtime",
                "flush",
                "save-migration",
                "flush");
        }

        [Test]
        public void PersistenceOwner_CorruptRuntimeBacksUpThenConvergesFromLegacy()
        {
            var persistence = new FakeLavaRushPersistence
            {
                RuntimeJson = "{malformed",
                Legacy = new CatLavaRushLegacySnapshot
                {
                    TimeSchemaVersion = 0,
                    TimeBasis = -1,
                    Stage = 1,
                    WinRank = 1,
                },
            };
            CatLavaRushCatalogResolver catalog = CreateCatalog();
            LavaRushEngine engine = CreateEngine(persistence, catalog);
            var owner = new CatLavaRushPersistenceOwner(
                persistence,
                catalog.GetMaxStage,
                () => TestUtc.Ticks,
                _ => { });

            Assert.That(owner.ImportIfNeeded(engine), Is.True);
            Assert.That(persistence.CorruptBackup, Is.EqualTo("{malformed"));
            Assert.That(persistence.RuntimeJson, Is.Not.EqualTo("{malformed"));
            Assert.That(persistence.MigrationStatus, Is.EqualTo(1));
        }

        [Test]
        public void PersistenceOwner_ResetPreservesExternalRewardLedger()
        {
            var persistence = new FakeLavaRushPersistence
            {
                RuntimeJson = "{}",
                MigrationStatus = 1,
                CorruptBackup = "bad",
                ExternalRewardLedger = "confirmed",
            };
            CatLavaRushCatalogResolver catalog = CreateCatalog();
            var owner = new CatLavaRushPersistenceOwner(
                persistence,
                catalog.GetMaxStage,
                () => TestUtc.Ticks,
                _ => { });

            owner.Reset();

            Assert.That(persistence.RuntimeJson, Is.Null);
            Assert.That(persistence.MigrationStatus, Is.Zero);
            Assert.That(persistence.CorruptBackup, Is.Null);
            Assert.That(persistence.ExternalRewardLedger, Is.EqualTo("confirmed"));
        }

        [Test]
        public void RewardOwner_NormalizesAttachmentsAndPreventsDuplicateAcrossInstances()
        {
            var persistence = new FakeRewardPersistence();
            var granted = new List<CatContentReward>();
            var first = new CatContentRewardService(persistence, granted.Add);
            IReadOnlyList<ContentReward> rewards = new[]
            {
                new ContentReward("Gold", 2),
                new ContentReward("gold", 3),
                new ContentReward("BoardItem/3_4", 1),
            };

            Assert.That(first.GrantOnce("lava/reward/1", rewards), Is.True);
            var restored = new CatContentRewardService(persistence, granted.Add);
            Assert.That(restored.GrantOnce("lava/reward/1", rewards), Is.False);

            Assert.That(granted.Count, Is.EqualTo(2));
            Assert.That(granted[0].RewardId, Is.EqualTo("BoardItem/3_4"));
            Assert.That(granted[1].RewardId, Is.EqualTo("Gold"));
            Assert.That(granted[1].Amount, Is.EqualTo(5));
            Assert.That(persistence.Snapshot.Transactions["lava/reward/1"].Status, Is.EqualTo(2));
        }

        [Test]
        public void RewardOwner_RejectsMalformedLedgerWithoutGranting()
        {
            var persistence = new FakeRewardPersistence
            {
                Snapshot = new CatRewardLedger
                {
                    SchemaVersion = 999,
                },
            };
            int grants = 0;
            var service = new CatContentRewardService(persistence, _ => grants++);

            Assert.Throws<InvalidOperationException>(
                () => service.GrantOnce(
                    "lava/reward/2",
                    new[] { new ContentReward("Energy", 1) }));
            Assert.That(grants, Is.Zero);
        }

        private static LavaRushEngine CreateEngine(
            FakeLavaRushPersistence persistence,
            CatLavaRushCatalogResolver catalog)
        {
            return new LavaRushEngine(
                new CatLavaRushStateStore(persistence),
                new NullRewardService(),
                catalog,
                new FixedClock(TestUtc),
                TimeZoneInfo.Utc,
                new LegacyClock(TestUtc),
                new FixedRandom(),
                new FixedCurve(),
                "cat-merge/lava-rush",
                new AllowAccess(),
                new EnabledSchedule());
        }

        private static CatLavaRushCatalogResolver CreateCatalog()
        {
            return new CatLavaRushCatalogResolver(
                "cat-merge-lava-rush-v1",
                "balance-v1",
                new[]
                {
                    new CatLavaRushStageRow(
                        1,
                        1,
                        2,
                        10,
                        30,
                        60,
                        Array.Empty<ContentReward>()),
                });
        }

        private static void AssertOrdered(IReadOnlyList<string> calls, params string[] expected)
        {
            int cursor = -1;
            for (int expectedIndex = 0; expectedIndex < expected.Length; expectedIndex++)
            {
                int found = -1;
                for (int callIndex = cursor + 1; callIndex < calls.Count; callIndex++)
                {
                    if (calls[callIndex] == expected[expectedIndex])
                    {
                        found = callIndex;
                        break;
                    }
                }

                Assert.That(found, Is.GreaterThan(cursor), $"Missing ordered call: {expected[expectedIndex]}");
                cursor = found;
            }
        }

        private sealed class FakeLavaRushPersistence : ICatLavaRushPersistence
        {
            public readonly List<string> Calls = new List<string>();
            public string RuntimeJson;
            public int MigrationStatus;
            public string CorruptBackup;
            public CatLavaRushLegacySnapshot Legacy = new CatLavaRushLegacySnapshot();
            public string ExternalRewardLedger;

            public string LoadRuntimeState()
            {
                Calls.Add("load-runtime");
                return RuntimeJson;
            }

            public void SaveRuntimeState(string json)
            {
                Calls.Add("save-runtime");
                RuntimeJson = json;
            }

            public void DeleteRuntimeState()
            {
                Calls.Add("delete-runtime");
                RuntimeJson = null;
            }

            public int LoadMigrationStatus()
            {
                Calls.Add("load-migration");
                return MigrationStatus;
            }

            public void SaveMigrationStatus(int status)
            {
                Calls.Add("save-migration");
                MigrationStatus = status;
            }

            public void DeleteMigrationStatus()
            {
                Calls.Add("delete-migration");
                MigrationStatus = 0;
            }

            public string LoadCorruptRuntimeBackup()
            {
                Calls.Add("load-backup");
                return CorruptBackup;
            }

            public void SaveCorruptRuntimeBackup(string json)
            {
                Calls.Add("save-backup");
                CorruptBackup = json;
            }

            public void DeleteCorruptRuntimeBackup()
            {
                Calls.Add("delete-backup");
                CorruptBackup = null;
            }

            public CatLavaRushLegacySnapshot LoadLegacySnapshot(int maxStage)
            {
                Calls.Add($"load-legacy:{maxStage}");
                return Legacy;
            }

            public void DeleteLegacyState()
            {
                Calls.Add("delete-legacy");
            }

            public void Flush()
            {
                Calls.Add("flush");
            }
        }

        private sealed class FakeRewardPersistence : ICatContentRewardPersistence
        {
            public CatRewardLedger Snapshot;

            public CatRewardLedger Load()
            {
                return Clone(Snapshot);
            }

            public void SaveAndFlush(CatRewardLedger ledger)
            {
                Snapshot = Clone(ledger);
            }

            private static CatRewardLedger Clone(CatRewardLedger source)
            {
                if (source == null)
                {
                    return null;
                }

                var copy = new CatRewardLedger
                {
                    SchemaVersion = source.SchemaVersion,
                    Transactions = source.Transactions == null
                        ? null
                        : new Dictionary<string, CatRewardTransaction>(StringComparer.Ordinal),
                };
                if (source.Transactions == null)
                {
                    return copy;
                }

                foreach (KeyValuePair<string, CatRewardTransaction> pair in source.Transactions)
                {
                    var transaction = new CatRewardTransaction
                    {
                        Status = pair.Value.Status,
                    };
                    for (int index = 0; index < pair.Value.Rewards.Count; index++)
                    {
                        CatRewardReceipt reward = pair.Value.Rewards[index];
                        transaction.Rewards.Add(new CatRewardReceipt
                        {
                            RewardId = reward.RewardId,
                            Amount = reward.Amount,
                            Granted = reward.Granted,
                        });
                    }

                    copy.Transactions.Add(pair.Key, transaction);
                }

                return copy;
            }
        }

        private sealed class NullRewardService : IContentRewardService
        {
            public bool IsAvailable => true;
            public bool HasGranted(string transactionId) => false;
            public bool GrantOnce(string transactionId, IReadOnlyList<ContentReward> rewards) => true;
        }

        private sealed class LegacyClock : ILavaRushLegacyLocalClock
        {
            public LegacyClock(DateTime now)
            {
                Now = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);
            }

            public DateTime Now { get; }
        }

        private sealed class FixedRandom : ILavaRushRandom
        {
            public int Range(int minInclusive, int maxExclusive) => minInclusive;
        }

        private sealed class FixedCurve : ILavaRushSeatCurveProvider
        {
            public int CurveCount => 1;
            public float Evaluate(int curveIndex, float normalizedTime) => normalizedTime;
        }

        private sealed class AllowAccess : ILavaRushAccessPolicy
        {
            public bool IsAccessAllowed => true;
        }

        private sealed class EnabledSchedule : ILavaRushSchedulePolicy
        {
            public bool IsEnabled => true;
            public bool IsActiveDay(DayOfWeek dayOfWeek) => true;
        }
    }
}
