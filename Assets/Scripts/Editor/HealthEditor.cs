using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Health))]
public class HealthEditor : Editor {

    SerializedProperty healthSegmentArray;
    SerializedProperty segment;

    private int numberOfSegments;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
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
        GUILayout.Label("Health Segments");

        serializedObject.Update();
        Health health = (Health)target;
        healthSegmentArray = serializedObject.FindProperty("healthSegmentArray");

        incrementButtons();
        healthSegmentArray.arraySize = numberOfSegments;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("universalRecharge"), new GUIContent("Universal Recharge",
            "When enabled all segments will only recharge in order of layer (lowest to highest). Otherwise all segments can recharge independently"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("universalDamageReset"), new GUIContent("Universal Damage Reset",
            "When enabled all segments will reset their recharge. Otherwise rechrage resets will only occur when specific segments take damage"));


        for (int i = 0; i < numberOfSegments; i++)
        {
            GUILayout.Label("Segment #" + i.ToString(), EditorStyles.boldLabel);
            segment = healthSegmentArray.GetArrayElementAtIndex(i);

            drawHealthOptions(); GUILayout.Space(2);
            drawRechargeOptions(); GUILayout.Space(2);
            drawSpecialOptions(); GUILayout.Space(2);

            EditorGUILayout.PropertyField(segment.FindPropertyRelative("segmentType"));
            switch (segment.FindPropertyRelative("segmentType").enumValueIndex)
            {
                case (int)SegmentType.health:
                    drawHealthSegment();
                    break;
                case (int)SegmentType.armour:
                    drawArmourSegment();
                    break;
                case (int)SegmentType.shield:
                    drawShieldSegment();
                    break;
                case (int)SegmentType.barrier:
                    drawBarrierSegment();
                    break;

            }

            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Draw and check for input to increase or decrease the number of segments
    private void incrementButtons ()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add"))
        {
            numberOfSegments++;
        }
        if (GUILayout.Button("Remove"))
        {
            if (numberOfSegments > 1)
            {
                numberOfSegments--;
            }
        }

        if (numberOfSegments < 1) numberOfSegments = 1;
        EditorGUILayout.EndHorizontal();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the portion of the GUI for health options
    private void drawHealthOptions ()
    {
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("startActive"), new GUIContent("Start with Health",
                "Determines if the segment will have any health on Start"));
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("maxHealth"), new GUIContent("Maximum Health",
            "Maximum health the segment can have"));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the portion of the GUI for recharge options
    private void drawRechargeOptions ()
    {
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("canRecharge"), new GUIContent("Can Recharge",
                "Determines if the segment can recharge its own health"));

        if (segment.FindPropertyRelative("canRecharge").boolValue)
        {
            EditorGUILayout.PropertyField(segment.FindPropertyRelative("rechargeRate"), new GUIContent("Recharge Rate",
                "The speed at which the segment will recharge health after the timer expires"));
            EditorGUILayout.PropertyField(segment.FindPropertyRelative("rechargeDelay"), new GUIContent("Recharge Delay",
                "The time it will take for the segment to begin recharging health after taking damage"));
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the portion of the GUI for special settings
    private void drawSpecialOptions ()
    {
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("carryDamageToNextSegment"), new GUIContent("Carry Damage",
                "Will any spillover damage be applied to the next segment?"));
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("carryHealingToNextSegment"), new GUIContent("Carry Healing",
            "Will any spillover health be applied to the next segment?"));
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("specialTags"), new GUIContent("Tags",
            "Tags/Strings used to identify this segment"), true);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the options specific to a health segment
    private void drawHealthSegment ()
    {
        GUILayout.Label("**No additional Options**");
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the options specific to an armour segment
    private void drawArmourSegment ()
    {
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("armourDamageReduction"), new GUIContent("Damage Reduction",
            "Value to be removed from every attack"));
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("minimumArmourDamage"), new GUIContent("Minimum Damage Taken",
            "Damage the segment will take if the modified value falls below this minimum"));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the options specific to a shield segment
    private void drawShieldSegment ()
    {
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("constantShieldDamage"), new GUIContent("Constant Shield Damage",
            "Amount of damage the shield will take every time damage is applied regardless of provided value"));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    //Called to draw the options specific to a barrier segment
    private void drawBarrierSegment ()
    {
        EditorGUILayout.PropertyField(segment.FindPropertyRelative("barrierDamageMitigation"), new GUIContent("Barrier Mitigation",
            "Percentage of the original damage provided the segment will take. Recommended: 0.0 - 1.0"));
    }
}
