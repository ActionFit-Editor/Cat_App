using System;
using System.Collections.Generic;
using ActionFit.LavaRush;
using ActionFit.LavaRush.UI;
using NUnit.Framework;

namespace ActionFit.Cat.App.Tests
{
    public sealed class CatLavaRushProductServicesTests
    {
        [Test]
        public void Profiles_NamePresenceKeepsStablePartialRecordAndDirection()
        {
            var store = new FakeStore
            {
                Name = "Saved Bot",
                ProfileId = null,
                FrameId = null,
            };
            var catalog = FakeCatalog.Create();
            catalog.Direction = -1;
            var roster = new CatLavaRushProfileRoster(
                new FakePlayer(),
                new CatBotProfileService(store, catalog, new QueueRandom()));

            LavaRushProfileSnapshot first = roster.LoadOrGenerateOpponent(4, 2);
            LavaRushProfileSnapshot second = roster.LoadOrGenerateOpponent(4, 2);

            Assert.That(first.DisplayName, Is.EqualTo("Saved Bot"));
            Assert.That(first.ProfileId, Is.EqualTo("0"));
            Assert.That(first.FrameId, Is.EqualTo("frame_blue"));
            Assert.That(first.HorizontalDirection, Is.EqualTo(-1));
            Assert.That(second.DisplayName, Is.EqualTo(first.DisplayName));
            Assert.That(store.SaveCount, Is.Zero);
            Assert.That(store.LastKey, Is.EqualTo("lava_rush_enemy_4_2"));
        }

        [Test]
        public void Profiles_GenerationPreservesFivePoolAndCandidateDrawOrder()
        {
            var store = new FakeStore();
            var catalog = FakeCatalog.Create();
            catalog.Names[CatBotNameLanguage.Korean] = new[] { "KR" };
            catalog.Names[CatBotNameLanguage.English] = new[] { "EN" };
            catalog.Characters.Add(new CatProfileCandidate("character", 0, null, null, true, 1, true));
            catalog.Profiles.Add(new CatProfileCandidate("profile", 0, null, null, true, -1, false));
            catalog.Frames.Add(new CatProfileCandidate("frame", 0, null, null, true, 0, false));
            var random = new QueueRandom(1, 1, 0);
            var service = new CatBotProfileService(store, catalog, random);

            CatBotProfileRecord result = service.Generate("key");

            Assert.That(result.Name, Is.EqualTo("EN"));
            Assert.That(result.ProfileId, Is.EqualTo("profile"));
            Assert.That(result.FrameId, Is.EqualTo("frame"));
            Assert.That(random.Ranges, Is.EqualTo(new[] { "0:2", "0:2", "0:1" }));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void Profiles_ExpiredRowsRemainAvailableOnlyWhenOwned()
        {
            DateTime end = new(2026, 7, 1);
            DateTime afterClose = new(2026, 7, 1, 23, 59, 59);

            Assert.That(
                new CatProfileCandidate("x", 1, null, end, false, 0, false)
                    .IsAvailable(afterClose),
                Is.False);
            Assert.That(
                new CatProfileCandidate("x", 1, null, end, true, 0, false)
                    .IsAvailable(afterClose),
                Is.True);
        }

        [Test]
        public void SoundService_GatesPlaybackAndOwnsConfigurationLifetime()
        {
            var backend = new FakeSoundBackend();
            var options = new FakeSoundOptions { Enabled = true };
            var events = new FakeAudioEvents();
            var service = new CatSoundService(backend, options, events);
            var playback = new CatSoundPlayback("clip", CatSoundChannel.Overlay, "SFX", 0.5f, false, 1f, 1f);

            service.Play(playback);
            service.Initialize();
            service.Initialize();
            service.Play(playback);
            options.Enabled = false;
            service.Play(playback);
            events.Raise(false);
            events.Raise(true);
            service.Dispose();
            events.Raise(true);

            Assert.That(backend.PlayCount, Is.EqualTo(1));
            Assert.That(backend.RecoverCount, Is.EqualTo(1));
            Assert.That(events.SubscribeCount, Is.EqualTo(1));
            Assert.That(events.UnsubscribeCount, Is.EqualTo(1));
        }

        [Test]
        public void Audio_MapsTenCuesAndPreservesSingleAndPitchedSemantics()
        {
            var backend = new FakeSoundBackend();
            var service = new CatSoundService(
                backend,
                new FakeSoundOptions { Enabled = true },
                new FakeAudioEvents());
            service.Initialize();
            var audio = new CatLavaRushAudio(service, CatLavaRushAudio.CreateLegacyClipIds());

            foreach (LavaRushAudioCue cue in Enum.GetValues(typeof(LavaRushAudioCue)))
                audio.Play(cue);
            audio.PlayPitched(
                LavaRushAudioCue.ProfileAppear,
                CatLavaRushAudio.DefaultVolume,
                CatLavaRushAudio.ProfilePitchMin,
                CatLavaRushAudio.ProfilePitchMax);

            Assert.That(backend.Playbacks.Count, Is.EqualTo(11));
            Assert.That(
                backend.Playbacks[(int)LavaRushAudioCue.TutorialStep].Channel,
                Is.EqualTo(CatSoundChannel.Single));
            Assert.That(backend.Playbacks[10].Pitched, Is.True);
            Assert.That(backend.Playbacks[10].PitchMin, Is.EqualTo(0.85f));
            Assert.That(backend.Playbacks[10].PitchMax, Is.EqualTo(1.2f));
        }

        [Test]
        public void Localization_MapsExactOperationalRowsFallbackAndRefresh()
        {
            var environment = new FakeLocalization();
            var localizer = new CatLavaRushUILocalizer(environment);
            int refreshCount = 0;
            localizer.LocaleChanged += () => refreshCount++;
            localizer.Initialize();
            localizer.Initialize();

            Assert.That(CatLavaRushUILocalizer.Mappings.Count, Is.EqualTo(18));
            Assert.That(
                FindLocalization("lavarush_lose_desc2").SmartFormat,
                Is.True);
            Assert.That(
                FindLocalization("lavarush_title").SharedDataId,
                Is.EqualTo(42680591513018368L));
            Assert.That(localizer.Get(LavaRushLocalizationKeys.Title, "fallback"), Is.EqualTo("fallback"));

            environment.Values["lavarush_title"] = "Localized";
            Assert.That(localizer.Get(LavaRushLocalizationKeys.Title, "fallback"), Is.EqualTo("Localized"));
            environment.Raise();
            localizer.Dispose();
            environment.Raise();
            Assert.That(refreshCount, Is.EqualTo(1));
        }

        [Test]
        public void AnalyticsRouter_DropsBeforeReadyOrdersDestinationsAndFlattensReward()
        {
            var calls = new List<string>();
            var primary = new FakePrimary(calls);
            var mirror = new FakeMirror(calls);
            var router = new CatAnalyticsRouter(primary, mirror);
            var properties = RewardProperties();

            Assert.That(router.Track("event", properties, true), Is.False);
            primary.Ready = true;
            Assert.That(router.Track("event", properties, true), Is.True);

            Assert.That(calls, Is.EqualTo(new[] { "primary:event", "mirror:event" }));
            Assert.That(mirror.Last.ContainsKey("reward_info"), Is.False);
            Assert.That(mirror.Last["reward_gold"], Is.EqualTo(15));
            Assert.That(mirror.Last["reward_item_id"], Is.EqualTo("item_a"));
            Assert.That(properties.ContainsKey("reward_info"), Is.True);
        }

        [Test]
        public void AnalyticsSink_UsesExactSchemasDropsMissingRewardAndDoesNotGuardEventEndReplay()
        {
            var calls = new List<string>();
            var primary = new FakePrimary(calls) { Ready = true };
            var mirror = new FakeMirror(calls);
            var rewards = new FakeRewards();
            var sink = new CatLavaRushAnalyticsSink(
                new CatAnalyticsRouter(primary, mirror),
                rewards);

            sink.StageEnded(2, 3, LavaRushResult.Lose, 4, 5, 6);
            sink.RewardClaimed(2, 99, false);
            rewards.Rows = new[]
            {
                new CatAnalyticsReward(CatAnalyticsRewardKind.Gold, "", 7),
            };
            sink.RewardClaimed(2, 3, true);
            sink.EventEnded(2, 3, false);
            sink.EventEnded(2, 3, false);

            Assert.That(primary.EventNames, Is.EqualTo(new[]
            {
                "te_lavarush_stage_end",
                "te_lavarush_reward_claim",
                "te_lavarush_event_end",
                "te_lavarush_event_end",
            }));
            Assert.That(primary.Properties[0]["result"], Is.EqualTo("lose"));
            Assert.That(primary.Properties[1]["is_final"], Is.True);
        }

        private static CatLavaRushLocalizationEntry FindLocalization(string entry)
        {
            foreach (CatLavaRushLocalizationEntry mapping in CatLavaRushUILocalizer.Mappings)
            {
                if (mapping.TableEntry == entry)
                    return mapping;
            }

            throw new AssertionException($"Missing localization mapping: {entry}");
        }

        private static Dictionary<string, object> RewardProperties()
        {
            return new Dictionary<string, object>
            {
                ["reward_info"] = new List<Dictionary<string, object>>
                {
                    new() { ["type"] = "gold", ["item_id"] = "", ["amount"] = 10 },
                    new() { ["type"] = "gold", ["item_id"] = "", ["amount"] = 5 },
                    new() { ["type"] = "item", ["item_id"] = "item_a", ["amount"] = 1 },
                },
            };
        }

        private sealed class FakeStore : CatBotProfileStoreBase
        {
            public string Name;
            public string ProfileId;
            public string FrameId;
            public int SaveCount;
            public string LastKey;

            public override string LoadName(string key)
            {
                LastKey = key;
                return Name;
            }

            public override string LoadProfileId(string key, string defaultValue) =>
                ProfileId ?? defaultValue;

            public override string LoadFrameId(string key, string defaultValue) =>
                FrameId ?? defaultValue;

            public override void Save(string key, CatBotProfileRecord record)
            {
                LastKey = key;
                Name = record.Name;
                ProfileId = record.ProfileId;
                FrameId = record.FrameId;
                SaveCount++;
            }

            public override void Delete(string key)
            {
                LastKey = key;
                Name = null;
                ProfileId = null;
                FrameId = null;
            }
        }

        private sealed class FakeCatalog : CatLavaRushProfileCatalogBase
        {
            public readonly Dictionary<CatBotNameLanguage, IReadOnlyList<string>> Names = new();
            public readonly List<CatProfileCandidate> Characters = new();
            public readonly List<CatProfileCandidate> Profiles = new();
            public readonly List<CatProfileCandidate> Frames = new();
            public int Direction;

            public override DateTime LocalNow => new(2026, 7, 24);
            public override IReadOnlyList<CatProfileCandidate> CharacterProfiles => Characters;
            public override IReadOnlyList<CatProfileCandidate> AuthoredProfiles => Profiles;
            public override IReadOnlyList<CatProfileCandidate> AuthoredFrames => Frames;

            public static FakeCatalog Create()
            {
                var result = new FakeCatalog();
                foreach (CatBotNameLanguage language in Enum.GetValues(typeof(CatBotNameLanguage)))
                    result.Names[language] = Array.Empty<string>();
                return result;
            }

            public override IReadOnlyList<string> GetBotNames(CatBotNameLanguage language) => Names[language];
            public override int GetHorizontalDirection(string profileId) => Direction;
        }

        private sealed class QueueRandom : CatRandomSourceBase
        {
            private readonly Queue<int> _values;
            public readonly List<string> Ranges = new();

            public QueueRandom(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public override int Range(int minInclusive, int maxExclusive)
            {
                Ranges.Add($"{minInclusive}:{maxExclusive}");
                return _values.Count == 0 ? minInclusive : _values.Dequeue();
            }
        }

        private sealed class FakePlayer : CatPlayerProfileSourceBase
        {
            public override CatPlayerProfileRecord ReadPlayer() =>
                new("Player", "0", "frame_blue", 1);
        }

        private sealed class FakeSoundBackend : CatSoundPlaybackBackendBase
        {
            public bool Available = true;
            public override bool IsAvailable => Available;
            public int PlayCount;
            public int RecoverCount;
            public readonly List<CatSoundPlayback> Playbacks = new();

            public override void Play(CatSoundPlayback playback)
            {
                PlayCount++;
                Playbacks.Add(playback);
            }

            public override void RecoverAudioDevice() => RecoverCount++;
        }

        private sealed class FakeSoundOptions : CatSoundOptionsBase
        {
            public bool Enabled;
            public override bool SoundEffectsEnabled => Enabled;
        }

        private sealed class FakeAudioEvents : CatAudioConfigurationEventsBase
        {
            private Action<bool> _configurationChanged;
            public int SubscribeCount;
            public int UnsubscribeCount;

            public override event Action<bool> ConfigurationChanged
            {
                add
                {
                    SubscribeCount++;
                    _configurationChanged += value;
                }
                remove
                {
                    UnsubscribeCount++;
                    _configurationChanged -= value;
                }
            }

            public void Raise(bool deviceChanged) => _configurationChanged?.Invoke(deviceChanged);
        }

        private sealed class FakeLocalization : CatLocalizationEnvironmentBase
        {
            public readonly Dictionary<string, string> Values = new();
            public override event Action LocaleChanged;

            public override string GetLocalizedString(string table, string entry)
            {
                Assert.That(table, Is.EqualTo("General"));
                return Values.TryGetValue(entry, out string value) ? value : "";
            }

            public void Raise() => LocaleChanged?.Invoke();
        }

        private sealed class FakePrimary : CatAnalyticsPrimaryDestinationBase
        {
            private readonly IList<string> _calls;
            public bool Ready;
            public readonly List<string> EventNames = new();
            public readonly List<IReadOnlyDictionary<string, object>> Properties = new();

            public FakePrimary(IList<string> calls)
            {
                _calls = calls;
            }

            public override bool IsReady => Ready;

            public override void Track(string eventName, IReadOnlyDictionary<string, object> properties)
            {
                _calls.Add($"primary:{eventName}");
                EventNames.Add(eventName);
                Properties.Add(properties);
            }
        }

        private sealed class FakeMirror : CatAnalyticsMirrorDestinationBase
        {
            private readonly IList<string> _calls;
            public IReadOnlyDictionary<string, object> Last;

            public FakeMirror(IList<string> calls)
            {
                _calls = calls;
            }

            public override void Track(string eventName, IReadOnlyDictionary<string, object> properties)
            {
                _calls.Add($"mirror:{eventName}");
                Last = properties;
            }
        }

        private sealed class FakeRewards : CatLavaRushRewardAnalyticsCatalogBase
        {
            public IReadOnlyList<CatAnalyticsReward> Rows = Array.Empty<CatAnalyticsReward>();

            public override bool TryGet(
                int difficulty,
                int stage,
                out IReadOnlyList<CatAnalyticsReward> rewards)
            {
                rewards = Rows;
                return Rows.Count > 0;
            }
        }
    }
}
