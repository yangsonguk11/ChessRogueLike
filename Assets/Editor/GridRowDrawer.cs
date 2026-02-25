using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(RangeInfo))]
public class RangeInfoDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty columns = property.FindPropertyRelative("columns");

        // 레이블(행 번호 등) 표시를 위해 너비 조절
        float columnWidth = position.width / columns.arraySize;
        Rect newPos = new Rect(position.x, position.y, columnWidth, position.height);

        for (int i = 0; i < columns.arraySize; i++)
        {
            // 각 칸(PropertyField)을 가로로 배치
            EditorGUI.PropertyField(newPos, columns.GetArrayElementAtIndex(i), GUIContent.none);
            newPos.x += columnWidth;
        }
    }
}