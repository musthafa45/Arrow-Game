using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour {

    public static GridGenerator Instance { get; private set; }
    public enum ShapeType { Circle, Square, Triangle, Diamond, Carrot }

    [Header("Grid Settings")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private ShapeType shapeType = ShapeType.Circle;
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float sizeScale = 0.9f;
    [SerializeField] private float outlineThickness = 0.6f;

    private IGridShape _shape;

    public RectTransform GridParent => gridParent;

    // ── All grid points ───────────────────────────────────────────────
    public List<Vector2> GridPoints { get; private set; } = new();

    // ── Classified points ─────────────────────────────────────────────
    public List<Vector2> ShapeInterior { get; private set; } = new();
    public List<Vector2> ShapeOutline { get; private set; } = new();
    public List<Vector2> ShapeExterior { get; private set; } = new();

    [Header("Grid Point Interaction")]
    [SerializeField] private bool spawnClickPoints = true;
    [SerializeField] private float pointClickSize = 20f; // size of hitbox in UI units

    // Fast lookup: local position → GridPoint
    public Dictionary<Vector2, GridPoint> PointMap { get; private set; } = new();
    public Dictionary<Vector2Int, GridPoint> CellMap = new();

    void Awake() {
        Instance = this;
        GenerateAndClassify();
    }

    [ContextMenu("Refresh Grid Points")]
    public void Refresh() {
        GenerateAndClassify();
    }

    // ── Core ──────────────────────────────────────────────────────────

    private void GenerateAndClassify() {
        if (gridParent == null) { Debug.LogWarning("[GridGenerator] gridParent not assigned!"); return; }

        // Clear old point GameObjects
        foreach (var kv in PointMap)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        PointMap.Clear();
        CellMap.Clear();

        _shape = CreateShape(shapeType);

        float W = gridParent.rect.width;
        float H = gridParent.rect.height;
        float xStep = columns > 1 ? W / (columns - 1) : 0f;
        float yStep = rows > 1 ? H / (rows - 1) : 0f;
        float startX = -W / 2f;
        float startY = -H / 2f;
        float size = Mathf.Min(W, H) / 2f * sizeScale;
        float thick = Mathf.Min(xStep, yStep) * outlineThickness;
        Vector2 center = Vector2.zero;

        GridPoints.Clear();
        ShapeInterior.Clear();
        ShapeOutline.Clear();
        ShapeExterior.Clear();

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                Vector2 point = new Vector2(startX + x * xStep, startY + y * yStep);
                GridPoints.Add(point);

                if (_shape.IsPointOnOutline(point, center, size, thick))
                    ShapeOutline.Add(point);
                else if (_shape.IsPointInside(point, center, size))
                    ShapeInterior.Add(point);
                else
                    ShapeExterior.Add(point);

                // ── Spawn clickable UI point ──────────────────────────
                if (spawnClickPoints)
                    SpawnGridPoint(point, new Vector2Int(x, y));
            }
        }

        gridParent.sizeDelta = Vector2.zero;

        Debug.Log($"[GridGenerator] Total: {GridPoints.Count} | " +
                  $"Interior: {ShapeInterior.Count} | " +
                  $"Outline: {ShapeOutline.Count} | " +
                  $"Exterior: {ShapeExterior.Count}");
    }

    private void SpawnGridPoint(Vector2 localPos, Vector2Int coord) {
        GameObject go = new GameObject($"GP_{coord.x}_{coord.y}");

        go.transform.SetParent(gridParent, false);

        RectTransform rt = go.AddComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = localPos;
        rt.sizeDelta = new Vector2(pointClickSize, pointClickSize);

        GridPoint gp = go.AddComponent<GridPoint>();

        gp.LocalPosition = localPos;
        gp.GridCoordinate = coord;

        PointMap[localPos] = gp;
        CellMap[coord] = gp;
    }

    // ── Shape Factory ─────────────────────────────────────────────────

    private static IGridShape CreateShape(ShapeType type) => type switch {
        ShapeType.Circle => new CircleShape(),
        ShapeType.Square => new SquareShape(),
        ShapeType.Triangle => new TriangleShape(),
        ShapeType.Diamond => new DiamondShape(),
        ShapeType.Carrot => new CarrotShape(),
        _ => new CircleShape(),
    };

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (gridParent == null) return;

        _shape ??= CreateShape(shapeType);

        float W = gridParent.rect.width;
        float H = gridParent.rect.height;
        float xStep = columns > 1 ? W / (columns - 1) : 0f;
        float yStep = rows > 1 ? H / (rows - 1) : 0f;
        float startX = -W / 2f;
        float startY = -H / 2f;
        float cellSize = Mathf.Min(xStep, yStep);
        float dotSize = cellSize * 0.2f;

        Vector2 center = Vector2.zero;
        float radius = Mathf.Min(W, H) / 2f * sizeScale;
        float thick = cellSize * outlineThickness;

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < columns; x++) {
                Vector2 local = new Vector2(startX + x * xStep, startY + y * yStep);
                Vector3 world = gridParent.TransformPoint(local);

                if (_shape.IsPointOnOutline(local, center, radius, thick)) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(world, dotSize * 1.2f);
                }
                else if (_shape.IsPointInside(local, center, radius)) {
                    Gizmos.color = new Color(0f, 1f, 0.4f, 0.85f);
                    Gizmos.DrawSphere(world, dotSize);
                }
                else {
                    Gizmos.color = new Color(1f, 1f, 1f, 0.08f);
                    Gizmos.DrawSphere(world, dotSize * 0.4f);
                }
            }
        }

        // Smooth wire outline
        Gizmos.color = Color.yellow;
        var wire = _shape.GetOutlinePoints(center, radius);
        for (int i = 0; i < wire.Count; i++) {
            Vector3 a = gridParent.TransformPoint(wire[i]);
            Vector3 b = gridParent.TransformPoint(wire[(i + 1) % wire.Count]);
            Gizmos.DrawLine(a, b);
        }

        // Center cross
        Gizmos.color = Color.red;
        Vector3 c3 = gridParent.TransformPoint(center);
        Gizmos.DrawLine(c3 - Vector3.right * cellSize, c3 + Vector3.right * cellSize);
        Gizmos.DrawLine(c3 - Vector3.up * cellSize, c3 + Vector3.up * cellSize);
    }
#endif
}