namespace QuantumConnect
{
    /// <summary>
    /// Small bundle of gameplay services to avoid long constructors.
    /// </summary>
    public sealed class GameServices
    {
        public CubeManager Cube { get; }
        public BlackHoleManager BlackHoles { get; }
        public IAudioService Audio { get; }
        public IBoardRules Rules { get; }
        public GameTuning Tuning { get; }
        public IGameVfx Vfx { get; }

        public GameServices(
            CubeManager cube,
            BlackHoleManager blackHoles,
            IAudioService audio,
            IBoardRules rules,
            GameTuning tuning,
            IGameVfx vfx)
        {
            Cube = cube;
            BlackHoles = blackHoles;
            Audio = audio;
            Rules = rules;
            Tuning = tuning;
            Vfx = vfx;
        }
    }
}
