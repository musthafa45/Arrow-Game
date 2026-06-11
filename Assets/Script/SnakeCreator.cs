using System.Collections.Generic;
using UnityEngine;

public class SnakeCreator : MonoBehaviour {
    [Header("References")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private RectTransform snakeContainer; // parent UI panel

    [Header("Snake Settings")]
    [SerializeField] private int snakeCount = 8;
    [SerializeField] private int minLength = 3;
    [SerializeField] private int maxLength = 8;
    [SerializeField] private float lineWidth = 6f;
    [SerializeField] private int maxTries = 100;

    private List<Vector2> _available = new();
    private HashSet<Vector2> _occupied = new();
    private List<UILineRenderer> _snakes = new();

    void Start() => Generate();

    [ContextMenu("Generate Snakes")]
    public void Generate() {
        // Clear old snakes
        foreach (var s in _snakes)
            if (s != null) DestroyImmediate(s.gameObject);
        _snakes.Clear();
        _occupied.Clear();

        // Clear all grid point occupancy
        foreach (var kv in gridGenerator.PointMap)
            kv.Value.SetFree();

        // Pull interior + outline points
        _available = new List<Vector2>();
        _available.AddRange(gridGenerator.ShapeInterior);
        _available.AddRange(gridGenerator.ShapeOutline);

        if (_available.Count == 0) {
            Debug.LogWarning("[SnakeCreator] No points — run GridGenerator first!");
            return;
        }

        for (int i = 0; i < snakeCount; i++) {
            List<Vector2> path = TryBuildSnake();
            if (path != null && path.Count >= minLength)
                SpawnSnake(path);
        }

        Debug.Log($"[SnakeCreator] Spawned {_snakes.Count}/{snakeCount} snakes.");
    }

    // ── Snake Path Builder ────────────────────────────────────────────

    private List<Vector2> TryBuildSnake() {
        float step = EstimateGridStep();

        for (int attempt = 0; attempt < maxTries; attempt++) {
            Vector2 start = RandomFreePoint();
            if (start == Vector2.negativeInfinity) return null;

            List<Vector2> path = new() { start };
            _occupied.Add(start);

            int target = Random.Range(minLength, maxLength + 1);

            while (path.Count < target) {
                Vector2 next = GetNextStep(path[path.Count - 1], step);
                if (next == Vector2.negativeInfinity) break;

                path.Add(next);
                _occupied.Add(next);
            }

            if (path.Count >= minLength) return path;

            // Rollback
            foreach (var p in path) _occupied.Remove(p);
        }

        return null;
    }

    private Vector2 GetNextStep(Vector2 current, float step) {
        // 8 directions shuffled
        Vector2[] dirs = ShuffledDirs(step);

        foreach (var dir in dirs) {
            Vector2 candidate = current + dir;
            Vector2 snapped = SnapToGrid(candidate, step);

            if (snapped == Vector2.negativeInfinity) continue;
            if (_occupied.Contains(snapped)) continue;

            return snapped;
        }

        return Vector2.negativeInfinity;
    }

    // ── Spawn ─────────────────────────────────────────────────────────

    private void SpawnSnake(List<Vector2> path) {

        if (path == null || path.Count < 2) return;
        if (Vector2.Distance(path[0], path[1]) < 0.001f) return;

        GameObject go = new GameObject("Snake");
        go.transform.SetParent(snakeContainer, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        UILineRenderer line = go.AddComponent<UILineRenderer>();
        line.lineWidth = lineWidth;

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
        line.SetColor(RandomColor());
        _snakes.Add(line);

        // ── Register snake on each GridPoint ──────────────────────────
        for (int i = 0; i < path.Count; i++) {
            if (gridGenerator.PointMap.TryGetValue(path[i], out GridPoint gp))
                gp.SetOccupied(line, i);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private float EstimateGridStep() {
        if (_available.Count < 2) return 1f;
        float min = float.MaxValue;
        for (int i = 1; i < Mathf.Min(_available.Count, 20); i++) {
            float d = Vector2.Distance(_available[0], _available[i]);
            if (d > 0.01f && d < min) min = d;
        }
        return min;
    }

    private Vector2 SnapToGrid(Vector2 pos, float step) {
        float best = float.MaxValue;
        Vector2 found = Vector2.negativeInfinity;

        foreach (var p in _available) {
            float d = Vector2.Distance(pos, p);
            if (d < step * 0.6f && d < best) { best = d; found = p; }
        }

        return found;
    }

    private Vector2 RandomFreePoint() {
        List<Vector2> free = new();
        foreach (var p in _available)
            if (!_occupied.Contains(p)) free.Add(p);

        return free.Count > 0
            ? free[Random.Range(0, free.Count)]
            : Vector2.negativeInfinity;
    }

    private Vector2[] ShuffledDirsDiagnol(float step) {
        Vector2[] dirs =
        {
            new( step,     0), new(-step,    0),
            new(    0,  step), new(    0, -step),
            new( step,  step), new(-step,  step),
            new( step, -step), new(-step, -step),
        };

        for (int i = dirs.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        return dirs;
    }

    private Vector2[] ShuffledDirs(float step) {
        Vector2[] dirs =
        {
        new( step,    0),   // right
        new(-step,    0),   // left
        new(    0,  step),  // up
        new(    0, -step),  // down
        
    };

        for (int i = dirs.Length - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        return dirs;
    }

    private static readonly Color[] Palette =
    {
        Color.red,Color.blue, Color.green, Color.violet, Color.brown,Color.orange,Color.yellow
    };

    private Color RandomColor() => Palette[Random.Range(0, Palette.Length)];
}