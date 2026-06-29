using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinUI : MonoBehaviour
{
    [SerializeField] private GameObject winUIPanel;
    [SerializeField] private Button restartGameButton;
    [SerializeField] private TMP_Text totalTimeText;


    private void Awake() {
        restartGameButton.onClick.AddListener(RestartGame);
        winUIPanel.SetActive(false);
    }


    private void Start() {
        SnakeCreator.Instance.OnAllSnakesRemoved += HandleAllSnakesRemoved;
    }

    private void RestartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleAllSnakesRemoved() {
        winUIPanel.SetActive(true);
        UpdateTotalTime();
    }

    private void OnDestroy() {
        SnakeCreator.Instance.OnAllSnakesRemoved -= HandleAllSnakesRemoved;
        restartGameButton.onClick.RemoveListener(RestartGame);
    }

    private void UpdateTotalTime() {
        float t = Timer.Instance.GetCounterValue();
        int mins = (int)(t / 60);
        int secs = (int)(t % 60);
        totalTimeText.text = $"TOTAL TIME: {mins:00}:{secs:00}";
    }
}
