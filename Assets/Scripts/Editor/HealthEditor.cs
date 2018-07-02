using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(Health))]
public class HealthEditor : Editor {
    public override void OnInspectorGUI()
    {
        /*  EXAMPLE
        serializedObject.Update();
        Health h = (Health)target;
        SerializedProperty a = serializedObject.FindProperty("healthSegmentArray");
        EditorGUI.BeginChangeCheck();
        a.arraySize = 3;
        SerializedProperty t = a.GetArrayElementAtIndex(0);
        t.FindPropertyRelative("maxHealth").floatValue = 999;
        EditorGUILayout.PropertyField(a, true);   //Shows in inspector
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        */


    }
}
