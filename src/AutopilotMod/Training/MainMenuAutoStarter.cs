using System.Linq;
using Bindito.Core;
using Timberborn.GameSceneLoading;
using Timberborn.MapRepositorySystem;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace TimberbornAutopilot.Training
{
    [Context("MainMenu")]
    public class AutopilotMainMenuConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<MainMenuAutoStarter>().AsSingleton();
        }
    }

    /// <summary>
    /// Training-mode entry: when training.json says Enabled, the main menu
    /// starts a fresh colony automatically (faction, map, settlement name from
    /// config) a few seconds after load — no human clicks needed.
    /// </summary>
    public class MainMenuAutoStarter : ILoadableSingleton, IUpdatableSingleton
    {
        private const int FramesBeforeStart = 180;

        private readonly GameSceneLoader _gameSceneLoader;
        private readonly MapRepository _mapRepository;

        private TrainingConfig _config;
        private int _frames;
        private bool _started;

        public MainMenuAutoStarter(GameSceneLoader gameSceneLoader, MapRepository mapRepository)
        {
            _gameSceneLoader = gameSceneLoader;
            _mapRepository = mapRepository;
        }

        public void Load()
        {
            _config = TrainingConfig.Load();
            if (_config.Enabled)
            {
                Debug.Log("[Autopilot:Training] Training mode ON — auto-starting " +
                          $"{_config.FactionId} on '{_config.MapName}' in a few seconds.");
            }
        }

        public void UpdateSingleton()
        {
            if (_started || !_config.Enabled || ++_frames < FramesBeforeStart)
            {
                return;
            }
            _started = true;
            string mapName = ResolveMapName(_config.MapName);
            Debug.Log($"[Autopilot:Training] Starting new game: {_config.FactionId} / {mapName} / " +
                      $"{_config.SettlementName}");
            _gameSceneLoader.StartNewGameInstantly(
                _config.FactionId, MapFileReference.FromResource(mapName), _config.SettlementName);
        }

        private string ResolveMapName(string requested)
        {
            var builtin = _mapRepository.GetBuiltinMapNames().ToList();
            string match = builtin.FirstOrDefault(
                name => name.ToLowerInvariant().Contains(requested.ToLowerInvariant()));
            if (match == null)
            {
                match = builtin.First();
                Debug.LogWarning($"[Autopilot:Training] Map '{requested}' not found; " +
                                 $"using '{match}'. Available: {string.Join(", ", builtin)}");
            }
            return match;
        }
    }
}
