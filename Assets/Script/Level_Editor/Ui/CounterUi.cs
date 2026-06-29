using Game;
using TMPro;
using UnityEngine;

public class CounterUi : MonoBehaviour
{
    [SerializeField] private TMP_Text counterText;
    private GameManager gameManager;
   

    private void Start() {
        gameManager = GameManager.Instance;
    }

    private void Update() {
        if (counterText != null) {
            UpdateTotalTime();
        }
    }

    private void UpdateTotalTime() {
        float t = Timer.Instance.GetCounterValue();
        int mins = (int)(t / 60);
        int secs = (int)(t % 60);
        counterText.text = $"{mins:00}:{secs:00}";
    }
}
