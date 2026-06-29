using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelEditManager : MonoBehaviour {
    public static LevelEditManager Instance { get; private set; }

    public event Action<bool> OnSnakeCreationStarted;
    public event Action<SnakeRenderer> OnSnakeSelected; 
    public event Action OnNudgeLayoutPerformed; 
    public event Action OnHeadSwapPerformed; 

    [HideInInspector] public bool CanOverlapSnake = false;
    [HideInInspector] public bool CanGoDiagonal = false;

    private List<GridPoint> currentSnakeGridPoints = new();

    private SnakeRenderer currentSelectedSnake = null;

    void Awake() {
        Instance = this;
    }

    public void HandleGridPointClick(GridPoint point) {


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
            GridPoint previous = currentSnakeGridPoints[^2];
            GridPoint current = currentSnakeGridPoints[^1];

            List<GridPoint> between = GridGenerator.Instance.GetPointsBetween(previous, current);

            // Remove the current point so we can insert the full path.
            currentSnakeGridPoints.RemoveAt(currentSnakeGridPoints.Count - 1);

            foreach (GridPoint p in between) {
                if (currentSnakeGridPoints.Count == 0 || currentSnakeGridPoints[^1] != p)
                    currentSnakeGridPoints.Add(p);
            }

            OnSnakeCreationStarted?.Invoke(true);
        }
        else {
            OnSnakeCreationStarted?.Invoke(false);
        }
    }

    public void FinishSnake() {
        if (IsSnakeCollidingWithSnake(currentSnakeGridPoints) && !CanOverlapSnake) {

            currentSnakeGridPoints.Reverse();
            SnakeCreator.Instance.CreatePreviewSnakeFromEditor(currentSnakeGridPoints);
            currentSnakeGridPoints.Clear();

            Debug.LogWarning("Cannot create snake on top of another snake!");
            return;
        }

        if (IsSnakeGoingDiagonal(currentSnakeGridPoints) && !CanGoDiagonal) {

            currentSnakeGridPoints.Reverse();
            SnakeCreator.Instance.CreatePreviewSnakeFromEditor(currentSnakeGridPoints);
            currentSnakeGridPoints.Clear();

            Debug.LogWarning("Cannot create diagonal snake!");
            return;
        }

        currentSnakeGridPoints.Reverse();
        SnakeCreator.Instance.CreateSnakeFromEditor(currentSnakeGridPoints);
        currentSnakeGridPoints.Clear();
    }

    private bool IsSnakeGoingDiagonal(List<GridPoint> currentSnakeGridPoints) {
        for (int i = 1; i < currentSnakeGridPoints.Count; i++) {
            GridPoint previous = currentSnakeGridPoints[i - 1];
            GridPoint current = currentSnakeGridPoints[i];

            if (Math.Abs(current.GridCoordinate.x - previous.GridCoordinate.x) == 1 && Math.Abs(current.GridCoordinate.y - previous.GridCoordinate.y) == 1) {
                return true;
            }
        }
        return false;
    }

    private bool IsSnakeCollidingWithSnake(List<GridPoint> currentSnakeGridPoints) {
        for (int i = 0; i < currentSnakeGridPoints.Count; i++) {
            if (currentSnakeGridPoints[i].OccupiedSnake != null) {
                Debug.LogWarning("Cannot create snake on top of another snake!");
                return true;
            }
        }
        return false;
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

        OnHeadSwapPerformed?.Invoke();
    }

    public void NudgeLayout(Vector2Int offset) {
        var snapshot = new List<(SnakeRenderer snake, List<GridPoint> newPoints)>();

        // Pass 1: validate + collect new points
        foreach (var snake in SnakeCreator.Instance.SpawnedSnakes) {
            List<GridPoint> newPoints = new();

            foreach (var gp in snake.OccupiedGridPoints) {
                Vector2Int newCoord = gp.GridCoordinate + offset;

                if (!GridGenerator.Instance.CellMap.TryGetValue(newCoord, out GridPoint newGP)) {
                    Debug.LogWarning($"Nudge out of bounds at {newCoord}");
                    return;
                }

                newPoints.Add(newGP);
            }

            snapshot.Add((snake, newPoints));
        }

        // Pass 2: clear all old occupation first
        foreach (var (snake, _) in snapshot)
            foreach (var gp in snake.OccupiedGridPoints)
                gp.ClearIfOwnedBy(snake);

        // Pass 3: apply all new positions
        foreach (var (snake, newPoints) in snapshot) {
            snake.OccupiedGridPoints = newPoints;
            snake.SetPoints(newPoints.ConvertAll(gp => gp.LocalPosition));

            foreach (var gp in newPoints)
                gp.OccupiedSnake = snake;
        }

        OnNudgeLayoutPerformed?.Invoke();
    }

}