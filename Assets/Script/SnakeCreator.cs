using System.Collections.Generic;
using UnityEngine;

public class SnakeCreator : MonoBehaviour {

    public static SnakeCreator Instance { get; private set; }


    [Header("References")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private RectTransform snakeContainer; // parent UI panel

    [Header("Snake Settings")]
    [SerializeField] private float snakeBodyWidth = 20f;


    private List<SnakeRenderer> _snakes = new();

    public IReadOnlyList<SnakeRenderer> SpawnedSnakes => _snakes;

    private void Awake() {
        Instance = this;
    }

    public void LoadLevel(SnakeLevelData levelData) {
        if (levelData == null) {
            Debug.LogWarning("[SnakeCreator] LoadLevel called with null levelData.");
            return;
        }

        foreach (var snake in levelData.snakes) {
            List<Vector2> path = new();

            foreach (var cell in snake.cells) {
                if (gridGenerator.CellMap.TryGetValue(cell, out GridPoint gp)) {
                    path.Add(gp.LocalPosition);
                }
            }

            SpawnSnake(path, snake.color);
        }
    }

    // ── Spawn ─────────────────────────────────────────────────────────

    private SnakeRenderer SpawnSnake(List<Vector2> path, Color color, bool isPreview = false) {

        if (path == null || path.Count < 2) {
            Debug.LogWarning("[SnakeCreator] path too short for snake spawn.");
            return null;
        } 
        if (Vector2.Distance(path[0], path[1]) < 0.001f) {
            Debug.LogWarning("[SnakeCreator] path points too close for snake spawn.");
            return null;
        }

        GameObject go = new GameObject("Snake");
        go.transform.SetParent(snakeContainer, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SnakeRenderer line = go.AddComponent<SnakeRenderer>();
        line.lineWidth = snakeBodyWidth;

        // ── Convert grid local points → world → snake local ──────────
        List<Vector2> converted = new();
        foreach (var p in path) {
            // Grid local → world
            Vector3 world = gridGenerator.GridParent.TransformPoint(p);

            // World → snake RectTransform local
            Vector3 local = rt.InverseTransformPoint(world);
            converted.Add(new Vector2(local.x, local.y));
        }

        line.SetPoints(converted);
        line.SetColor(color);

        if(!isPreview) {
            _snakes.Add(line);

            // ── Register snake on each GridPoint ──────────────────────────
            List<GridPoint> occupiedGp = new();

            for (int i = 0; i < path.Count; i++) {
                if (gridGenerator.PointMap.TryGetValue(path[i], out GridPoint gp)) {
                    gp.SetOccupied(line, i);
                    occupiedGp.Add(gp);
                }
            }

            line.SetOccupiedGridPoints(occupiedGp);
        }
        else {
            Destroy(line.gameObject,2f);
            
        }
        return line;

    }

    private static readonly Color[] Palette =
    {
        Color.red,Color.blue, Color.green, Color.violet, Color.brown,Color.orange,Color.yellow
    };

    private Color RandomColor() => Palette[Random.Range(0, Palette.Length)];

    public SnakeRenderer CreateSnakeFromEditor(List<GridPoint> points, Color? snakeColor = null) {
        List<Vector2> path = new();
        foreach (var p in points) path.Add(p.LocalPosition);
        return SpawnSnake(path, snakeColor ?? RandomColor());
    }

    public void DeleteSnakeFromEditor(SnakeRenderer currentSelectedSnake) {
        _snakes.Remove(currentSelectedSnake);
        Destroy(currentSelectedSnake.gameObject);
    }

    public void CreatePreviewSnakeFromEditor(List<GridPoint> points) {
        List<Vector2> path = new();

        foreach (var p in points)
            path.Add(p.LocalPosition);

        Color preview = new Color(1f, 0f, 0f, 0.5f);

        SpawnSnake(path, preview, true);
    }

    public void RemoveSnakeFromList(SnakeRenderer snakeRenderer) {
        if(!_snakes.Contains(snakeRenderer)) {
            Debug.LogWarning("[SnakeCreator] Snake not found in list.");
            return;
        }
        else {
            _snakes.Remove(snakeRenderer);

            
            Debug.Log("[SnakeCreator] Snake removed from list.");
        }
        
    }

    public void DeleteAllSnakes() {
        foreach (var snake in _snakes) {
            Destroy(snake.gameObject);
        }
        _snakes.Clear();
    }
}