using System;
using System.Collections.Generic;
using ActionFit.LavaRush.UI;
using UnityEngine.Localization.Settings;

namespace ActionFit.Cat.App
{
    public readonly struct CatLavaRushLocalizationEntry
    {
        public CatLavaRushLocalizationEntry(
            string semanticKey,
            string tableEntry,
            long sharedDataId,
            bool smartFormat)
        {
            SemanticKey = semanticKey ?? throw new ArgumentNullException(nameof(semanticKey));
            TableEntry = tableEntry ?? throw new ArgumentNullException(nameof(tableEntry));
            SharedDataId = sharedDataId;
            SmartFormat = smartFormat;
        }

        public string SemanticKey { get; }
        public string TableEntry { get; }
        public long SharedDataId { get; }
        public bool SmartFormat { get; }
    }

    /// <summary>Project table and locale-change leaf used by the Cat product mapping.</summary>
    public abstract class CatLocalizationEnvironmentBase
    {
        public abstract event Action LocaleChanged;
        public abstract string GetLocalizedString(string table, string entry);
    }

    /// <summary>Direct Unity Localization leaf; Cat localization assets remain project-owned.</summary>
    public sealed class UnityCatLocalizationEnvironment : CatLocalizationEnvironmentBase, IDisposable
    {
        private bool _disposed;

        public UnityCatLocalizationEnvironment()
        {
            LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
        }

        public override event Action LocaleChanged;

        public override string GetLocalizedString(string table, string entry)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnityCatLocalizationEnvironment));
            return LocalizationSettings.StringDatabase.GetLocalizedString(table, entry);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
            LocaleChanged = null;
            _disposed = true;
        }

        private void HandleSelectedLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            LocaleChanged?.Invoke();
        }
    }

    /// <summary>
    /// Owns the complete Cat semantic-to-General mapping while preserving package fallbacks and
    /// explicit live-refresh lifetime.
    /// </summary>
    public sealed class CatLavaRushUILocalizer :
        LavaRushLocalizationRefreshSourceBase,
        ILavaRushUILocalizer,
        IDisposable
    {
        public const string TableName = "General";

        private static readonly CatLavaRushLocalizationEntry[] Entries =
        {
            new(LavaRushLocalizationKeys.DifficultyDescription, "lavarush_difficulty_desc", 42682123033763840L, false),
            new(LavaRushLocalizationKeys.EventEndDescription, "lavarush_end_desc", 42681629234159616L, false),
            new(LavaRushLocalizationKeys.MatchLosePrimary, "lavarush_lose_desc1", 42741296069074944L, false),
            new(LavaRushLocalizationKeys.MatchLoseRemaining, "lavarush_lose_desc2", 42741320979046400L, true),
            new(LavaRushLocalizationKeys.MatchLoseTertiary, "lavarush_lose_desc3", 42741357054255104L, false),
            new(LavaRushLocalizationKeys.MatchDescription, "lavarush_match_desc", 42738662671114240L, false),
            new(LavaRushLocalizationKeys.EventStartDescription, "lavarush_start_desc", 42681148432703488L, false),
            new(LavaRushLocalizationKeys.Title, "lavarush_title", 42680591513018368L, false),
            new(LavaRushLocalizationKeys.TutorialStep1, "lavarush_tutorial_desc1", 44559779329204224L, false),
            new(LavaRushLocalizationKeys.TutorialStep2, "lavarush_tutorial_desc2", 44559779329204225L, false),
            new(LavaRushLocalizationKeys.TutorialStep3, "lavarush_tutorial_desc3", 44559779329204226L, false),
            new(LavaRushLocalizationKeys.TutorialInfo1, "lavarush_tutorialinfo_desc1", 47168582696558592L, false),
            new(LavaRushLocalizationKeys.TutorialInfo2, "lavarush_tutorialinfo_desc2", 47168582738501632L, false),
            new(LavaRushLocalizationKeys.TutorialInfo3, "lavarush_tutorialinfo_desc3", 47168582738501633L, false),
            new(LavaRushLocalizationKeys.TutorialInfo4, "lavarush_tutorialinfo_desc4", 47168582738501634L, false),
            new(LavaRushLocalizationKeys.MatchWinPrimary, "lavarush_win_desc1", 42741374745829376L, false),
            new(LavaRushLocalizationKeys.MatchWinSecondary, "lavarush_win_desc2", 42741391212666880L, false),
            new(LavaRushLocalizationKeys.MatchWinComplete, "lavarush_win_desc3", 42741409722130432L, false),
        };

        private readonly CatLocalizationEnvironmentBase _environment;
        private readonly Dictionary<string, CatLavaRushLocalizationEntry> _entryBySemanticKey;
        private bool _initialized;
        private bool _disposed;

        public CatLavaRushUILocalizer(CatLocalizationEnvironmentBase environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _entryBySemanticKey = BuildMapping();
        }

        public static IReadOnlyList<CatLavaRushLocalizationEntry> Mappings => Entries;

        public override event Action LocaleChanged;

        public void Initialize()
        {
            ThrowIfDisposed();
            if (_initialized)
                return;
            _environment.LocaleChanged += HandleLocaleChanged;
            _initialized = true;
        }

        public string Get(string key, string fallback)
        {
            ThrowIfDisposed();
            if (!_entryBySemanticKey.TryGetValue(key ?? string.Empty, out CatLavaRushLocalizationEntry entry))
                return fallback ?? string.Empty;

            string localized = _environment.GetLocalizedString(TableName, entry.TableEntry);
            return string.IsNullOrWhiteSpace(localized) ? fallback ?? string.Empty : localized;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            if (_initialized)
                _environment.LocaleChanged -= HandleLocaleChanged;
            LocaleChanged = null;
            _initialized = false;
            _disposed = true;
        }

        private static Dictionary<string, CatLavaRushLocalizationEntry> BuildMapping()
        {
            var result = new Dictionary<string, CatLavaRushLocalizationEntry>(StringComparer.Ordinal);
            for (int index = 0; index < Entries.Length; index++)
                result.Add(Entries[index].SemanticKey, Entries[index]);

            AddCompatibilityAlias(result, LavaRushUIKeys.Title, LavaRushLocalizationKeys.Title);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageStart, LavaRushLocalizationKeys.EventStartDescription);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageDifficulty, LavaRushLocalizationKeys.DifficultyDescription);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageTutorial, LavaRushLocalizationKeys.TutorialStep1);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageReady, LavaRushLocalizationKeys.MatchDescription);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessagePlaying, LavaRushLocalizationKeys.MatchDescription);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageWin, LavaRushLocalizationKeys.MatchWinPrimary);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageLose, LavaRushLocalizationKeys.MatchLosePrimary);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageComplete, LavaRushLocalizationKeys.MatchWinComplete);
            AddCompatibilityAlias(result, LavaRushUIKeys.MessageEventEnd, LavaRushLocalizationKeys.EventEndDescription);
            return result;
        }

        private static void AddCompatibilityAlias(
            IDictionary<string, CatLavaRushLocalizationEntry> mapping,
            string compatibilityKey,
            string semanticKey)
        {
            mapping.Add(compatibilityKey, mapping[semanticKey]);
        }

        private void HandleLocaleChanged()
        {
            LocaleChanged?.Invoke();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CatLavaRushUILocalizer));
        }
    }
}
