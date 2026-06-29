using UnityEngine;

namespace Game {
    public class GameManager : MonoBehaviour {

        public static GameManager Instance { get; private set; }

        [Header("Level Data")]
        [SerializeField] private SnakeLevelData snakeLevelData;

        private void Awake() {
            Instance = this;
        }


        private void Start() {
            if (snakeLevelData != null) {
                SnakeCreator.Instance.LoadLevel(snakeLevelData);
                Timer.Instance.StartCounter();
            }
            else {
                Debug.LogError("SnakeLevelData is not assigned in the GameManager.");
            }

            SnakeCreator.Instance.OnAllSnakesRemoved += SnakeCreator_Instance_OnAllSnakesRemoved;
        }

        private void SnakeCreator_Instance_OnAllSnakesRemoved() {
            Timer.Instance.StopCounter();
        }

        private void OnDestroy() {
            SnakeCreator.Instance.OnAllSnakesRemoved -= SnakeCreator_Instance_OnAllSnakesRemoved;
        }
    }
}

