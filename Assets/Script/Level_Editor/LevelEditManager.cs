using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelEditManager : MonoBehaviour {
    public static LevelEditManager Instance { get; private set; }

    public bool IsInEditMode = true;

    private List<GridPoint> currentSnakeGridPoints = new();

    public event System.Action<bool> OnSnakeCreationStarted; // for UI to know when to show finish/cancel buttons, bool indicates if we have at least 2 points to create a snake
    public event System.Action<UILineRenderer> OnSnakeSelected; // for UI to know when to show delete button

    private UILineRenderer currentSelectedSnake = null;
    void Awake() {
        Instance = this;
    }

    public void HandleGridPointClick(GridPoint point) {
        if (!IsInEditMode)
            return;

        // Don't allow using occupied cells
        if (point.OccupiedSnake != null) {
            currentSelectedSnake = point.OccupiedSnake;

            currentSelectedSnake.StartHighlight();

            OnSnakeSelected?.Invoke(currentSelectedSnake);
            return;
        }
        else {
            currentSelectedSnake = null;
        }


           

        currentSnakeGridPoints.Add(point);

        if (currentSnakeGridPoints.Count >= 2) {
            // has Atleast 2 point, start creating the snake
            OnSnakeCreationStarted?.Invoke(true);
        }
        else {
            // Only 1 point, keep waiting for more points
            OnSnakeCreationStarted?.Invoke(false);
        }
    }

    public void FinishSnake() {
        currentSnakeGridPoints.Reverse();

        SnakeCreator.Instance.CreateSnakeFromEditor(currentSnakeGridPoints);

        currentSnakeGridPoints.Clear();
    }

    public void SaveLevel() {
        SnakeLevelData levelData = ScriptableObject.CreateInstance<SnakeLevelData>();

        foreach (var snake in SnakeCreator.Instance.SpawnedSnakes) {
            SnakeData snakeData = new SnakeData();

            snakeData.color = snake.color;

            foreach (var point in snake.OccupiedGridPoints) {
                snakeData.cells.Add(point.GridCoordinate);
            }

            levelData.snakes.Add(snakeData);
        }
        // Save the ScriptableObject as an asset
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Level Data", "NewSnakeLevel", "asset", "New Snake Level Data");
        if (!string.IsNullOrEmpty(path)) {
            UnityEditor.AssetDatabase.CreateAsset(levelData, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Level data saved to {path}");
        }
#endif
    }


    public void CancelSnake() {
        foreach (var p in currentSnakeGridPoints)
            p.ResetColor();

        currentSnakeGridPoints.Clear();
    }

    public void DeleteSelectedSnake() {
        if(currentSelectedSnake == null) {
            Debug.LogWarning("No snake selected to delete!");
            return;
        }
           
        SnakeCreator.Instance.DeleteSnakeFromEditor(currentSelectedSnake);
    }

    public void SwapHeadSnake() {
        if (currentSelectedSnake == null) {
            Debug.LogWarning("No snake selected to swap!");
            return;
        }

        List<GridPoint> points = new(currentSelectedSnake.OccupiedGridPoints);
        Color snakeColor = currentSelectedSnake.color;
        points.Reverse();

        SnakeCreator.Instance.DeleteSnakeFromEditor(currentSelectedSnake);
        SnakeCreator.Instance.CreateSnakeFromEditor(points, snakeColor);

        currentSnakeGridPoints.Clear();

        currentSelectedSnake = SnakeCreator.Instance.SpawnedSnakes[^1];
    }
}