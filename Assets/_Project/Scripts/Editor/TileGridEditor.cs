using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileGrid))]
public class TileGridEditor : Editor
{
    readonly HashSet<int> openRows = new HashSet<int>();
    Vector2Int coordToAdd;

    // azione da eseguire a fine OnInspectorGUI: modificare la lista mentre la si disegna rompe il layout IMGUI
    System.Action pending;

    public override void OnInspectorGUI()
    {
        var grid = (TileGrid)target;

        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "slots");
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate / Update Grid")) pending = grid.Generate;
        if (GUILayout.Button("Clear Grid (dimentica anche le tile cancellate)")) pending = grid.Clear;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Aggiungi / ricrea una tile", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        coordToAdd = EditorGUILayout.Vector2IntField("Coord (x, riga)", coordToAdd);
        if (GUILayout.Button("Add", GUILayout.Width(50)))
        {
            Vector2Int c = coordToAdd;
            pending = () => grid.AddTile(c);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Tile per riga ({grid.Slots.Count} slot)", EditorStyles.boldLabel);

        serializedObject.Update();
        DrawRows(grid);
        serializedObject.ApplyModifiedProperties();

        if (pending != null)
        {
            System.Action action = pending;
            pending = null;
            action();
        }
    }

    void DrawRows(TileGrid grid)
    {
        IReadOnlyList<TileSlot> slots = grid.Slots;
        int i = 0;

        // gli slot sono ordinati per riga: ogni riga è un blocco contiguo [i, end)
        while (i < slots.Count)
        {
            int row = slots[i].coord.y;
            int end = i;
            int present = 0;

            while (end < slots.Count && slots[end].coord.y == row)
            {
                if (slots[end].tile != null) present++;
                end++;
            }

            bool wasOpen = openRows.Contains(row);
            bool isOpen = EditorGUILayout.Foldout(wasOpen, $"Riga {row}   ({present}/{end - i} tile)", true);
            if (isOpen != wasOpen)
            {
                if (isOpen) openRows.Add(row);
                else openRows.Remove(row);
            }

            if (isOpen)
            {
                EditorGUI.indentLevel++;
                for (int k = i; k < end; k++) DrawSlot(grid, k);
                EditorGUI.indentLevel--;
            }

            i = end;
        }
    }

    void DrawSlot(TileGrid grid, int index)
    {
        TileSlot slot = grid.Slots[index];

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"x {slot.coord.x}", GUILayout.Width(60));

        if (slot.tile == null)
        {
            EditorGUILayout.LabelField("cancellata", EditorStyles.miniLabel);
            if (GUILayout.Button("Ricrea", GUILayout.Width(60)))
            {
                Vector2Int c = slot.coord;
                pending = () => grid.AddTile(c);
            }
        }
        else
        {
            SerializedProperty tileProp = serializedObject.FindProperty("slots")
                .GetArrayElementAtIndex(index).FindPropertyRelative("tile");
            EditorGUILayout.PropertyField(tileProp, GUIContent.none);

            if (GUILayout.Button("Edit", GUILayout.Width(40)))
                EditorUtility.OpenPropertyEditor(slot.tile);

            if (GUILayout.Button("Sel", GUILayout.Width(40)))
                Selection.activeGameObject = slot.tile.gameObject;
        }

        EditorGUILayout.EndHorizontal();
    }
}