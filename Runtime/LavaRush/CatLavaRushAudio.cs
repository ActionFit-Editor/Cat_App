using System;
using System.Collections.Generic;
using ActionFit.LavaRush.UI;

namespace ActionFit.Cat.App
{
    /// <summary>Maps every restored Lava Rush cue to the current Cat playback meaning.</summary>
    public sealed class CatLavaRushAudio : LavaRushAudioBase
    {
        public const string MixerGroup = "SFX";
        public const float DefaultVolume = 0.5f;
        public const float ProfilePitchMin = 0.85f;
        public const float ProfilePitchMax = 1.2f;

        private readonly CatSoundService _sound;
        private readonly IReadOnlyDictionary<LavaRushAudioCue, string> _clipIds;

        public CatLavaRushAudio(
            CatSoundService sound,
            IReadOnlyDictionary<LavaRushAudioCue, string> clipIds)
        {
            _sound = sound ?? throw new ArgumentNullException(nameof(sound));
            _clipIds = clipIds ?? throw new ArgumentNullException(nameof(clipIds));
            ValidateCompleteMapping();
        }

        public override void Play(LavaRushAudioCue cue)
        {
            CatSoundChannel channel = cue == LavaRushAudioCue.TutorialStep
                ? CatSoundChannel.Single
                : CatSoundChannel.Overlay;
            _sound.Play(new CatSoundPlayback(
                _clipIds[cue],
                channel,
                MixerGroup,
                DefaultVolume,
                false,
                1f,
                1f));
        }

        public override void PlayPitched(
            LavaRushAudioCue cue,
            float volume,
            float pitchMin,
            float pitchMax)
        {
            _sound.Play(new CatSoundPlayback(
                _clipIds[cue],
                CatSoundChannel.Overlay,
                MixerGroup,
                volume,
                true,
                pitchMin,
                pitchMax));
        }

        public static IReadOnlyDictionary<LavaRushAudioCue, string> CreateLegacyClipIds()
        {
            return new Dictionary<LavaRushAudioCue, string>
            {
                [LavaRushAudioCue.DifficultySelect] = "lava-rush.difficulty-select",
                [LavaRushAudioCue.RewardSpawn] = "lava-rush.reward-spawn",
                [LavaRushAudioCue.RewardArrive] = "lava-rush.reward-arrive",
                [LavaRushAudioCue.WinJump] = "lava-rush.win-jump",
                [LavaRushAudioCue.BlockClear] = "lava-rush.block-clear",
                [LavaRushAudioCue.MatchWin] = "lava-rush.match-win",
                [LavaRushAudioCue.MatchLose] = "lava-rush.match-lose",
                [LavaRushAudioCue.ProfileAppear] = "lava-rush.profile-appear",
                [LavaRushAudioCue.TutorialStep] = "lava-rush.tutorial-step",
                [LavaRushAudioCue.FinalRewardOpen] = "lava-rush.final-reward-open",
            };
        }

        private void ValidateCompleteMapping()
        {
            foreach (LavaRushAudioCue cue in Enum.GetValues(typeof(LavaRushAudioCue)))
            {
                if (!_clipIds.TryGetValue(cue, out string clipId)
                    || string.IsNullOrWhiteSpace(clipId))
                {
                    throw new ArgumentException(
                        $"A clip identifier is required for {cue}.",
                        nameof(_clipIds));
                }
            }
        }
    }
}
