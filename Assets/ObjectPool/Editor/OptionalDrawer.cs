using UnityEngine;
using UnityEditor;

// From: https://gist.github.com/aarthificial/f2dbb58e4dbafd0a93713a380b9612af

namespace eilume.ObjectPool
{
    [CustomPropertyDrawer(typeof(Optional<>))]
    public class OptionalDrawer : PropertyDrawer
    {
        protected SerializedProperty enabledProperty;
        protected SerializedProperty valueProperty;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            valueProperty = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProperty);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            enabledProperty = property.FindPropertyRelative("enabled");
            valueProperty = property.FindPropertyRelative("value");

            EditorGUI.BeginProperty(position, label, property);
            position.width -= 24;
            EditorGUI.BeginDisabledGroup(!enabledProperty.boolValue);
            EditorGUI.PropertyField(position, valueProperty, label, true);
            EditorGUI.EndDisabledGroup();

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            position.x += position.width + 24;
            position.width = position.height = EditorGUI.GetPropertyHeight(enabledProperty);
            position.x -= position.width;
            EditorGUI.PropertyField(position, enabledProperty, GUIContent.none);
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}