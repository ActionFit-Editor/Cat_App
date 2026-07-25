using System;
using System.Collections.Generic;
using ActionFit.LavaRush.UI;

namespace ActionFit.Cat.App
{
    public enum CatBotNameLanguage
    {
        Korean,
        English,
        Japanese,
        SimplifiedChinese,
        TraditionalChinese,
    }

    public readonly struct CatBotProfileRecord
    {
        public CatBotProfileRecord(string name, string profileId, string frameId)
        {
            Name = name ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            FrameId = frameId ?? string.Empty;
        }

        public string Name { get; }
        public string ProfileId { get; }
        public string FrameId { get; }
    }

    public readonly struct CatProfileCandidate
    {
        public CatProfileCandidate(
            string id,
            int type,
            DateTime? startDate,
            DateTime? endDate,
            bool owned,
            int horizontalDirection,
            bool characterEligible)
        {
            Id = id ?? string.Empty;
            Type = type;
            StartDate = startDate;
            EndDate = endDate;
            Owned = owned;
            HorizontalDirection = horizontalDirection;
            CharacterEligible = characterEligible;
        }

        public string Id { get; }
        public int Type { get; }
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }
        public bool Owned { get; }
        public int HorizontalDirection { get; }
        public bool CharacterEligible { get; }

        public bool IsAvailable(DateTime localNow)
        {
            if (StartDate.HasValue && localNow < StartDate.Value)
                return false;

            if (!EndDate.HasValue)
                return true;

            DateTime end = EndDate.Value;
            var close = new DateTime(end.Year, end.Month, end.Day, 23, 59, 59);
            return localNow < close || Owned;
        }
    }

    public readonly struct CatPlayerProfileRecord
    {
        public CatPlayerProfileRecord(
            string displayName,
            string profileId,
            string frameId,
            int horizontalDirection)
        {
            DisplayName = displayName ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            FrameId = frameId ?? string.Empty;
            HorizontalDirection = horizontalDirection;
        }

        public string DisplayName { get; }
        public string ProfileId { get; }
        public string FrameId { get; }
        public int HorizontalDirection { get; }
    }

    /// <summary>Supplies current player identity without moving profile persistence into the product package.</summary>
    public abstract class CatPlayerProfileSourceBase
    {
        public abstract CatPlayerProfileRecord ReadPlayer();
    }

    /// <summary>Supplies primitive bot records through the current Project Shell persistence owner.</summary>
    public abstract class CatBotProfileStoreBase
    {
        public abstract string LoadName(string key);
        public abstract string LoadProfileId(string key, string defaultValue);
        public abstract string LoadFrameId(string key, string defaultValue);
        public abstract void Save(string key, CatBotProfileRecord record);
        public abstract void Delete(string key);
    }

    /// <summary>Supplies ordered Cat-authored profile, frame, and five-language bot-name inputs.</summary>
    public abstract class CatLavaRushProfileCatalogBase
    {
        public abstract DateTime LocalNow { get; }
        public abstract IReadOnlyList<string> GetBotNames(CatBotNameLanguage language);
        public abstract IReadOnlyList<CatProfileCandidate> CharacterProfiles { get; }
        public abstract IReadOnlyList<CatProfileCandidate> AuthoredProfiles { get; }
        public abstract IReadOnlyList<CatProfileCandidate> AuthoredFrames { get; }
        public abstract int GetHorizontalDirection(string profileId);
    }

    public abstract class CatRandomSourceBase
    {
        public abstract int Range(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// Preserves Cat bot record presence, candidate order, fallback, and random draw order while
    /// leaving the concrete store and generated catalogs in the Project Shell.
    /// </summary>
    public sealed class CatBotProfileService
    {
        public const string DefaultName = "Bot";
        public const string DefaultProfileId = LavaRushProfileSnapshot.DefaultProfileId;
        public const string DefaultFrameId = LavaRushProfileSnapshot.DefaultFrameId;

        private static readonly CatBotNameLanguage[] NamePoolOrder =
        {
            CatBotNameLanguage.Korean,
            CatBotNameLanguage.English,
            CatBotNameLanguage.Japanese,
            CatBotNameLanguage.SimplifiedChinese,
            CatBotNameLanguage.TraditionalChinese,
        };

        private readonly CatBotProfileStoreBase _store;
        private readonly CatLavaRushProfileCatalogBase _catalog;
        private readonly CatRandomSourceBase _random;

        public CatBotProfileService(
            CatBotProfileStoreBase store,
            CatLavaRushProfileCatalogBase catalog,
            CatRandomSourceBase random)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public CatBotProfileRecord LoadOrGenerate(string key)
        {
            RequireKey(key);
            string name = _store.LoadName(key);
            if (string.IsNullOrEmpty(name))
                return Generate(key);

            return new CatBotProfileRecord(
                name,
                _store.LoadProfileId(key, DefaultProfileId),
                _store.LoadFrameId(key, DefaultFrameId));
        }

        public CatBotProfileRecord Generate(string key)
        {
            RequireKey(key);
            var record = new CatBotProfileRecord(
                SelectName(),
                SelectProfileId(),
                SelectFrameId());
            _store.Save(key, record);
            return record;
        }

        public void Delete(string key)
        {
            RequireKey(key);
            _store.Delete(key);
        }

        public int GetHorizontalDirection(string profileId)
        {
            return _catalog.GetHorizontalDirection(profileId);
        }

        private string SelectName()
        {
            int total = 0;
            for (int index = 0; index < NamePoolOrder.Length; index++)
                total += _catalog.GetBotNames(NamePoolOrder[index])?.Count ?? 0;

            if (total == 0)
                return DefaultName;

            int selected = _random.Range(0, total);
            for (int index = 0; index < NamePoolOrder.Length; index++)
            {
                IReadOnlyList<string> names = _catalog.GetBotNames(NamePoolOrder[index]);
                int count = names?.Count ?? 0;
                if (selected < count)
                    return names[selected];
                selected -= count;
            }

            throw new InvalidOperationException("Bot name selection exceeded the available pool.");
        }

        private string SelectProfileId()
        {
            var candidates = new List<string>();
            IReadOnlyList<CatProfileCandidate> characters = _catalog.CharacterProfiles;
            for (int index = 0; index < (characters?.Count ?? 0); index++)
            {
                CatProfileCandidate candidate = characters[index];
                if (candidate.CharacterEligible)
                    candidates.Add(candidate.Id);
            }

            IReadOnlyList<CatProfileCandidate> profiles = _catalog.AuthoredProfiles;
            for (int index = 0; index < (profiles?.Count ?? 0); index++)
            {
                CatProfileCandidate candidate = profiles[index];
                if (candidate.Type == 0 || candidate.IsAvailable(_catalog.LocalNow))
                    candidates.Add(candidate.Id);
            }

            return Select(candidates, DefaultProfileId);
        }

        private string SelectFrameId()
        {
            var candidates = new List<string>();
            IReadOnlyList<CatProfileCandidate> frames = _catalog.AuthoredFrames;
            for (int index = 0; index < (frames?.Count ?? 0); index++)
            {
                CatProfileCandidate candidate = frames[index];
                if (candidate.Type == 0 || candidate.IsAvailable(_catalog.LocalNow))
                    candidates.Add(candidate.Id);
            }

            return Select(candidates, DefaultFrameId);
        }

        private string Select(IReadOnlyList<string> candidates, string fallback)
        {
            if (candidates == null || candidates.Count == 0)
                return fallback;
            return candidates[_random.Range(0, candidates.Count)];
        }

        private static void RequireKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A stable bot record key is required.", nameof(key));
        }
    }

    /// <summary>Adapts Cat player and bot services to the reusable Lava Rush roster contract.</summary>
    public sealed class CatLavaRushProfileRoster : LavaRushProfileRosterBase
    {
        private readonly CatPlayerProfileSourceBase _player;
        private readonly CatBotProfileService _bots;

        public CatLavaRushProfileRoster(
            CatPlayerProfileSourceBase player,
            CatBotProfileService bots)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _bots = bots ?? throw new ArgumentNullException(nameof(bots));
        }

        public override LavaRushProfileSnapshot GetPlayer()
        {
            CatPlayerProfileRecord player = _player.ReadPlayer();
            return new LavaRushProfileSnapshot(
                player.DisplayName,
                player.ProfileId,
                player.FrameId,
                player.HorizontalDirection);
        }

        public override LavaRushProfileSnapshot LoadOrGenerateOpponent(int stage, int slot)
        {
            ValidateStageAndSlot(stage, slot);
            CatBotProfileRecord bot = _bots.LoadOrGenerate(EnemyBotKey(stage, slot));
            return new LavaRushProfileSnapshot(
                bot.Name,
                bot.ProfileId,
                bot.FrameId,
                _bots.GetHorizontalDirection(bot.ProfileId));
        }

        public override void DeleteOpponents(int stage, int slotCount)
        {
            if (stage < 0)
                throw new ArgumentOutOfRangeException(nameof(stage));
            if (slotCount < 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount));

            for (int slot = 0; slot < slotCount; slot++)
                _bots.Delete(EnemyBotKey(stage, slot));
        }

        public static string EnemyBotKey(int stage, int slot)
        {
            ValidateStageAndSlot(stage, slot);
            return $"lava_rush_enemy_{stage}_{slot}";
        }

        private static void ValidateStageAndSlot(int stage, int slot)
        {
            if (stage < 0)
                throw new ArgumentOutOfRangeException(nameof(stage));
            if (slot < 0)
                throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }
}
