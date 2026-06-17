using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Snake/Level Data")]
public class SnakeLevelData : ScriptableObject {
    public List<SnakeData> snakes = new();
}

[System.Serializable]
public class SnakeData {
    public Color color = Color.red;
    public List<Vector2Int> cells = new();
}