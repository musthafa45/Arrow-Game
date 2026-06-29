using System;
using UnityEngine;

namespace LevelEditor {
    public enum GameMode {
        PlayMode,
        LevelEditorMode
    }

    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        public event Action<bool> OnLevelLoadedWithCustomLevel;

        [SerializeField] private GameMode currentGameMode = GameMode.PlayMode;

        [Header("Level Data")]
        [SerializeField] private SnakeLevelData snakeLevelData;

        public GameMode CurrentGameMode => currentGameMode;

        private void Awake() {
            Instance = this;
        }

        private void Start() {
            if (currentGameMode == GameMode.PlayMode) {
                // Initialize the game in Play Mode
                if (snakeLevelData != null) {
                    SnakeCreator.Instance.LoadLevel(snakeLevelData);
                    Timer.Instance.StartCounter();
                }
                else {
                    Debug.LogError("SnakeLevelData is not assigned in the GameManager.");
                }

                SnakeCreator.Instance.OnAllSnakesRemoved += SnakeCreator_Instance_OnAllSnakesRemoved;

            }
            else if (currentGameMode == GameMode.LevelEditorMode) {
                // Initialize the game in Level Editor Mode

                if (snakeLevelData != null) {
                    SnakeCreator.Instance.LoadLevel(snakeLevelData);
                    // Notify subscribers that the level has been loaded
                    OnLevelLoadedWithCustomLevel?.Invoke(true);
                }
                else {
                    Debug.Log("Starting Level Editor Mode without a predefined level. You can create a new level.");
                    OnLevelLoadedWithCustomLevel?.Invoke(false);
                }
            }
        }

        private void SnakeCreator_Instance_OnAllSnakesRemoved() {
            Timer.Instance.StopCounter();
        }

        private void OnDestroy() {
            SnakeCreator.Instance.OnAllSnakesRemoved -= SnakeCreator_Instance_OnAllSnakesRemoved;
        }
    }
}

