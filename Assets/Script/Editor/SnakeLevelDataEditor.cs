#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SnakeLevelData))]
public class SnakeLevelDataEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        SnakeLevelData data = (SnakeLevelData)target;

        GUILayout.Space(10);
        GUILayout.Label("Nudge Layout", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◄ Left")) Nudge(data, -1, 0);
        if (GUILayout.Button("► Right")) Nudge(data, 1, 0);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("▲ Up")) Nudge(data, 0, 1);
        if (GUILayout.Button("▼ Down")) Nudge(data, 0, -1);
        GUILayout.EndHorizontal();
    }

    private void Nudge(SnakeLevelData data, int dx, int dy) {
        Undo.RecordObject(data, "Nudge Snake Layout");

        foreach (var snake in data.snakes)
            for (int i = 0; i < snake.cells.Count; i++)
                snake.cells[i] += new Vector2Int(dx, dy);

        EditorUtility.SetDirty(data);
    }
}
#endif