using System;

namespace ActionFit.Cat.App
{
    public sealed class CatLoop
    {
        private float _secondAccumulator;

        public float GameSpeed { get; private set; } = 1f;
        public bool IsGameActive { get; set; } = true;

        public event Action<float> UpdateRequested;
        public event Action<float> LateUpdateRequested;
        public event Action<float> GameUpdateRequested;
        public event Action EverySecondRequested;

        public void AdvanceFrame(float deltaTime, float unscaledDeltaTime)
        {
            UpdateRequested?.Invoke(deltaTime);
            _secondAccumulator += unscaledDeltaTime;
            while (_secondAccumulator >= 1f)
            {
                _secondAccumulator -= 1f;
                EverySecondRequested?.Invoke();
            }
        }

        public void AdvanceLateFrame(float deltaTime)
        {
            LateUpdateRequested?.Invoke(deltaTime);
        }

        public void AdvanceGame(float deltaTime)
        {
            if (!IsGameActive)
            {
                return;
            }

            GameUpdateRequested?.Invoke(deltaTime * GameSpeed);
        }

        public void SetGameSpeed(float gameSpeed)
        {
            GameSpeed = gameSpeed;
        }

        public void ResetGameEvent()
        {
            GameUpdateRequested = null;
        }

        public void Clear()
        {
            UpdateRequested = null;
            LateUpdateRequested = null;
            GameUpdateRequested = null;
            EverySecondRequested = null;
        }
    }
}
