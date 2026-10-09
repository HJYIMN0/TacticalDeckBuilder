using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileGrid))]
public class TileGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var grid = (TileGrid)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate / Update Grid")) grid.Generate();
        if (GUILayout.Button("Clear Grid")) grid.Clear();
    }
}