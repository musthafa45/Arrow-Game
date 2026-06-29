using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LevelEditor;

public class GridPoint : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

    public Vector2Int GridCoordinate;
    public Vector2 LocalPosition;
    public SnakeRenderer OccupiedSnake;   // which snake owns this point
    public int SnakePointIndex;   // index in that snake's path

    private Image _image;

    void Awake() {
        _image = gameObject.AddComponent<Image>();
        _image.color = new Color(1, 1, 1, 0.15f); // dim by default
        _image.raycastTarget = true;
    }

    public void SetOccupied(SnakeRenderer snake, int index) {
        OccupiedSnake = snake;
        SnakePointIndex = index;

        if(OccupiedSnake != null) {
            _image.color = new Color(1f, 1f, 0f, 0.25f); // yellow tint = occupied
        }
        else {
            _image.color = new Color(1f, 1f, 1f, 0.15f);
        }

    }

    public void ClearIfOwnedBy(SnakeRenderer snake) {
        if (OccupiedSnake != snake) return;

        SetFree();
    }

    public void SetFree() {
        OccupiedSnake = null;
        SnakePointIndex = -1;
        _image.color = new Color(1, 1, 1, 0.15f);
    }

    public bool IsOccupied() => OccupiedSnake != null;

    // ── Pointer Events ────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData) {

        if (GameManager.Instance != null) {
            if (GameManager.Instance.CurrentGameMode == GameMode.LevelEditorMode) {
                Debug.Log($"[GridPoint] Clicked point at {LocalPosition} in edit mode.");
                LevelEditManager.Instance.HandleGridPointClick(this);
                return;
            }
        }
        

        if (OccupiedSnake != null) {
            Debug.Log($"[GridPoint] Clicked occupied point → Snake: {OccupiedSnake.name} " +
                      $"index: {SnakePointIndex} color: {OccupiedSnake.color}");
            // Forward click to the snake
            if(!OccupiedSnake.IsDoingMove) {
                OccupiedSnake.StartHighlight();
                OccupiedSnake.MoveSnakeOffScreen();
            }
            else {
                Debug.Log($"[GridPoint] Snake {OccupiedSnake.name} is already moving, click ignored.");
            }
            
        }
        else {
            Debug.Log($"[GridPoint] Clicked free point at {LocalPosition}");
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        _image.color = OccupiedSnake != null
            ? new Color(1f, 1f, 0f, 0.5f)   // brighter yellow on hover
            : new Color(1f, 1f, 1f, 0.35f); // brighter white on hover
    }

    public void OnPointerExit(PointerEventData eventData) {
        _image.color = OccupiedSnake != null
            ? new Color(1f, 1f, 0f, 0.25f)
            : new Color(1f, 1f, 1f, 0.15f);
    }

    public void Blink() {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine() {
        if (_image == null)
            yield break;

        Color normalColor = OccupiedSnake != null
            ? new Color(1f, 1f, 0f, 0.25f)
            : new Color(1f, 1f, 1f, 0.15f);

        Color blinkColor = OccupiedSnake != null
            ? new Color(1f, 1f, 0f, 0.5f)
            : new Color(1f, 1f, 1f, 0.35f);

        _image.color = blinkColor;

        yield return new WaitForSeconds(0.15f);

        _image.color = normalColor;
    }

    public void Highlight(Color yellow) {
        _image.color = yellow;
    }

    public void ResetColor() {
        _image.color = new Color(1f, 1f, 1f, 0.15f);
    }
}