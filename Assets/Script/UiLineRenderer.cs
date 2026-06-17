using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : Graphic{

    public bool IsDoingMove { get; private set; } = false;

    [SerializeField] public List<Vector2> Points = new();
    [SerializeField] public float lineWidth = 5f;
    [SerializeField][Range(3, 12)] public int joinSegments = 10;

    [Header("Snake Head")]
    [SerializeField] public bool showHead = true;
    [SerializeField] public float headSize = 1.6f;  // multiplier of lineWidth
    [SerializeField] public Color headColor = Color.red;
    [SerializeField] public int eyeSegments = 8;
    [SerializeField] public float tongueLength = 0.4f;


    [Header("Click")]
    [SerializeField] public Color highlightColor = Color.white;
    [SerializeField] public float highlightDuration = 0.3f; // seconds to fade back
    private Color _originalColor;

    private Coroutine _highlightCoroutine;
    private GridPoint _headGridPoint;

    [Header("Debug")]
    private Vector2 _debugHeadPos;
    private Vector2 _debugDir;

    [Header("Movement")]
    private readonly float moveSpeed = 7000f;
    private readonly float maxMoveDistance = 3000f; // how far to move off screen

    private List<GridPoint> towarsGridPoints;
    private List<GridPoint> ownedGridPoints = new();

    private List<Vector2> originalPoints;
    protected override void Awake() {
        base.Awake();
        // Graphic requires a CanvasRenderer — Unity adds it but just in case:
        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();

        raycastTarget = false; // ← enables click detection on the mesh
        _originalColor = color;
    }

    public void MoveSnakeOffScreen() {
        Debug.Log($"[UILineRenderer] Moving snake off screen: {name}");

        towarsGridPoints = new List<GridPoint>();

        towarsGridPoints = GetTowardsGridPoints();

        bool canGoOutOffScreen = true;

        List<GridPoint> collideBeforeGridPoints = new();

        foreach (var gp in towarsGridPoints) {
            Debug.Log( $"Point {gp.LocalPosition} Occupied:{gp.IsOccupied() && gp.OccupiedSnake != this}");
            gp.Blink();
            
            if(gp.IsOccupied() && gp.OccupiedSnake != this) {
                canGoOutOffScreen = false;
                break;
            }

            collideBeforeGridPoints.Add(gp);
        }

        Debug.Log($"Can go off screen: {canGoOutOffScreen}");


        if(canGoOutOffScreen) {
            StartCoroutine(MoveOffScreenCoroutine());

            // ── Un Register snake on each GridPoint ──────────────────────────
            foreach (var gp in ownedGridPoints)
                gp.ClearIfOwnedBy(this);
        }
        else {
            // ── Snake Can't Go Off Screen But Still Has to Move Towards grid points Until it Can hit Front Snake And has to Come back old Pos ──────────────────────────
            originalPoints = new List<Vector2>(Points);
            StartCoroutine(MoveCollideBeforeGridPointsCoroutine(collideBeforeGridPoints));
        }
    }

    private IEnumerator MoveCollideBeforeGridPointsCoroutine(List<GridPoint> path) {
        if (path.Count == 0)
            yield break;

        IsDoingMove = true;

        Vector2 dir = (Points[0] - Points[1]).normalized;

        Vector2 startHeadPos = Points[0];

        // Move forward
        foreach (var gp in path) {
            Vector2 target = gp.LocalPosition;

            while (Vector2.Distance(Points[0], target) > 5f) {
                List<Vector2> oldPositions = new List<Vector2>(Points);

                Points[0] = Vector2.MoveTowards(
                    Points[0],
                    target,
                    moveSpeed * Time.deltaTime);

                for (int i = 1; i < Points.Count; i++)
                    Points[i] = oldPositions[i - 1];

                SetVerticesDirty();

                yield return null;
            }
        }

        yield return new WaitForSeconds(0.2f);

        // Reverse back
        while (Vector2.Distance(Points[0], startHeadPos) > 5f) {
            for (int i = 0; i < Points.Count; i++) {
                Points[i] = Vector2.MoveTowards(
                    Points[i],
                    originalPoints[i],
                    moveSpeed * Time.deltaTime);
            }

            SetVerticesDirty();

            yield return null;
        }

        Points = new List<Vector2>(originalPoints);
        SetVerticesDirty();

        IsDoingMove = false;
    }

    private IEnumerator MoveOffScreenCoroutine() {
        Vector2 dir = (Points[0] - Points[1]).normalized;

        Vector2 startPos = Points[0];

        while (true) {
            float travelled = Vector2.Distance(startPos, Points[0]);

            if (travelled >= maxMoveDistance) {
                gameObject.SetActive(false);
                yield break;
            }

            List<Vector2> oldPositions = new List<Vector2>(Points);

            Points[0] += moveSpeed * Time.deltaTime * dir;

            for (int i = 1; i < Points.Count; i++) {
                Points[i] = oldPositions[i - 1];
            }

            SetVerticesDirty();

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void SetHeadGridPoint() {
        float bestDist = float.MaxValue;

        Vector2 headPos = Points[0];

        foreach (var kv in GridGenerator.Instance.PointMap) {
            float d = Vector2.Distance(headPos, kv.Key);

            if (d < bestDist) {
                bestDist = d;
                _headGridPoint = kv.Value;
            }
        }
    }

    private List<GridPoint> GetTowardsGridPoints() {
        List<GridPoint> result = new();

        SetHeadGridPoint();

        if (_headGridPoint == null)
            return result;

        Vector2 dir = (Points[0] - Points[1]).normalized;

        Vector2 headPos = _headGridPoint.LocalPosition;

        _debugDir = dir;
        _debugHeadPos = headPos;

        foreach (var kvp in GridGenerator.Instance.PointMap) {
            Vector2 pos = kvp.Key;

            // Right
            if (dir == Vector2.right &&
                Mathf.Approximately(pos.y, headPos.y) &&
                pos.x > headPos.x) {
                result.Add(kvp.Value);
            }

            // Left
            else if (dir == Vector2.left &&
                     Mathf.Approximately(pos.y, headPos.y) &&
                     pos.x < headPos.x) {
                result.Add(kvp.Value);
            }

            // Up
            else if (dir == Vector2.up &&
                     Mathf.Approximately(pos.x, headPos.x) &&
                     pos.y > headPos.y) {
                result.Add(kvp.Value);
            }

            // Down
            else if (dir == Vector2.down &&
                     Mathf.Approximately(pos.x, headPos.x) &&
                     pos.y < headPos.y) {
                result.Add(kvp.Value);
            }
        }

        result.Sort((a, b) =>
        {
            float da = Vector2.Distance(a.LocalPosition, headPos);
            float db = Vector2.Distance(b.LocalPosition, headPos);
            return da.CompareTo(db);
        });

        return result;
    }


    protected override void OnPopulateMesh(VertexHelper vh) {
        vh.Clear();

        // ── Guard: need at least 2 DISTINCT points ────────────────────
        if (Points == null || Points.Count < 2) return;
        if (Vector2.Distance(Points[0], Points[1]) < 0.001f) return;

        // ── Body segments ─────────────────────────────────────────────
        for (int i = 0; i < Points.Count - 1; i++)
            DrawSegment(vh, Points[i], Points[i + 1]);

        // ── Corner joins ──────────────────────────────────────────────
        for (int i = 1; i < Points.Count - 1; i++)
            DrawCircle(vh, Points[i], lineWidth * 0.5f, color);

        // ── Tail cap ──────────────────────────────────────────────────
        DrawCircle(vh, Points[Points.Count - 1], lineWidth * 0.5f, color);

        // ── Snake head ────────────────────────────────────────────────
        if (showHead)
            DrawHead(vh, Points[0], Points[1]);
    }

    // ── Head ──────────────────────────────────────────────────────────

    private void DrawHead(VertexHelper vh, Vector2 headPos, Vector2 nextPos) {
        float headRadius = lineWidth * headSize * 0.5f;

        Vector2 forward = (headPos - nextPos).normalized;

        // ── Safety: if forward is zero skip head entirely ─────────────
        if (forward == Vector2.zero) return;

        Vector2 right = new Vector2(-forward.y, forward.x);

        // ── Head circle ───────────────────────────────────────────────
        DrawCircle(vh, headPos, headRadius, color);

        // ── Snout bump ────────────────────────────────────────────────
        Vector2 snoutPos = headPos + forward * (headRadius * 0.5f);
        DrawCircle(vh, snoutPos, headRadius * 0.55f, color);

        // ── Eyes ──────────────────────────────────────────────────────
        float eyeRadius = headRadius * 0.28f;
        float eyeOffset = headRadius * 0.42f;
        float eyeForward = headRadius * 0.25f;

        Vector2 leftEye = headPos + right * eyeOffset + forward * eyeForward;
        Vector2 rightEye = headPos - right * eyeOffset + forward * eyeForward;

        DrawCircle(vh, leftEye, eyeRadius, Color.white);
        DrawCircle(vh, rightEye, eyeRadius, Color.white);

        float pupilRadius = eyeRadius * 0.55f;
        Vector2 pupilPush = forward * (eyeRadius * 0.2f);

        DrawCircle(vh, leftEye + pupilPush, pupilRadius, Color.black);
        DrawCircle(vh, rightEye + pupilPush, pupilRadius, Color.black);

        // ── Tongue ────────────────────────────────────────────────────
        DrawTongue(vh, headPos, forward, right, headRadius);
    }

    private void DrawTongue(VertexHelper vh, Vector2 headPos,
                         Vector2 forward, Vector2 right, float headRadius) {
        Color tongueColor = new Color(0.9f, 0.1f, 0.2f);
        float tongueWidth = lineWidth * 0.12f;
        float stemLen = headRadius * tongueLength;        // ← uses your field
        float forkLen = stemLen * 0.4f;                // fork = 40% of stem
        float forkSpread = headRadius * 0.28f;

        Vector2 tongueBase = headPos + forward * headRadius * 0.85f;
        Vector2 tongueTip = tongueBase + forward * stemLen;

        // Main stem
        DrawThickLine(vh, tongueBase, tongueTip, tongueWidth, tongueColor);

        // Left fork
        DrawThickLine(vh, tongueTip,
                      tongueTip + forward * forkLen + right * forkSpread,
                      tongueWidth * 0.8f, tongueColor);

        // Right fork
        DrawThickLine(vh, tongueTip,
                      tongueTip + forward * forkLen - right * forkSpread,
                      tongueWidth * 0.8f, tongueColor);
    }

    // ── Primitives ────────────────────────────────────────────────────

    private void DrawSegment(VertexHelper vh, Vector2 a, Vector2 b) {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);
        int idx = vh.currentVertCount;

        AddVert(vh, a - perp, color);
        AddVert(vh, a + perp, color);
        AddVert(vh, b + perp, color);
        AddVert(vh, b - perp, color);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }

    private void DrawThickLine(VertexHelper vh, Vector2 a, Vector2 b,
                               float width, Color c) {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);
        int idx = vh.currentVertCount;

        AddVert(vh, a - perp, c);
        AddVert(vh, a + perp, c);
        AddVert(vh, b + perp, c);
        AddVert(vh, b - perp, c);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }

    private void DrawCircle(VertexHelper vh, Vector2 center,
                            float radius, Color c) {
        int idx = vh.currentVertCount;
        AddVert(vh, center, c);

        for (int i = 0; i <= joinSegments; i++) {
            float angle = 2f * Mathf.PI * i / joinSegments;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            AddVert(vh, center + offset, c);
        }

        for (int i = 0; i < joinSegments; i++)
            vh.AddTriangle(idx, idx + i + 1, idx + i + 2);
    }

    // ── Vertex helper ─────────────────────────────────────────────────

    private void AddVert(VertexHelper vh, Vector2 pos, Color c) {
        UIVertex v = UIVertex.simpleVert;
        v.color = c;
        v.position = new Vector3(pos.x, pos.y, 0);
        vh.AddVert(v);
    }

    // ── Public API ────────────────────────────────────────────────────

    public void SetPoints(List<Vector2> pts) {
        Points = new List<Vector2>(pts);
        SetVerticesDirty();
    }

    public void SetOccupiedGridPoints(List<GridPoint> points) {
        ownedGridPoints = points;
    }

    public void SetColor(Color c) {
        color = c;
        _originalColor = c;
        SetVerticesDirty();
    }

    public void StartHighlight() {
        if (_highlightCoroutine != null)
            StopCoroutine(_highlightCoroutine);

        _highlightCoroutine = StartCoroutine(HighlightFade());
    }

    // ── Highlight Coroutine ─────────────────────────────────────────

    private IEnumerator HighlightFade() {
        color = highlightColor;
        SetVerticesDirty();

        float elapsed = 0f;
        while (elapsed < highlightDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / highlightDuration;
            color = Color.Lerp(highlightColor, _originalColor, t);
            SetVerticesDirty();
            yield return null;
        }

        color = _originalColor;
        SetVerticesDirty();
    }


    private void OnDrawGizmos() {
        if (_headGridPoint == null)
            return;

        Gizmos.color = Color.red;

        Vector3 start = transform.TransformPoint(_debugHeadPos);
        Vector3 end = transform.TransformPoint(_debugHeadPos + _debugDir * 200f);

        Gizmos.DrawLine(start, end);

        Gizmos.DrawSphere(start, 5f);
    }
}