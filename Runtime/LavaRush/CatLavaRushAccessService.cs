using System;
using ActionFit.LavaRush.UI;

namespace ActionFit.Cat.App.LavaRush
{
    /// <summary>Defines the live project-manager reads used by the Cat Lava Rush access service.</summary>
    public sealed class CatLavaRushAccessBinding
    {
        public CatLavaRushAccessBinding(
            Func<bool> isEventActive,
            Func<bool> isEventStarted,
            Func<DateTime> eventEndTime,
            Func<TimeSpan> eventRemainTime,
            Action openContent)
        {
            IsEventActive = isEventActive ?? throw new ArgumentNullException(nameof(isEventActive));
            IsEventStarted = isEventStarted ?? throw new ArgumentNullException(nameof(isEventStarted));
            EventEndTime = eventEndTime ?? throw new ArgumentNullException(nameof(eventEndTime));
            EventRemainTime = eventRemainTime ?? throw new ArgumentNullException(nameof(eventRemainTime));
            OpenContent = openContent ?? throw new ArgumentNullException(nameof(openContent));
        }

        public Func<bool> IsEventActive { get; }
        public Func<bool> IsEventStarted { get; }
        public Func<DateTime> EventEndTime { get; }
        public Func<TimeSpan> EventRemainTime { get; }
        public Action OpenContent { get; }
    }

    /// <summary>Adapts the current Cat manager access state to the neutral Lava Rush UI port.</summary>
    public sealed class CatLavaRushAccessService : LavaRushAccessServiceBase
    {
        private readonly CatLavaRushAccessBinding _binding;

        public CatLavaRushAccessService(CatLavaRushAccessBinding binding)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        }

        public override bool IsEventActive => _binding.IsEventActive();
        public override bool IsEventStarted => _binding.IsEventStarted();
        public override DateTime EventEndTime => _binding.EventEndTime();
        public override TimeSpan EventRemainTime => _binding.EventRemainTime();

        public override void OpenContent() => _binding.OpenContent();
    }
}
