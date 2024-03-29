﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace RedeevEditor
{
    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    public class EnumFlagsAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasMultipleDifferentValues) EditorGUI.LabelField(position, "Multi selection is not allowed", EditorStyles.centeredGreyMiniLabel);
            else property.intValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
        }
    }

    [CustomPropertyDrawer(typeof(EnumSingleAttribute))]
    public class EnumSingleAttribute_Editor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasMultipleDifferentValues)
            {
                EditorGUI.LabelField(position, "Multi selection is not allowed", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EnumSingleAttribute singleAttribute = (EnumSingleAttribute)attribute;

            List<GUIContent> displayTexts = new();
            List<int> enumValues = new();
            foreach (var displayText in Enum.GetValues(singleAttribute.Type))
            {
                displayTexts.Add(new GUIContent(displayText.ToString()));
                enumValues.Add((int)displayText);
            }

            property.intValue = EditorGUI.IntPopup(position, label, property.intValue, displayTexts.ToArray(), enumValues.ToArray());
        }
    }
}
#endif