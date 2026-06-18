using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RangeInfoSO))]
public class RangeInfoSOEditor : Editor
{
    private int pendingSize;

    private void OnEnable()
    {
        pendingSize = serializedObject.FindProperty("rows").arraySize;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty rows = serializedObject.FindProperty("rows");

        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        pendingSize = EditorGUILayout.IntField(pendingSize);
        if (GUILayout.Button("Apply", GUILayout.Width(60)))
        {
            ResizeGrid(rows, Mathf.Max(1, pendingSize));
            pendingSize = rows.arraySize;
        }
        if (GUILayout.Button("전부 지우기", GUILayout.Width(80)))
        {
            ClearAll(rows);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(rows, true);

        serializedObject.ApplyModifiedProperties();
    }

    // rows와 각 행의 columns를 정사각형(newSize x newSize)으로 맞추고, 기존에 칠해진 셀은 가능한 한 보존한다.
    private void ResizeGrid(SerializedProperty rows, int newSize)
    {
        rows.arraySize = newSize;
        for (int i = 0; i < newSize; i++)
        {
            SerializedProperty columns = rows.GetArrayElementAtIndex(i).FindPropertyRelative("columns");
            columns.arraySize = newSize;
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void ClearAll(SerializedProperty rows)
    {
        for (int i = 0; i < rows.arraySize; i++)
        {
            SerializedProperty columns = rows.GetArrayElementAtIndex(i).FindPropertyRelative("columns");
            for (int j = 0; j < columns.arraySize; j++)
            {
                columns.GetArrayElementAtIndex(j).boolValue = false;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
