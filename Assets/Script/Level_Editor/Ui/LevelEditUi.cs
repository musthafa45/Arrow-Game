using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelEditUi : MonoBehaviour {
    [SerializeField] private Button finishSnakeBtn, cancelSnakeBtn, saveLevelBtn,deleteSnakeBtn, swapHeadBtn;
    [SerializeField] private Toggle canOverlapSnake,canGoDiagonal;
    [SerializeField] private GameObject levelEditPanel,levelEditPanel2;
    [SerializeField] private TMP_Dropdown colorDropDown;
    [SerializeField] private Image colorPreviewImage;

    private void Start() {
        if (LevelEditManager.Instance != null) {
           if(LevelEditManager.Instance.IsInEditMode) {
                levelEditPanel.SetActive(true);

                finishSnakeBtn.onClick.AddListener(() =>
                {
                    LevelEditManager.Instance.FinishSnake();

                    finishSnakeBtn.interactable = false;
                    cancelSnakeBtn.interactable = false;
                    saveLevelBtn.interactable = true;
                });

                cancelSnakeBtn.onClick.AddListener(() => { 
                    LevelEditManager.Instance.CancelSnake();

                    finishSnakeBtn.interactable = false;
                    cancelSnakeBtn.interactable = false;
                    saveLevelBtn.interactable = SnakeCreator.Instance.SpawnedSnakes.Count > 0; 
                    deleteSnakeBtn.interactable = false; 
                });

                saveLevelBtn.onClick.AddListener(() => LevelEditManager.Instance.SaveLevel());

                deleteSnakeBtn.onClick.AddListener(() => {
                    LevelEditManager.Instance.DeleteSelectedSnake();
                    
                    saveLevelBtn.interactable = SnakeCreator.Instance.SpawnedSnakes.Count > 0;
                    deleteSnakeBtn.interactable = false;
                    swapHeadBtn.interactable = false;
                });

                swapHeadBtn.onClick.AddListener(() => {
                    LevelEditManager.Instance.SwapHeadSnake();
                });

                canOverlapSnake.isOn = LevelEditManager.Instance.CanOverlapSnake;

                canOverlapSnake.onValueChanged.AddListener((value) => {
                    LevelEditManager.Instance.CanOverlapSnake = value;
                });

                canGoDiagonal.isOn = LevelEditManager.Instance.CanGoDiagonal;

                canGoDiagonal.onValueChanged.AddListener((value) => {
                    LevelEditManager.Instance.CanGoDiagonal = value;
                });

                finishSnakeBtn.interactable = false; 
                cancelSnakeBtn.interactable = false;
                saveLevelBtn.interactable = false; 
                deleteSnakeBtn.interactable = false; 
                swapHeadBtn.interactable = false;

                LevelEditManager.Instance.OnSnakeCreationStarted += (hasValidPointsForSnakes) => { 

                    finishSnakeBtn.interactable = hasValidPointsForSnakes;
                    cancelSnakeBtn.interactable = true;
                    saveLevelBtn.interactable = false; 
                    deleteSnakeBtn.interactable = false; 
                };

                LevelEditManager.Instance.OnSnakeSelected += (snake) =>
                {
                    deleteSnakeBtn.interactable = true;
                    swapHeadBtn.interactable = true;

                    int index = (int)snake.SnakeColor;
                    colorPreviewImage.color = GetColor(snake.SnakeColor);

                    if (index >= 0 && index < colorDropDown.options.Count) {
                        colorDropDown.SetValueWithoutNotify(index);
                        colorDropDown.RefreshShownValue();
                    }

                    colorDropDown.onValueChanged.RemoveAllListeners();

                    colorDropDown.onValueChanged.AddListener((index) =>
                    {
                        if(snake != null) {
                            snake.SetColor((SnakeColor)index);
                            colorPreviewImage.color = GetColor(snake.SnakeColor);
                        }
                        else {
                            Debug.LogWarning("No snake selected to change color.");
                            colorPreviewImage.color = Color.white;
                        }
                        
                    });
                };
            }
           else {
                levelEditPanel.SetActive(false);
                levelEditPanel2.SetActive(false);
           }
        }

       
    }

    public Color GetColor(SnakeColor snakeColor) {
        return snakeColor switch {
            SnakeColor.Red => Color.red,
            SnakeColor.Green => Color.green,
            SnakeColor.Blue => Color.blue,
            SnakeColor.Yellow => Color.yellow,
            SnakeColor.Cyan => Color.cyan,
            SnakeColor.Magenta => Color.magenta,
            SnakeColor.Orange => Color.orange,
            SnakeColor.Purple => Color.purple,
            SnakeColor.Brown => Color.brown,
            SnakeColor.Violet => Color.violet,
            _ => Color.white 
        };
    }

    private void OnDestroy() {
        finishSnakeBtn.onClick.RemoveAllListeners();
        cancelSnakeBtn.onClick.RemoveAllListeners();
        saveLevelBtn.onClick.RemoveAllListeners();
    }
}
